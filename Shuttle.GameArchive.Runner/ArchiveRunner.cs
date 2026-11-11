using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Shuttle.GameArchive.Runner;

public static class ArchiveRunner {
    public static void ValidateConfiguration(IConfiguration configuration, IHostEnvironment environment) {
        if (!environment.IsDevelopment())
            throw new InvalidOperationException("The local archive runner requires the Development environment.");
        RequireLoopbackUrl(configuration["Archive:RemoteUrl"], "Archive:RemoteUrl");
        RequireLoopbackUrl(configuration["Archive:VaultUri"]
            ?? configuration.GetConnectionString("archivevault")
            ?? configuration.GetConnectionString("archive-vault"), "Archive:VaultUri");
        var source = configuration["Archive:SourceUrl"] ?? new GameArchiveOptions().SourceUrl;
        if (!Uri.TryCreate(source, UriKind.Absolute, out var root)
            || root.Scheme != Uri.UriSchemeHttps && !(root.IsLoopback && root.Scheme == Uri.UriSchemeHttp)
            || !root.AbsolutePath.EndsWith('/') || root.UserInfo.Length > 0
            || root.Query.Length > 0 || root.Fragment.Length > 0)
            throw new InvalidOperationException("Archive:SourceUrl must be an HTTPS directory URL or a loopback HTTP fixture URL.");
    }

    public static async Task<int> RunOnceAsync(
        Func<CancellationToken, Task> synchronize, ILogger logger, CancellationToken cancellationToken) {
        try {
            cancellationToken.ThrowIfCancellationRequested();
            await synchronize(cancellationToken);
            logger.LogInformation("One-shot archive sync completed");
            return 0;
        } catch (OperationCanceledException) {
            logger.LogWarning("One-shot archive sync cancelled");
            return 130;
        } catch (Exception ex) {
            logger.LogError(ex, "One-shot archive sync failed");
            return 1;
        }
    }

    private static void RequireLoopbackUrl(string? value, string setting) {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || !uri.IsLoopback
            || uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps
            || uri.UserInfo.Length > 0 || uri.Query.Length > 0 || uri.Fragment.Length > 0)
            throw new InvalidOperationException($"{setting} must be a loopback HTTP(S) URL without credentials, query, or fragment.");
    }
}
