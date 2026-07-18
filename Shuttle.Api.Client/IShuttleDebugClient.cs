using Refit;
using Shuttle.Models.Users;

namespace Shuttle.Api.Client;

/// <summary>
/// Typed Refit client for the Shuttle backend API (<c>Shuttle.Api</c>) development-only debug
/// endpoints. These endpoints require authentication and are only routable when the API itself is
/// running in the Development environment; against any other environment they return HTTP 404.
/// <para>
/// The client must be configured with an auth message handler that attaches the caller's access
/// token, otherwise the backend rejects the request with 401.
/// </para>
/// </summary>
public interface IShuttleDebugClient {
    /// <summary>
    /// Returns the roles the API sees for the currently authenticated caller, as resolved from the
    /// validated bearer token on the server. Complements the client-side view of roles parsed from
    /// the same token.
    /// </summary>
    /// <param name="token">A cancellation token.</param>
    [Get("/debug/roles")]
    Task<IReadOnlyList<string>> GetServerRoles(CancellationToken token = default);

    /// <summary>
    /// Scrapes the given forum member's profile page on the server (via the forum client) and returns
    /// the Discord username listed on it, if any. Development-only; the API returns 404 outside the
    /// Development environment.
    /// </summary>
    /// <param name="userId">The forum member id to look up.</param>
    /// <param name="token">A cancellation token.</param>
    [Get("/debug/users/{userId}/discord")]
    Task<DiscordUsernameResult> GetDiscordUsername(int userId, CancellationToken token = default);

    /// <summary>
    /// Calls the admin-gated <c>GET /admin/ping</c> endpoint. Unlike the other members this endpoint
    /// is not development-only; it succeeds only when the caller holds the <c>Shuttle.Admin</c> app
    /// role on the API. Otherwise the backend responds 401 (unauthenticated) or 403 (authenticated
    /// without the role), which Refit surfaces as an <see cref="ApiException"/>.
    /// </summary>
    /// <param name="token">A cancellation token.</param>
    [Get("/admin/ping")]
    Task<AdminPingResponse> PingAdmin(CancellationToken token = default);
}

/// <summary>Response payload from the admin ping endpoint (<c>GET /admin/ping</c>).</summary>
/// <param name="Message">Constant marker returned on success (<c>"pong"</c>).</param>
/// <param name="User">Display name of the authenticated caller, if available.</param>
public sealed record AdminPingResponse(string Message, string? User);
