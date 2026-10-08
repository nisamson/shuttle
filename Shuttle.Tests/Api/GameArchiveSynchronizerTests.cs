using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shuttle.GameArchive;

namespace Shuttle.Tests.Api;

public sealed class GameArchiveSynchronizerTests {
    [Fact]
    public async Task MissingProductionCredentialFailsBeforeCrawling() {
        var source = new ArchiveSource();
        using var client = new HttpClient(source);
        var synchronizer = new GameArchiveSynchronizer(
            new GameArchiveCrawler(client),
            new MissingCredentialProvider(),
            Options.Create(new GameArchiveOptions()),
            new DevelopmentEnvironment { EnvironmentName = Environments.Production },
            NullLogger<GameArchiveSynchronizer>.Instance);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => synchronizer.SynchronizeAsync(TestContext.Current.CancellationToken));

        Assert.Equal(0, source.RequestCount);
    }

    [Fact]
    public async Task PublishesSnapshotWithNestedFilesManifestAndExistingReadme() {
        await using var fixture = new ArchiveFixture();
        fixture.Seed(("README.md", "Archive introduction"), ("other/keep.txt", "unmanaged"));
        fixture.Source.SetFiles(("shl/S85/game.csv", "first"), ("smjhl/S85/game.csv", "second"));

        await fixture.SynchronizeAsync();

        Assert.Equal("first", fixture.RemoteFile("shl/S85/game.csv"));
        Assert.Equal("second", fixture.RemoteFile("smjhl/S85/game.csv"));
        Assert.Equal("Archive introduction", fixture.RemoteFile("README.md"));
        Assert.Equal("unmanaged", fixture.RemoteFile("other/keep.txt"));
        Assert.Equal(
            ["shl/S85/game.csv", "smjhl/S85/game.csv"],
            ReadManifest(fixture).Files);
        Assert.Equal(GameArchiveManifest.SchemaUri, ReadManifest(fixture).Schema);
        Assert.Equal(1, ReadManifest(fixture).SchemaVersion);
        Assert.Empty(ReadManifest(fixture).SkippedDirectories);
        Assert.Equal(2, fixture.CommitCount);
    }

    [Fact]
    public async Task SkippedDirectoriesRemoveOnlyManagedFilesAndPublishReport() {
        await using var fixture = new ArchiveFixture();
        fixture.Seed(("README.md", "untouched"), ("skipped/notes.txt", "unmanaged"));
        fixture.Source.SetFiles(("skipped/old.csv", "old"), ("game.csv", "game"));
        await fixture.SynchronizeAsync();
        var previousHead = fixture.Head;
        fixture.Source.SetFiles(("skipped/old.csv", "not downloaded"), ("game.csv", "updated"));
        fixture.Source.DirectoryPages["skipped/"] = "<html><head><title>IIHF Indexes</title></head><body><a href=\"old.csv\">Old</a></body></html>";
        fixture.Source.RequestedPaths.Clear();

        await fixture.SynchronizeAsync();

        Assert.Null(fixture.RemoteFile("skipped/old.csv"));
        Assert.Equal("old", fixture.HistoricalFile(previousHead, "skipped/old.csv"));
        Assert.Equal("unmanaged", fixture.RemoteFile("skipped/notes.txt"));
        Assert.Equal("untouched", fixture.RemoteFile("README.md"));
        Assert.Equal("updated", fixture.RemoteFile("game.csv"));
        Assert.Equal("game.csv", Assert.Single(ReadManifest(fixture).Files));
        Assert.DoesNotContain("skipped/old.csv", fixture.Source.RequestedPaths);
        var skipped = Assert.Single(ReadManifest(fixture).SkippedDirectories);
        Assert.Equal("skipped/", skipped.Path);
        Assert.Equal("http://localhost/games/skipped/", skipped.Url);
        Assert.Equal("IIHF Indexes", skipped.Title);
        Assert.Equal("non-apache-directory-listing", skipped.Reason);
    }

    [Fact]
    public async Task SkippedRootRemovesManagedFilesButPreservesUnmanagedFilesAndHistory() {
        await using var fixture = new ArchiveFixture();
        fixture.Seed(("README.md", "untouched"), ("notes.txt", "unmanaged"));
        fixture.Source.SetFiles(("game.csv", "game"));
        await fixture.SynchronizeAsync();
        var previousHead = fixture.Head;
        fixture.Source.DirectoryPages[""] = "<html><body>Custom root index</body></html>";

        await fixture.SynchronizeAsync();

        Assert.Null(fixture.RemoteFile("game.csv"));
        Assert.Equal("game", fixture.HistoricalFile(previousHead, "game.csv"));
        Assert.Equal("untouched", fixture.RemoteFile("README.md"));
        Assert.Equal("unmanaged", fixture.RemoteFile("notes.txt"));
        Assert.Empty(ReadManifest(fixture).Files);
        var skipped = Assert.Single(ReadManifest(fixture).SkippedDirectories);
        Assert.Equal(".", skipped.Path);
        Assert.Null(skipped.Title);
    }

    [Fact]
    public async Task IdenticalSkippedReportDoesNotCommitButReportChangesDo() {
        await using var fixture = new ArchiveFixture();
        fixture.Seed(("README.md", "untouched"));
        fixture.Source.SetFiles(("skipped/hidden.csv", "hidden"));
        fixture.Source.DirectoryPages["skipped/"] = "<html><head><title>Custom index</title></head></html>";
        await fixture.SynchronizeAsync();
        var head = fixture.Head;

        await fixture.SynchronizeAsync();

        Assert.Equal(head, fixture.Head);
        Assert.Equal(2, fixture.CommitCount);
        fixture.Source.DirectoryPages["skipped/"] = "<html><head><title>Changed custom index</title></head></html>";

        await fixture.SynchronizeAsync();

        Assert.NotEqual(head, fixture.Head);
        Assert.Equal(3, fixture.CommitCount);
        Assert.Equal("Changed custom index", Assert.Single(ReadManifest(fixture).SkippedDirectories).Title);
        Assert.Null(fixture.RemoteFile("skipped/hidden.csv"));
    }

    [Fact]
    public async Task ResumingApacheListingClearsReportAndArchivesFiles() {
        await using var fixture = new ArchiveFixture();
        fixture.Seed(("README.md", "untouched"));
        fixture.Source.SetFiles(("skipped/game.csv", "game"));
        fixture.Source.DirectoryPages["skipped/"] = "<html><head><title>Custom index</title></head></html>";
        await fixture.SynchronizeAsync();
        Assert.Single(ReadManifest(fixture).SkippedDirectories);
        fixture.Source.DirectoryPages.Clear();

        await fixture.SynchronizeAsync();

        Assert.Empty(ReadManifest(fixture).SkippedDirectories);
        Assert.Equal("game", fixture.RemoteFile("skipped/game.csv"));
    }

    [Fact]
    public async Task FailedDownloadDoesNotPublishChangedSkippedReportOrRemoveFiles() {
        await using var fixture = new ArchiveFixture();
        fixture.Seed(("README.md", "untouched"));
        fixture.Source.SetFiles(("skipped/old.csv", "old"), ("game.csv", "game"));
        await fixture.SynchronizeAsync();
        var head = fixture.Head;
        var manifest = fixture.RemoteFile(GameArchiveManifest.FileName);
        fixture.Source.DirectoryPages["skipped/"] = "<html><head><title>Custom index</title></head></html>";
        fixture.Source.FailDownload = "game.csv";

        await Assert.ThrowsAsync<HttpRequestException>(() => fixture.SynchronizeAsync());

        Assert.Equal(head, fixture.Head);
        Assert.Equal(manifest, fixture.RemoteFile(GameArchiveManifest.FileName));
        Assert.Equal("old", fixture.RemoteFile("skipped/old.csv"));
    }

    [Fact]
    public async Task UpdatesAddsAndRemovesOnlyManifestOwnedFiles() {
        await using var fixture = new ArchiveFixture();
        fixture.Seed(
            ("README.md", "Keep this README"),
            ("outside.txt", "Keep this file"),
            ("shl/old.csv", "old"),
            ("shl/updated.csv", "before"),
            (GameArchiveManifest.FileName, ManifestJson("shl/old.csv", "shl/updated.csv")));
        fixture.Source.SetFiles(("shl/updated.csv", "after"), ("shl/new.csv", "new"));

        await fixture.SynchronizeAsync();

        Assert.Equal("after", fixture.RemoteFile("shl/updated.csv"));
        Assert.Equal("new", fixture.RemoteFile("shl/new.csv"));
        Assert.Null(fixture.RemoteFile("shl/old.csv"));
        Assert.Equal("Keep this README", fixture.RemoteFile("README.md"));
        Assert.Equal("Keep this file", fixture.RemoteFile("outside.txt"));
        Assert.Equal(
            ["shl/new.csv", "shl/updated.csv"],
            ReadManifest(fixture).Files);
        Assert.Equal(2, fixture.CommitCount);
    }

    [Fact]
    public async Task NonCsvFilesAreNotDownloadedAndRemoveOnlyManagedFilesAfterSuccessfulSync() {
        await using var fixture = new ArchiveFixture();
        string[] excluded = ["game.HTML", "players.xml", "playbyplay.TxT", "snapshot.sth",
            "archive.zip", "styles.css", "data.bin", "extensionless"];
        fixture.Seed([
            ("README.md", "untouched"), ("notes.txt", "unmanaged"),
            ..excluded.Select(path => (path, "old excluded file")),
            ("game.csv", "old game"),
            (GameArchiveManifest.FileName, ManifestJson([..excluded, "game.csv"]))]);
        var previousHead = fixture.Head;
        fixture.Source.SetFiles([..excluded.Select(path => (path, "ignored file")), ("game.csv", "updated game")]);
        fixture.Source.FailDownload = "game.csv";

        await Assert.ThrowsAsync<HttpRequestException>(() => fixture.SynchronizeAsync());

        Assert.Equal(previousHead, fixture.Head);
        foreach (var path in excluded)
            Assert.Equal("old excluded file", fixture.RemoteFile(path));
        fixture.Source.FailDownload = "game.HTML";
        fixture.Source.RequestedPaths.Clear();

        await fixture.SynchronizeAsync();

        foreach (var path in excluded) {
            Assert.Null(fixture.RemoteFile(path));
            Assert.Equal("old excluded file", fixture.HistoricalFile(previousHead, path));
            Assert.DoesNotContain(path, fixture.Source.RequestedPaths);
        }
        Assert.Equal("updated game", fixture.RemoteFile("game.csv"));
        Assert.Equal("untouched", fixture.RemoteFile("README.md"));
        Assert.Equal("unmanaged", fixture.RemoteFile("notes.txt"));
        Assert.Equal("game.csv", Assert.Single(ReadManifest(fixture).Files));
        var head = fixture.Head;
        fixture.Source.SetFiles(("game.csv", "updated game"), ("archive.zip", "changed ignored save"));

        await fixture.SynchronizeAsync();

        Assert.Equal(head, fixture.Head);
    }

    [Fact]
    public async Task IdenticalSecondRunDoesNotCommitOrPush() {
        await using var fixture = new ArchiveFixture();
        fixture.Seed(("README.md", "untouched"));
        fixture.Source.SetFiles(("game.csv", "same"));
        await fixture.SynchronizeAsync();
        var head = fixture.Head;

        await fixture.SynchronizeAsync();

        Assert.Equal(head, fixture.Head);
        Assert.Equal(2, fixture.CommitCount);
        Assert.Equal("untouched", fixture.RemoteFile("README.md"));
    }

    [Fact]
    public async Task ArchiveStagesFilesEvenWhenRemoteGitignoreMatchesThem() {
        await using var fixture = new ArchiveFixture();
        fixture.Seed(("README.md", "untouched"), (".gitignore", "*.csv\n"));
        fixture.Source.SetFiles(("shl/game.csv", "archived"));

        await fixture.SynchronizeAsync();

        Assert.Equal("archived", fixture.RemoteFile("shl/game.csv"));
        Assert.Equal("*.csv\n", fixture.RemoteFile(".gitignore"));
    }

    [Fact]
    public async Task EmptyInventoryRemovesOnlyPreviouslyManagedFiles() {
        await using var fixture = new ArchiveFixture();
        fixture.Seed(("README.md", "untouched"), ("notes.txt", "unmanaged"),
            ("old.csv", "old"), (GameArchiveManifest.FileName, ManifestJson("old.csv")));
        fixture.Source.SetFiles();

        await fixture.SynchronizeAsync();

        Assert.Null(fixture.RemoteFile("old.csv"));
        Assert.Empty(ReadManifest(fixture).Files);
        Assert.Equal("untouched", fixture.RemoteFile("README.md"));
        Assert.Equal("unmanaged", fixture.RemoteFile("notes.txt"));
    }

    [Fact]
    public async Task FailedDownloadLeavesRemoteAtPreviousSnapshot() {
        await using var fixture = new ArchiveFixture();
        fixture.Seed(("README.md", "untouched"));
        fixture.Source.SetFiles(("old.csv", "old"));
        await fixture.SynchronizeAsync();
        var head = fixture.Head;
        fixture.Source.SetFiles(("new.csv", "new"), ("old.csv", "changed"));
        fixture.Source.FailDownload = "new.csv";

        await Assert.ThrowsAsync<HttpRequestException>(() => fixture.SynchronizeAsync());

        Assert.Equal(head, fixture.Head);
        Assert.Equal("old", fixture.RemoteFile("old.csv"));
        Assert.Null(fixture.RemoteFile("new.csv"));
        Assert.Equal("old.csv", Assert.Single(ReadManifest(fixture).Files));
    }

    [Fact]
    public async Task CancelledDownloadLeavesRemoteAtPreviousSnapshot() {
        await using var fixture = new ArchiveFixture();
        fixture.Seed(("README.md", "untouched"));
        fixture.Source.SetFiles(("old.csv", "old"));
        await fixture.SynchronizeAsync();
        var head = fixture.Head;
        fixture.Source.SetFiles(("old.csv", "changed"));
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        fixture.Source.OnDownload = cancellation.Cancel;

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => fixture.SynchronizeWithCancellationAsync(cancellation.Token));

        Assert.Equal(head, fixture.Head);
        Assert.Equal("old", fixture.RemoteFile("old.csv"));
    }

    [Fact]
    public async Task TruncatedDownloadLeavesRemoteAtPreviousSnapshot() {
        await using var fixture = new ArchiveFixture();
        fixture.Seed(("README.md", "untouched"));
        fixture.Source.SetFiles(("old.csv", "old"));
        await fixture.SynchronizeAsync();
        var head = fixture.Head;
        fixture.Source.SetFiles(("old.csv", "changed"), ("new.csv", "incomplete"));
        fixture.Source.TruncateDownload = "new.csv";

        await Assert.ThrowsAsync<InvalidDataException>(() => fixture.SynchronizeAsync());

        Assert.Equal(head, fixture.Head);
        Assert.Equal("old", fixture.RemoteFile("old.csv"));
        Assert.Null(fixture.RemoteFile("new.csv"));
        Assert.Equal("old.csv", Assert.Single(ReadManifest(fixture).Files));
    }

    [Fact]
    public async Task UnmanagedFileAtIncomingPathAbortsWithoutOverwritingOrPushing() {
        await using var fixture = new ArchiveFixture();
        fixture.Seed(("README.md", "untouched"), ("shl/foreign.csv", "unmanaged"));
        var head = fixture.Head;
        fixture.Source.SetFiles(("shl/foreign.csv", "incoming"));

        await Assert.ThrowsAsync<InvalidDataException>(() => fixture.SynchronizeAsync());

        Assert.Equal(head, fixture.Head);
        Assert.Equal("unmanaged", fixture.RemoteFile("shl/foreign.csv"));
        Assert.Null(fixture.RemoteFile(GameArchiveManifest.FileName));
    }

    [Fact]
    public async Task InvalidManifestAbortsBeforeMutatingRemote() {
        await using var fixture = new ArchiveFixture();
        fixture.Seed(("README.md", "untouched"), (GameArchiveManifest.FileName, ManifestJson("../README.md")));
        var head = fixture.Head;
        fixture.Source.SetFiles(("game.csv", "incoming"));

        await Assert.ThrowsAsync<InvalidDataException>(() => fixture.SynchronizeAsync());

        Assert.Equal(head, fixture.Head);
        Assert.Null(fixture.RemoteFile("game.csv"));
    }

    [Fact]
    public async Task CaseCollidingManifestAbortsBeforeMutatingRemote() {
        await using var fixture = new ArchiveFixture();
        fixture.Seed(("README.md", "untouched"), (GameArchiveManifest.FileName, ManifestJson("shl/game.csv", "shl/GAME.csv")));
        var head = fixture.Head;
        fixture.Source.SetFiles(("game.csv", "incoming"));

        await Assert.ThrowsAsync<InvalidDataException>(() => fixture.SynchronizeAsync());

        Assert.Equal(head, fixture.Head);
        Assert.Null(fixture.RemoteFile("game.csv"));
    }

    [Fact]
    public async Task UnsupportedManifestVersionAbortsWithoutPublishing() {
        await using var fixture = new ArchiveFixture();
        var manifest = new GameArchiveManifest(GameArchiveManifest.SchemaUri, 2, [], []);
        fixture.Seed(("README.md", "untouched"),
            (GameArchiveManifest.FileName, JsonSerializer.Serialize(manifest, new JsonSerializerOptions(JsonSerializerDefaults.Web))));
        var head = fixture.Head;
        fixture.Source.SetFiles(("game.csv", "incoming"));

        await Assert.ThrowsAsync<InvalidDataException>(() => fixture.SynchronizeAsync());

        Assert.Equal(head, fixture.Head);
        Assert.Null(fixture.RemoteFile("game.csv"));
    }

    [Fact]
    public async Task ManagedGitCloneFailureDoesNotExposeCredential() {
        await using var fixture = new ArchiveFixture();
        const string password = "archive-secret-9387";
        var git = new GameArchiveGit("bot", password);
        var remote = await fixture.GetGitRemoteAsync();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            git.Clone(remote.Replace("/remote.git", "/missing.git", StringComparison.Ordinal),
                Path.Combine(fixture.DirectoryPath, "missing-clone"), TestContext.Current.CancellationToken));

        Assert.DoesNotContain(password, exception.ToString(), StringComparison.Ordinal);
        Assert.False(File.Exists(Path.Combine(fixture.DirectoryPath, "archive-askpass.cmd")));
    }

    [Fact]
    public async Task TemporaryCloneUsesDepthTwoAndPushPreservesFullRemoteHistory() {
        await using var fixture = new ArchiveFixture();
        fixture.Seed(("README.md", "original"));
        var originalHead = fixture.Head;
        fixture.Seed(("history.txt", "second"));
        fixture.Seed(("history.txt", "third"));
        fixture.Seed(("history.txt", "fourth"));
        var remote = await fixture.GetGitRemoteAsync();
        var checkout = Path.Combine(fixture.DirectoryPath, "shallow-checkout");
        var git = new GameArchiveGit("bot", "test-password");
        using (var repository = git.Clone(remote, checkout, TestContext.Current.CancellationToken)) {
            Assert.True(repository.Info.IsShallow);
            Assert.Equal(2, repository.Commits.Count());
            Assert.Equal(fixture.Head, repository.Head.Tip.Sha);
            Assert.Equal("fourth", File.ReadAllText(Path.Combine(checkout, "history.txt")));
        }

        fixture.Source.SetFiles(("game.csv", "new snapshot"));
        await fixture.SynchronizeAsync();

        Assert.Equal(5, fixture.CommitCount);
        Assert.Equal("original", fixture.HistoricalFile(originalHead, "README.md"));
        Assert.Equal("new snapshot", fixture.RemoteFile("game.csv"));
        using var nextClone = git.Clone(remote, Path.Combine(fixture.DirectoryPath, "next-checkout"),
            TestContext.Current.CancellationToken);
        Assert.True(nextClone.Info.IsShallow);
        Assert.Equal(2, nextClone.Commits.Count());
        Assert.Equal(fixture.Head, nextClone.Head.Tip.Sha);
    }

    private sealed class ArchiveFixture : IAsyncDisposable {
        private readonly string bare;
        private readonly string seed;
        private readonly HttpClient client;
        private readonly GameArchiveSynchronizer synchronizer;
        private readonly GameArchiveOptions options;
        private GameArchiveGitHttpServer? gitServer;

        public ArchiveFixture() {
            DirectoryPath = Path.Combine(Path.GetTempPath(), $"shuttle-archive-test-{Guid.NewGuid():N}");
            Directory.CreateDirectory(DirectoryPath);
            bare = Path.Combine(DirectoryPath, "remote.git");
            seed = Path.Combine(DirectoryPath, "seed");
            Git(DirectoryPath, "init", "--bare", bare);
            Git(DirectoryPath, $"--git-dir={bare}", "config", "http.receivepack", "true");
            Git(DirectoryPath, "init", "-b", "main", seed);
            Git(seed, "remote", "add", "origin", new Uri(bare).AbsoluteUri);
            Source = new ArchiveSource();
            client = new HttpClient(Source);
            options = new GameArchiveOptions {
                SourceUrl = "http://localhost/games/",
                RemoteUrl = new Uri(bare).AbsoluteUri,
            };
            synchronizer = new GameArchiveSynchronizer(
                new GameArchiveCrawler(client),
                new FakeCredentialProvider(),
                Options.Create(options),
                new DevelopmentEnvironment(),
                NullLogger<GameArchiveSynchronizer>.Instance);
        }

        public string DirectoryPath { get; }
        public ArchiveSource Source { get; }
        public string Head => Git(DirectoryPath, $"--git-dir={bare}", "rev-parse", "refs/heads/main");
        public int CommitCount => int.Parse(Git(DirectoryPath, $"--git-dir={bare}", "rev-list", "--count", "refs/heads/main"));
        public string HistoricalFile(string commit, string path) => Git(DirectoryPath, $"--git-dir={bare}", "show", $"{commit}:{path}");

        public void Seed(params (string Path, string Content)[] files) {
            foreach (var (path, content) in files) {
                var target = Path.Combine(seed, path.Replace('/', Path.DirectorySeparatorChar));
                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                File.WriteAllText(target, content);
            }
            Git(seed, "add", ".");
            Git(seed, "-c", "user.name=test", "-c", "user.email=test@example.invalid", "commit", "-m", "seed");
            Git(seed, "push", "-u", "origin", "main");
            Git(DirectoryPath, $"--git-dir={bare}", "symbolic-ref", "HEAD", "refs/heads/main");
        }

        public string? RemoteFile(string path) {
            var start = GitStart(DirectoryPath, $"--git-dir={bare}", "show", $"refs/heads/main:{path}");
            using var process = Process.Start(start)!;
            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();
            process.WaitForExit();
            if (process.ExitCode != 0) {
                Assert.Contains("does not exist", error, StringComparison.OrdinalIgnoreCase);
                return null;
            }
            return output;
        }

        public async Task<string> GetGitRemoteAsync() {
            gitServer ??= await GameArchiveGitHttpServer.StartAsync(DirectoryPath, TestContext.Current.CancellationToken);
            options.RemoteUrl = gitServer.RemoteUrl;
            return options.RemoteUrl;
        }

        public async Task SynchronizeAsync() {
            await GetGitRemoteAsync();
            await synchronizer.SynchronizeAsync(TestContext.Current.CancellationToken);
        }

        public async Task SynchronizeWithCancellationAsync(CancellationToken cancellationToken) {
            await GetGitRemoteAsync();
            await synchronizer.SynchronizeAsync(cancellationToken);
        }

        public async ValueTask DisposeAsync() {
            if (gitServer is not null)
                await gitServer.DisposeAsync();
            client.Dispose();
            foreach (var path in Directory.EnumerateFiles(DirectoryPath, "*", SearchOption.AllDirectories))
                File.SetAttributes(path, FileAttributes.Normal);
            Directory.Delete(DirectoryPath, recursive: true);
        }

        private static string Git(string directory, params string[] args) {
            using var process = Process.Start(GitStart(directory, args))!;
            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();
            process.WaitForExit();
            Assert.True(process.ExitCode == 0, $"git {string.Join(' ', args)} failed: {error}");
            return output.Trim();
        }

        private static ProcessStartInfo GitStart(string directory, params string[] args) {
            var start = new ProcessStartInfo("git") {
                WorkingDirectory = directory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            foreach (var arg in args)
                start.ArgumentList.Add(arg);
            start.Environment["GIT_CONFIG_GLOBAL"] = Path.Combine(directory, "empty-gitconfig");
            start.Environment["GIT_CONFIG_NOSYSTEM"] = "1";
            start.Environment["GIT_TERMINAL_PROMPT"] = "0";
            return start;
        }
    }

    private sealed class ArchiveSource : HttpMessageHandler {
        private readonly Dictionary<string, string> files = new(StringComparer.Ordinal);

        public string? FailDownload { get; set; }
        public string? TruncateDownload { get; set; }
        public int RequestCount { get; private set; }
        public Action? OnDownload { get; set; }
        public Dictionary<string, string> DirectoryPages { get; } = new(StringComparer.Ordinal);
        public List<string> RequestedPaths { get; } = [];

        public void SetFiles(params (string Path, string Content)[] entries) {
            files.Clear();
            foreach (var (path, content) in entries)
                files.Add(path, content);
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
            RequestCount++;
            var path = Uri.UnescapeDataString(request.RequestUri!.AbsolutePath);
            var relative = path["/games/".Length..];
            RequestedPaths.Add(relative);
            if (DirectoryPages.TryGetValue(relative, out var page))
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) {
                    Content = new StringContent(page, Encoding.UTF8, "text/html"),
                });
            if (relative == FailDownload)
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
            if (files.TryGetValue(relative, out var body)) {
                OnDownload?.Invoke();
                cancellationToken.ThrowIfCancellationRequested();
                var response = new HttpResponseMessage(HttpStatusCode.OK) {
                    Content = new StringContent(body, Encoding.UTF8, "application/octet-stream"),
                };
                if (relative == TruncateDownload)
                    response.Content.Headers.ContentLength = Encoding.UTF8.GetByteCount(body) + 100;
                return Task.FromResult(response);
            }
            var children = files.Keys
                .Where(file => file.StartsWith(relative, StringComparison.Ordinal))
                .Select(file => file[relative.Length..].Split('/')[0])
                .Distinct(StringComparer.Ordinal)
                .Order(StringComparer.Ordinal)
                .Select(name => files.ContainsKey(relative + name) ? name : name + "/");
            var links = string.Concat(children.Select(name => $"<tr><td><a href=\"{name}\">{name}</a></td></tr>"));
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) {
                Content = new StringContent(
                    $"<html><head><title>Index of {path.TrimEnd('/')}</title></head><body><table>{links}</table></body></html>",
                    Encoding.UTF8, "text/html"),
            });
        }
    }

    private sealed class FakeCredentialProvider : IGameArchiveCredentialProvider {
        public Task<string> GetPasswordAsync(string secretName, CancellationToken cancellationToken) =>
            Task.FromResult("local-test-only");
    }

    private static GameArchiveManifest ReadManifest(ArchiveFixture fixture) =>
        Assert.IsType<GameArchiveManifest>(JsonSerializer.Deserialize<GameArchiveManifest>(
            Assert.IsType<string>(fixture.RemoteFile(GameArchiveManifest.FileName)),
            new JsonSerializerOptions(JsonSerializerDefaults.Web)));

    private static string ManifestJson(params string[] files) =>
        JsonSerializer.Serialize(GameArchiveManifest.Create(files, []), new JsonSerializerOptions(JsonSerializerDefaults.Web));

    private sealed class MissingCredentialProvider : IGameArchiveCredentialProvider {
        public Task<string> GetPasswordAsync(string secretName, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Missing production credential.");
    }

    private sealed class DevelopmentEnvironment : IHostEnvironment {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "ArchiveTests";
        public string ContentRootPath { get; set; } = Environment.CurrentDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
