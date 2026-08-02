using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication.Internal;

namespace Shuttle.WebClient.Services;

/// <summary>
/// Entra emits multi-valued claims (such as <c>roles</c>) as a JSON array, which MSAL stores as a
/// single claim whose value is the literal array text (e.g. <c>["Shuttle.Admin"]</c>). That breaks
/// <c>IsInRole</c>/<c>[Authorize(Roles=...)]</c>/<c>AuthorizeView Roles</c>, which do an exact string
/// match. This factory expands any JSON-array-valued role claim into individual role claims.
/// </summary>
public class ArrayClaimsPrincipalFactory : AccountClaimsPrincipalFactory<RemoteUserAccount> {
    public ArrayClaimsPrincipalFactory(IAccessTokenProviderAccessor accessor) : base(accessor) {
    }

    public override async ValueTask<ClaimsPrincipal> CreateUserAsync(
        RemoteUserAccount account,
        RemoteAuthenticationUserOptions options) {
        var user = await base.CreateUserAsync(account, options);

        if (user.Identity is not ClaimsIdentity identity) {
            return user;
        }

        var arrayRoleClaims = identity.FindAll(identity.RoleClaimType)
            .Where(c => c.Value.TrimStart().StartsWith('['))
            .ToList();

        foreach (var claim in arrayRoleClaims) {
            identity.RemoveClaim(claim);

            string[]? roles;
            try {
                roles = JsonSerializer.Deserialize<string[]>(claim.Value);
            } catch (JsonException) {
                continue;
            }

            if (roles is null) {
                continue;
            }

            foreach (var role in roles) {
                identity.AddClaim(new Claim(identity.RoleClaimType, role));
            }
        }

        return user;
    }
}
