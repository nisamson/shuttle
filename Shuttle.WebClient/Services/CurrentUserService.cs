using Shuttle.Api.Client;
using Shuttle.Models.Users;

namespace Shuttle.WebClient.Services;

/// <summary>
/// Resolves and caches the authenticated caller's own <c>ShuttleUser</c> account (<c>GET /users/me</c>).
/// Scouting UI needs the caller's <c>ShuttleUser</c> id — not just their Entra claims — to tell which
/// team members and comments belong to the current user (authorship is keyed on the account id).
/// </summary>
public interface ICurrentUserService {
    /// <summary>
    /// Returns the caller's account, fetching it at most once and caching the result. Returns
    /// <c>null</c> if the caller is unauthenticated or the account could not be loaded.
    /// </summary>
    Task<CurrentUser?> GetAsync(CancellationToken token = default);
}

/// <inheritdoc cref="ICurrentUserService"/>
public sealed class CurrentUserService : ICurrentUserService, IDisposable {
    private readonly IShuttleUserClient client;
    private readonly ILogger<CurrentUserService> logger;
    private readonly SemaphoreSlim loadLock = new(1, 1);

    private CurrentUser? cache;

    public CurrentUserService(IShuttleUserClient client, ILogger<CurrentUserService> logger) {
        this.client = client;
        this.logger = logger;
    }

    public async Task<CurrentUser?> GetAsync(CancellationToken token = default) {
        if (cache is not null) {
            return cache;
        }

        await loadLock.WaitAsync(token);
        try {
            if (cache is not null) {
                return cache;
            }

            cache = await client.GetCurrentUser(token);
            return cache;
        } catch (Exception ex) {
            logger.LogWarning(ex, "Failed to resolve the current user account");
            return null;
        } finally {
            loadLock.Release();
        }
    }

    public void Dispose() => loadLock.Dispose();
}
