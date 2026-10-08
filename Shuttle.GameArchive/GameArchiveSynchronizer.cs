using System.Security.Cryptography;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Shuttle.GameArchive;

public sealed class GameArchiveSynchronizer(
    GameArchiveCrawler crawler,
    IGameArchiveCredentialProvider secrets,
    IOptions<GameArchiveOptions> configured,
    IHostEnvironment environment,
    ILogger<GameArchiveSynchronizer> logger) {

    public async Task SynchronizeAsync(CancellationToken cancellationToken) {
        var options = configured.Value;
        var root = new Uri(options.SourceUrl, UriKind.Absolute);
        var remote = new Uri(options.RemoteUrl, UriKind.Absolute);
        if (root.Scheme != Uri.UriSchemeHttps && !(environment.IsDevelopment() && root.IsLoopback))
            throw new InvalidOperationException("Archive source must use HTTPS outside local development.");
        if (remote.Scheme != Uri.UriSchemeHttps && !(environment.IsDevelopment() &&
            (remote.IsLoopback && remote.Scheme == Uri.UriSchemeHttp || remote.IsFile)))
            throw new InvalidOperationException("Archive Git remote must use HTTPS outside local development.");
        if (!environment.IsDevelopment() &&
            (remote.Host != "github.com" || remote.AbsolutePath.TrimEnd('/') is not "/shuttle-shl/shl-games-archive.git" and not "/shuttle-shl/shl-games-archive"))
            throw new InvalidOperationException("Production archive Git remote must be shuttle-shl/shl-games-archive.");
        if (remote.UserInfo.Length > 0 || remote.Query.Length > 0 || remote.Fragment.Length > 0)
            throw new InvalidOperationException("Archive Git remote must not contain credentials, query or fragment.");

        var password = await secrets.GetPasswordAsync(options.SecretName, cancellationToken);
        if (string.IsNullOrEmpty(password))
            throw new InvalidOperationException("Archive Git credential is empty.");
        var scan = await crawler.InventoryAsync(root, cancellationToken);
        var inventory = scan.Files;
        foreach (var skipped in scan.SkippedDirectories)
            logger.LogWarning("Skipping non-Apache directory {DirectoryUrl} (title: {Title}); its managed files will be removed from the current snapshot",
                skipped.Url, skipped.Title);
        if (scan.SkippedDirectories.Count > 0)
            logger.LogWarning("Archive snapshot excludes {SkippedDirectoryCount} non-Apache directories; see {ReportFile}",
                scan.SkippedDirectories.Count, GameArchiveManifest.FileName);
        logger.LogInformation("Found {FileCount} game files in the source tree", inventory.Count);
        var workspace = Path.Combine(Path.GetTempPath(), "shuttle-game-archive", Guid.NewGuid().ToString("N"));
        var staging = Path.Combine(workspace, "downloads");
        var checkout = Path.Combine(workspace, "repository");
        Directory.CreateDirectory(staging);
        try {
            await Parallel.ForEachAsync(inventory, new ParallelOptions {
                MaxDegreeOfParallelism = 4,
                CancellationToken = cancellationToken,
            }, async (entry, token) => {
                var (relative, url) = entry;
                await crawler.DownloadAsync(url, root, Path.Combine(staging, relative.Replace('/', Path.DirectorySeparatorChar)), token);
            });
            var totalBytes = Directory.EnumerateFiles(staging, "*", SearchOption.AllDirectories)
                .Sum(file => new FileInfo(file).Length);
            logger.LogInformation("Downloaded {FileCount} game files ({TotalBytes} bytes)", inventory.Count, totalBytes);
            if (totalBytes > 5L * 1024 * 1024 * 1024)
                logger.LogWarning("Game files exceed GitHub's recommended 5 GiB repository size before Git history");

            var git = new GameArchiveGit(options.GitUsername, password);
            using var repository = git.Clone(remote.AbsoluteUri, checkout, cancellationToken);
            var manifestPath = Path.Combine(checkout, GameArchiveManifest.FileName);
            EnsureSafeCheckoutPath(checkout, GameArchiveManifest.FileName);
            var previous = new HashSet<string>(StringComparer.Ordinal);
            if (File.Exists(manifestPath)) {
                await using var manifestStream = File.OpenRead(manifestPath);
                var previousManifest = await GameArchiveManifest.ReadAsync(manifestStream, cancellationToken);
                previous.UnionWith(previousManifest.Files);
            }

            foreach (var relative in inventory.Keys) {
                EnsureSafeCheckoutPath(checkout, relative);
                var target = Path.Combine(checkout, relative.Replace('/', Path.DirectorySeparatorChar));
                if (File.Exists(target) && !previous.Contains(relative))
                    throw new InvalidDataException($"An unmanaged archive file already exists at {relative}.");
                var incoming = Path.Combine(staging, relative.Replace('/', Path.DirectorySeparatorChar));
                if (File.Exists(target) && await EqualFilesAsync(incoming, target, cancellationToken))
                    continue;
                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                File.Copy(incoming, target, true);
            }
            var removedPaths = previous.Except(inventory.Keys, StringComparer.Ordinal).ToArray();
            foreach (var removed in removedPaths) {
                EnsureSafeCheckoutPath(checkout, removed);
                var target = Path.Combine(checkout, removed.Replace('/', Path.DirectorySeparatorChar));
                if (File.Exists(target))
                    File.Delete(target);
            }
            await File.WriteAllTextAsync(manifestPath,
                GameArchiveManifest.Create(inventory.Keys, scan.SkippedDirectories).ToJson(), cancellationToken);
            if (!git.CommitAndPush(repository, inventory.Keys, removedPaths, cancellationToken)) {
                logger.LogInformation("Game archive is already current");
                return;
            }
            logger.LogInformation("Published game archive snapshot with {FileCount} files", inventory.Count);
        } finally {
            if (OperatingSystem.IsWindows() && Directory.Exists(workspace)) {
                foreach (var file in Directory.EnumerateFiles(workspace, "*", SearchOption.AllDirectories))
                    File.SetAttributes(file, FileAttributes.Normal);
            }
            Directory.Delete(workspace, true);
        }
    }

    private static async Task<bool> EqualFilesAsync(string left, string right, CancellationToken cancellationToken) {
        await using var a = File.OpenRead(left);
        await using var b = File.OpenRead(right);
        if (a.Length != b.Length)
            return false;
        var leftHash = await SHA256.HashDataAsync(a, cancellationToken);
        var rightHash = await SHA256.HashDataAsync(b, cancellationToken);
        return leftHash.AsSpan().SequenceEqual(rightHash);
    }

    private static void EnsureSafeCheckoutPath(string checkout, string relative) {
        var current = checkout;
        foreach (var segment in relative.Split('/')) {
            current = Path.Combine(current, segment);
            if (Path.Exists(current) && (File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                throw new InvalidDataException($"An archive path is a symbolic link: {relative}");
        }
    }
}
