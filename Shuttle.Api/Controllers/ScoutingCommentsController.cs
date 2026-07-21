using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shuttle.Api.Services.Scouting;
using Shuttle.Models.Scouting;

namespace Shuttle.Api.Controllers;

/// <summary>
/// Editing and deleting individual scouting comments. Authors may edit or delete their own comments;
/// team owners and site admins may delete any comment (moderation). Enforced by <see cref="IScoutingService"/>.
/// </summary>
[Authorize]
[ApiController]
[Route("scouting/comments")]
[Route("scouting/comment")]
public class ScoutingCommentsController : ControllerBase {
    private readonly IScoutingService scouting;

    public ScoutingCommentsController(IScoutingService scouting) {
        this.scouting = scouting;
    }

    /// <summary>Edits a comment's body. Author only.</summary>
    [HttpPut("{commentId:guid}")]
    [ProducesResponseType<ScoutingComment>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ScoutingComment>> EditComment(
        Guid commentId,
        [FromBody] UpdateScoutingCommentRequest request,
        CancellationToken cancellationToken) {
        return (await scouting.EditCommentAsync(commentId, request, User, cancellationToken)).ToActionResult(this);
    }

    /// <summary>Deletes a comment. Author, or a team owner / site admin (moderation).</summary>
    [HttpDelete("{commentId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteComment(Guid commentId, CancellationToken cancellationToken) {
        return (await scouting.DeleteCommentAsync(commentId, User, cancellationToken)).ToNoContent(this);
    }
}
