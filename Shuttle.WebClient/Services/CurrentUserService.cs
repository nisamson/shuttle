using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Shuttle.Api.Client;
using Shuttle.Models.Users;
using Shuttle.WebClient.Extensions;

namespace Shuttle.WebClient.Services;

/// <summary>
/// Resolves and caches the authenticated caller's own <c>ShuttleUser</c> account (<c>GET /users/me</c>).
/// Scouting UI needs the caller's <c>ShuttleUser</c> id — not just their Entra claims — to tell which
/// team members and comments belong to the current user (authorship is keyed on the account id).
/// Because the endpoint creates the account lazily on first access, hitting it right after sign-in
/// also guarantees the caller's account is fully initialized whenever they log in.
/// </summary>
public interface ICurrentUserService {
    /// <summary>
    /// Returns the caller's account, fetching it at most once per identity and caching the result.
    /// Returns <c>null</c> if the caller is unauthenticated or the account could not be loaded.
    /// </summary>
    Task<CurrentUser?> GetAsync(CancellationToken token = default);

    /// <summary>
    /// Ensures the caller's account exists by hitting <c>GET /users/me</c> for the current
    /// authentication state. Called at startup so an already-signed-in user (e.g. returning from the
    /// MSAL redirect) is initialized without waiting for a page that consumes the account.
    /// </summary>
    Task EnsureInitializedAsync(CancellationToken token = default);
}

/// <inheritdoc cref="ICurrentUserService"/>
public sealed class CurrentUserService : ICurrentUserService, IDisposable {
    private readonly IShuttleUserClient client;
    private readonly AuthenticationStateProvider authState;
    private readonly ILogger<CurrentUserService> logger;
    private readonly SemaphoreSlim loadLock = new(1, 1);

    private CurrentUser? cache;
    private string? cachedForUserId;

    public CurrentUserService(
        IShuttleUserClient client,
        AuthenticationStateProvider authState,
        ILogger<CurrentUserService> logger) {
        this.client = client;
        this.authState = authState;
        this.logger = logger;

        // Re-initialize (or clear) the account whenever the caller signs in, out, or switches
        // identities during the session (e.g. silent token renewal or popup sign-in).
        authState.AuthenticationStateChanged += OnAuthenticationStateChanged;
    }

    public async Task<CurrentUser?> GetAsync(CancellationToken token = default) {
        var state = await authState.GetAuthenticationStateAsync();
        return await SyncToPrincipalAsync(state.User, token);
    }

    public async Task EnsureInitializedAsync(CancellationToken token = default) {
        var state = await authState.GetAuthenticationStateAsync();
        await SyncToPrincipalAsync(state.User, token);
    }

    private async Task<CurrentUser?> SyncToPrincipalAsync(ClaimsPrincipal user, CancellationToken token) {
        var userId = user.Identity?.IsAuthenticated == true ? user.GetUserId() : null;

        await loadLock.WaitAsync(token);
        try {
            if (userId is null) {
                // Signed out: drop any cached account so a subsequent user can't observe it.
                cache = null;
                cachedForUserId = null;
                return null;
            }

            if (cache is not null && cachedForUserId == userId) {
                return cache;
            }

            // First load for this identity: fetch (and lazily create) the caller's account.
            cache = await client.GetCurrentUser(token);
            cachedForUserId = userId;
            return cache;
        } catch (Exception ex) {
            logger.LogWarning(ex, "Failed to resolve the current user account");
            return null;
        } finally {
            loadLock.Release();
        }
    }

    private void OnAuthenticationStateChanged(Task<AuthenticationState> task) =>
        _ = HandleAuthenticationStateChangedAsync(task);

    private async Task HandleAuthenticationStateChangedAsync(Task<AuthenticationState> task) {
        try {
            var state = await task;
            await SyncToPrincipalAsync(state.User, CancellationToken.None);
        } catch (Exception ex) {
            logger.LogWarning(ex, "Failed to initialize the account after an authentication state change");
        }
    }

    public void Dispose() {
        authState.AuthenticationStateChanged -= OnAuthenticationStateChanged;
        loadLock.Dispose();
    }
}
