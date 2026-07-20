using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web;
using Shuttle.Api.Services.Users;
using Shuttle.EFCore.Entities;
using Shuttle.Models.Users;

namespace Shuttle.Api.Controllers;

/// <summary>
/// Manages the authenticated caller's own Shuttle account. The account is created lazily on first
/// access and keyed to the caller's Entra object id. All persistence is delegated to
/// <see cref="IUserService"/>.
/// </summary>
[Authorize]
[ApiController]
[Route("users")]
public class UsersController : ControllerBase {
    private readonly IUserService users;

    public UsersController(IUserService users) {
        this.users = users;
    }

    /// <summary>
    /// Returns the caller's own account, creating it (keyed to their Entra object id, with a
    /// username derived from the generated id) if it does not already exist.
    /// </summary>
    [HttpGet("me")]
    [ProducesResponseType<CurrentUser>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CurrentUser>> GetCurrentUser(CancellationToken cancellationToken) {
        if (!TryGetObjectId(out var objectId)) {
            return Unauthorized();
        }

        var user = await users.GetOrCreateAsync(objectId, cancellationToken);
        return Ok(ToCurrentUser(user));
    }

    /// <summary>
    /// Updates the mutable fields of the caller's own account. Currently limited to the username,
    /// which must be 2-32 characters of ASCII letters, digits, periods, and underscores.
    /// </summary>
    [HttpPut("me")]
    [ProducesResponseType<CurrentUser>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CurrentUser>> UpdateCurrentUser(
        [FromBody] UpdateCurrentUserRequest request,
        CancellationToken cancellationToken) {
        if (!TryGetObjectId(out var objectId)) {
            return Unauthorized();
        }

        var result = await users.UpdateUsernameAsync(objectId, request.Username, cancellationToken);
        return result switch {
            UpdateUsernameResult.Success success => Ok(ToCurrentUser(success.User)),
            UpdateUsernameResult.UsernameTaken => Conflict(new ProblemDetails {
                Title = "Username already taken",
                Detail = "The requested username is already in use.",
                Status = StatusCodes.Status409Conflict,
            }),
            _ => InvalidUsername(),
        };
    }

    private ActionResult<CurrentUser> InvalidUsername() {
        ModelState.AddModelError(
            nameof(UpdateCurrentUserRequest.Username),
            "Username must be 2-32 characters and contain only ASCII letters, digits, periods, and underscores.");
        return ValidationProblem(ModelState);
    }

    private bool TryGetObjectId(out Guid objectId) => Guid.TryParse(User.GetObjectId(), out objectId);

    private static CurrentUser ToCurrentUser(ShuttleUser user) => new() {
        Id = user.Id,
        Username = user.Username,
    };
}
