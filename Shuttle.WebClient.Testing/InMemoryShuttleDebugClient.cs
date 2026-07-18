using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;
using Shuttle.Api.Client;
using Shuttle.Models.Users;

namespace Shuttle.WebClient.Testing;

/// <summary>
/// In-memory <see cref="IShuttleDebugClient"/> that reports the (fake) caller's roles as the
/// "server" would, without any HTTP, backend, or Azure dependency. Reads roles from the supplied
/// <see cref="AuthenticationStateProvider"/> so the fake backend's server-side view matches the fake
/// identity; an anonymous or absent provider yields no roles.
/// </summary>
public sealed class InMemoryShuttleDebugClient : IShuttleDebugClient {
    private readonly AuthenticationStateProvider? authProvider;

    public InMemoryShuttleDebugClient(AuthenticationStateProvider? authProvider = null) {
        this.authProvider = authProvider;
    }

    public async Task<IReadOnlyList<string>> GetServerRoles(CancellationToken token = default) {
        if (authProvider is null) {
            return [];
        }

        var state = await authProvider.GetAuthenticationStateAsync();
        var identity = state.User.Identity as ClaimsIdentity;
        var roleClaimType = identity?.RoleClaimType ?? ClaimTypes.Role;

        return state.User.FindAll(roleClaimType)
            .Select(c => c.Value)
            .OrderBy(r => r, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public async Task<AdminPingResponse> PingAdmin(CancellationToken token = default) {
        // Mirror the server's [Authorize(Roles = "Shuttle.Admin")] gate against the fake identity.
        var roles = await GetServerRoles(token);
        if (!roles.Contains("Shuttle.Admin")) {
            throw new UnauthorizedAccessException("The fake caller does not hold the Shuttle.Admin role.");
        }

        var state = authProvider is null ? null : await authProvider.GetAuthenticationStateAsync();
        return new AdminPingResponse("pong", state?.User.Identity?.Name);
    }

    public Task<DiscordUsernameResult> GetDiscordUsername(int userId, CancellationToken token = default) {
        // Deterministic offline stand-in for the forum scrape: non-positive ids resolve to "not
        // found" (null), everything else yields a stable fake username.
        var username = userId > 0 ? $"discorduser{userId}" : null;
        return Task.FromResult(new DiscordUsernameResult(userId, username));
    }
}
