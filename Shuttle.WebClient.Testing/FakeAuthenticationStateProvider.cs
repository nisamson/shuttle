using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace Shuttle.WebClient.Testing;

/// <summary>
/// An <see cref="AuthenticationStateProvider"/> that returns a fixed, fake identity built from
/// <see cref="FakeAuthOptions"/> — no MSAL, no Entra ID, no network. Used by the WebClient's
/// fake-backend run mode so an agent or a browser can drive the authenticated UI offline. The
/// role claim type is <c>"roles"</c> to match the real app (which sets
/// <c>UserOptions.RoleClaim = "roles"</c>), so <c>AuthorizeView Roles</c> and
/// <c>[Authorize(Roles=...)]</c> behave identically.
/// </summary>
public sealed class FakeAuthenticationStateProvider : AuthenticationStateProvider {
    /// <summary>The role claim type used by the real WebClient identities.</summary>
    public const string RoleClaimType = "roles";

    private readonly FakeAuthOptions options;

    public FakeAuthenticationStateProvider(FakeAuthOptions options) {
        this.options = options;
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync() =>
        Task.FromResult(new AuthenticationState(BuildPrincipal()));

    /// <summary>
    /// Replaces the current identity with one built from <paramref name="update"/> applied to a copy
    /// of the current options and notifies the UI. Handy for driving auth transitions inside tests.
    /// </summary>
    public void SetUser(Action<FakeAuthOptions> update) {
        update(options);
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(BuildPrincipal())));
    }

    private ClaimsPrincipal BuildPrincipal() {
        if (!options.IsAuthenticated) {
            return new ClaimsPrincipal(new ClaimsIdentity());
        }

        var claims = new List<Claim> {
            new(ClaimTypes.Name, options.UserName),
            new("name", options.UserName),
            new("oid", options.UserId),
        };
        claims.AddRange(options.Roles.Select(r => new Claim(RoleClaimType, r)));

        var identity = new ClaimsIdentity(
            claims,
            authenticationType: "FakeAuth",
            nameType: ClaimTypes.Name,
            roleType: RoleClaimType);

        return new ClaimsPrincipal(identity);
    }
}
