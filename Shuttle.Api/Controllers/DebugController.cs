using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shuttle.Api.Filters;

namespace Shuttle.Api.Controllers;

/// <summary>
/// Development-only diagnostics for the API. These endpoints are only routable in the Development
/// hosting environment (see <see cref="DevelopmentOnlyAttribute"/>) and return 404 elsewhere.
/// </summary>
[Authorize]
[DevelopmentOnly]
[ApiController]
[Route("[controller]")]
public class DebugController : ControllerBase {
    /// <summary>
    /// Returns the roles the API sees for the currently authenticated caller, as resolved from the
    /// validated bearer token. Useful for verifying role mapping between the identity provider and
    /// the server without inspecting the raw token on the client.
    /// </summary>
    [HttpGet("roles")]
    public ActionResult<IEnumerable<string>> GetRoles() {
        var identity = User.Identity as ClaimsIdentity;
        var roleClaimType = identity?.RoleClaimType ?? ClaimTypes.Role;
        var roles = User.FindAll(roleClaimType)
            .Select(c => c.Value)
            .OrderBy(r => r, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return Ok(roles);
    }
}
