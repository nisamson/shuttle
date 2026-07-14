using System.Security.Claims;

namespace Shuttle.WebClient.Extensions;

public static class ClaimsPrincipalExtensions {
    private const string ObjectIdClaimType = "oid";

    private const string ObjectIdSchemaClaimType =
        "http://schemas.microsoft.com/identity/claims/objectidentifier";

    /// <summary>
    /// Gets the stable, unique site identifier for the user: the Entra object id (<c>oid</c>) claim.
    /// This is immutable and consistent across the WebClient and the API, unlike <c>sub</c> (per-app)
    /// or mutable claims such as email/name.
    /// </summary>
    /// <returns>The object id, or <see langword="null"/> if the claim is not present.</returns>
    public static string? GetUserId(this ClaimsPrincipal principal) {
        ArgumentNullException.ThrowIfNull(principal);
        return principal.FindFirst(ObjectIdClaimType)?.Value
               ?? principal.FindFirst(ObjectIdSchemaClaimType)?.Value;
    }
}
