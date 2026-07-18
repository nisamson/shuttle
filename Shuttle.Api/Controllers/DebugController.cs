using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shuttle.Api.Filters;
using Shuttle.Models.Users;
using Shuttle.Shl.Api.Client;

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
    private readonly IShlForumClient forumClient;

    public DebugController(IShlForumClient forumClient) {
        this.forumClient = forumClient;
    }

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

    /// <summary>
    /// Scrapes the given forum member's profile page (via <see cref="IShlForumClient"/>) and returns
    /// the Discord username listed on it, if any. Intended for verifying the forum scraper against
    /// live data; the result's <see cref="DiscordUsernameResult.DiscordUsername"/> is <c>null</c>
    /// when the profile lists no Discord username.
    /// </summary>
    [HttpGet("users/{userId:int}/discord")]
    [ProducesResponseType<DiscordUsernameResult>(StatusCodes.Status200OK)]
    public async Task<ActionResult<DiscordUsernameResult>> GetDiscordUsername(
        int userId,
        CancellationToken cancellationToken) {
        var username = await forumClient.GetDiscordUsername(userId, cancellationToken);
        return Ok(new DiscordUsernameResult(userId, username));
    }
}
