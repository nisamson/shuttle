using AngleSharp.Html.Parser;

namespace Shuttle.GameArchive;

public sealed class GameArchiveCrawler(HttpClient client) {
    private readonly HtmlParser parser = new();

    public async Task<GameArchiveInventory> InventoryAsync(Uri root, CancellationToken cancellationToken) {
        if (!root.IsAbsoluteUri || !root.AbsolutePath.EndsWith('/') || root.UserInfo.Length > 0
            || root.Query.Length > 0 || root.Fragment.Length > 0)
            throw new ArgumentException("The archive source must be an absolute directory URL.", nameof(root));

        var directories = new Queue<Uri>();
        var visited = new HashSet<string>(StringComparer.Ordinal);
        var files = new Dictionary<string, Uri>(StringComparer.Ordinal);
        var skippedDirectories = new List<GameArchiveSkippedDirectory>();
        var localNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        directories.Enqueue(root);

        while (directories.TryDequeue(out var directory)) {
            if (!visited.Add(directory.AbsoluteUri))
                continue;

            using var response = await GetAsync(directory, root, cancellationToken);
            response.EnsureSuccessStatusCode();
            if (response.Content.Headers.ContentType?.MediaType is not "text/html")
                throw new InvalidDataException($"Expected an HTML directory listing at {directory}.");
            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var document = await parser.ParseDocumentAsync(stream, cancellationToken);
            if (document.Title != $"Index of {Uri.UnescapeDataString(directory.AbsolutePath.TrimEnd('/'))}"
                || document.QuerySelector("table") is null) {
                var relative = Uri.UnescapeDataString(root.MakeRelativeUri(directory).ToString());
                skippedDirectories.Add(new GameArchiveSkippedDirectory(
                    relative.Length == 0 ? "." : relative,
                    directory.AbsoluteUri, document.QuerySelector("title") is null ? null : document.Title,
                    GameArchiveSkippedDirectory.NonApacheReason));
                continue;
            }
            foreach (var anchor in document.QuerySelectorAll("table tr td a[href]")) {
                var href = anchor.GetAttribute("href");
                if (string.IsNullOrEmpty(href) || href.StartsWith('?') || href.StartsWith('#'))
                    continue;
                var target = new Uri(directory, href);
                if (!WithinRoot(target, root) || target.Query.Length > 0 || target.Fragment.Length > 0)
                    continue;
                if (target.AbsoluteUri == directory.AbsoluteUri)
                    continue;
                var relative = Uri.UnescapeDataString(root.MakeRelativeUri(target).ToString());
                var isDirectory = target.AbsolutePath.EndsWith('/');
                if (isDirectory && target.AbsolutePath.Length <= directory.AbsolutePath.Length)
                    continue;
                if (!ValidPath(relative, isDirectory))
                    throw new InvalidDataException($"Unsafe archive entry {target}.");
                if (isDirectory) {
                    if (!localNames.Add(relative.TrimEnd('/')))
                        throw new InvalidDataException($"Duplicate or case-colliding archive directory {relative}.");
                    directories.Enqueue(target);
                } else {
                    if (!localNames.Add(relative))
                        throw new InvalidDataException($"Duplicate or case-colliding archive entry {relative}.");
                    if (relative.EndsWith(".html", StringComparison.OrdinalIgnoreCase)
                        || relative.EndsWith(".xml", StringComparison.OrdinalIgnoreCase)
                        || relative.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
                        continue;
                    files.Add(relative, target);
                }
            }
        }

        return new GameArchiveInventory(files, skippedDirectories.OrderBy(entry => entry.Path, StringComparer.Ordinal).ToArray());
    }

    public async Task DownloadAsync(Uri url, Uri root, string destination, CancellationToken cancellationToken) {
        using var response = await GetAsync(url, root, cancellationToken);
        response.EnsureSuccessStatusCode();
        if (response.Content.Headers.ContentLength > GameArchiveOptions.MaxGitFileSize)
            throw new InvalidDataException($"GitHub's 100 MiB Git file limit exceeded: {url}");
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        await using var output = new FileStream(destination, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        await using var input = await response.Content.ReadAsStreamAsync(cancellationToken);
        var buffer = new byte[81920];
        long length = 0;
        int read;
        while ((read = await input.ReadAsync(buffer, cancellationToken)) != 0) {
            length += read;
            if (length > GameArchiveOptions.MaxGitFileSize)
                throw new InvalidDataException($"GitHub's 100 MiB Git file limit exceeded: {url}");
            await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
        }
        if (response.Content.Headers.ContentLength is { } expected && length != expected)
            throw new InvalidDataException($"Incomplete download from {url}: expected {expected} bytes, got {length}.");
    }

    private async Task<HttpResponseMessage> GetAsync(Uri url, Uri root, CancellationToken cancellationToken) {
        for (var redirects = 0; redirects < 6; redirects++) {
            if (!WithinRoot(url, root))
                throw new InvalidDataException($"Archive redirect left the source tree: {url}");
            HttpResponseMessage? response = null;
            for (var attempt = 0; attempt < 3; attempt++) {
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                try {
                    response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                } catch (HttpRequestException) when (attempt < 2) {
                    await Task.Delay(TimeSpan.FromSeconds(1 << attempt), cancellationToken);
                    continue;
                }
                if ((response.StatusCode == System.Net.HttpStatusCode.TooManyRequests
                     || (int)response.StatusCode >= 500) && attempt < 2) {
                    response.Dispose();
                    await Task.Delay(TimeSpan.FromSeconds(1 << attempt), cancellationToken);
                    continue;
                }
                break;
            }
            if (response is null)
                throw new InvalidOperationException("The archive source returned no response.");
            if ((int)response.StatusCode is >= 300 and < 400 && response.Headers.Location is { } location) {
                response.Dispose();
                url = new Uri(url, location);
                continue;
            }
            return response;
        }
        throw new InvalidDataException($"Too many archive redirects from {url}.");
    }

    private static bool WithinRoot(Uri url, Uri root) =>
        url.Scheme == root.Scheme && url.Host == root.Host && url.Port == root.Port
        && url.AbsolutePath.StartsWith(root.AbsolutePath, StringComparison.Ordinal)
        && url.UserInfo.Length == 0;

    public static bool IsValidFilePath(string relative) => ValidPath(relative, false);
    public static bool IsValidDirectoryPath(string relative) => relative == "." || ValidPath(relative, true);

    private static bool ValidPath(string relative, bool directory) {
        if (relative.Length == 0 || relative.StartsWith('/') || relative.EndsWith('/') != directory)
            return false;
        var parts = relative.TrimEnd('/').Split('/');
        if (parts.Length == 0 || parts.Any(part =>
                part.Length == 0 || part is "." or ".." ||
                part.IndexOfAny(['\\', ':', '*', '?', '"', '<', '>', '|']) >= 0 ||
                part.Any(char.IsControl) || part.EndsWith('.') || part.EndsWith(' ') ||
                part.Equals(".git", StringComparison.OrdinalIgnoreCase) ||
                part.Equals("README.md", StringComparison.OrdinalIgnoreCase) && parts.Length == 1 ||
                part.Split('.')[0].ToUpperInvariant() is "CON" or "PRN" or "AUX" or "NUL" or
                    "COM1" or "COM2" or "COM3" or "COM4" or "COM5" or "COM6" or "COM7" or "COM8" or "COM9" or
                    "LPT1" or "LPT2" or "LPT3" or "LPT4" or "LPT5" or "LPT6" or "LPT7" or "LPT8" or "LPT9"))
            return false;
        if (parts[0].Equals(".github", StringComparison.OrdinalIgnoreCase)
            || parts[0].Equals(GameArchiveManifest.FileName, StringComparison.OrdinalIgnoreCase)
            || parts[0].Equals(".gitattributes", StringComparison.OrdinalIgnoreCase)
            || parts[0].Equals(".gitmodules", StringComparison.OrdinalIgnoreCase))
            return false;
        return directory || !relative.EndsWith('/');
    }
}
