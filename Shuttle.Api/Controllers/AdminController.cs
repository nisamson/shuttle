using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Shuttle.Api.Controllers;

/// <summary>
/// Administrative API endpoints. Access requires the <see cref="Startup.AdminRole"/> app role,
/// assigned to the caller on the API's Entra app registration and carried in the validated JWT
/// bearer token's <c>roles</c> claim.
/// </summary>
[Authorize(Policy = Startup.AdminAuthorizationPolicy)]
[ApiController]
[Route("[controller]")]
public class AdminController : ControllerBase {
    /// <summary>
    /// Simple admin-gated health check. Returns 200 with the caller's name when the caller holds
    /// the admin role, 401 when unauthenticated, or 403 when authenticated without the role.
    /// </summary>
    [HttpGet("ping")]
    public ActionResult<object> Ping() {
        var name = User.FindFirstValue("name")
            ?? User.FindFirstValue(ClaimTypes.Name)
            ?? User.Identity?.Name;
        return Ok(new { Message = "pong", User = name });
    }
}
