using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Shuttle.Api.Controllers;

[Authorize]
[ApiController]
[Route("[controller]")]
public class MeController : ControllerBase {

    [HttpGet("roles")]
    public ActionResult<IEnumerable<string>> GetRoles() {
        // Microsoft.Identity.Web maps app roles to the "roles" claim type, exposed via
        // ClaimsIdentity.RoleClaimType. Reading the fixed ClaimTypes.Role URI would miss them.
        var roleClaimType = (User.Identity as ClaimsIdentity)?.RoleClaimType ?? ClaimTypes.Role;
        var roles = User.FindAll(roleClaimType).Select(c => c.Value).ToList();
        return Ok(roles);
    }
}
