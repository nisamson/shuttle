using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SHLAnalytics.Shuttle.Api.Controllers;

[Authorize]
[ApiController]
[Route("[controller]")]
public class MeController : ControllerBase {
    
    [HttpGet("roles")]
    public ActionResult<IEnumerable<string>> GetRoles() {
        var roles = User.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList();
        return Ok(roles);
    }
}
