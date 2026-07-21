using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shuttle.Api.Services.Scouting;
using Shuttle.Models.Scouting;

namespace Shuttle.Api.Controllers;

/// <summary>
/// A scouting team's draft boards: board detail and edits, the ranked player entries, and the
/// board-level and per-entry comment threads. Permissions are enforced by <see cref="IScoutingService"/>.
/// </summary>
[Authorize]
[ApiController]
[Route("scouting/boards")]
[Route("scouting/board")]
public class ScoutingBoardsController : ControllerBase {
    private readonly IScoutingService scouting;

    public ScoutingBoardsController(IScoutingService scouting) {
        this.scouting = scouting;
    }

    /// <summary>Returns a board's detail and its ranked entries.</summary>
    [HttpGet("{boardId:guid}")]
    [ProducesResponseType<ScoutingBoardDetail>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ScoutingBoardDetail>> GetBoard(Guid boardId, CancellationToken cancellationToken) {
        return (await scouting.GetBoardAsync(boardId, User, cancellationToken)).ToActionResult(this);
    }

    /// <summary>Updates a board's name and/or target draft season. Owners and editors only.</summary>
    [HttpPut("{boardId:guid}")]
    [ProducesResponseType<ScoutingBoardDetail>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ScoutingBoardDetail>> UpdateBoard(
        Guid boardId,
        [FromBody] UpdateScoutingBoardRequest request,
        CancellationToken cancellationToken) {
        return (await scouting.UpdateBoardAsync(boardId, request, User, cancellationToken)).ToActionResult(this);
    }

    /// <summary>Deletes a board. Owners and editors only.</summary>
    [HttpDelete("{boardId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteBoard(Guid boardId, CancellationToken cancellationToken) {
        return (await scouting.DeleteBoardAsync(boardId, User, cancellationToken)).ToNoContent(this);
    }

    /// <summary>Adds a player to the board at the next rank. Owners and editors only.</summary>
    [HttpPost("{boardId:guid}/entries")]
    [ProducesResponseType<ScoutingBoardEntry>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ScoutingBoardEntry>> AddEntry(
        Guid boardId,
        [FromBody] AddScoutingBoardEntryRequest request,
        CancellationToken cancellationToken) {
        return (await scouting.AddEntryAsync(boardId, request, User, cancellationToken)).ToActionResult(this);
    }

    /// <summary>
    /// Adds several players to the board in one transaction, identified by upstream id and/or player
    /// name; only players that exist in the database are appended. A name matching more than one
    /// player rejects the request. Owners and editors only.
    /// </summary>
    [HttpPost("{boardId:guid}/entries/bulk")]
    [ProducesResponseType<AddScoutingBoardEntriesResult>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AddScoutingBoardEntriesResult>> AddEntries(
        Guid boardId,
        [FromBody] AddScoutingBoardEntriesRequest request,
        CancellationToken cancellationToken) {
        return (await scouting.AddEntriesAsync(boardId, request, User, cancellationToken)).ToActionResult(this);
    }

    /// <summary>Removes a player from the board and closes the rank gap. Owners and editors only.</summary>
    [HttpDelete("{boardId:guid}/entries/{playerId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> RemoveEntry(Guid boardId, int playerId, CancellationToken cancellationToken) {
        return (await scouting.RemoveEntryAsync(boardId, playerId, User, cancellationToken)).ToNoContent(this);
    }

    /// <summary>
    /// Updates a prospect's scouting status, assignment, and (for active prospects) rank in one
    /// request. Rejecting unranks the prospect and compacts the active ranks. Owners and editors only.
    /// </summary>
    [HttpPut("{boardId:guid}/entries/{playerId:int}")]
    [ProducesResponseType<ScoutingBoardEntry>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ScoutingBoardEntry>> UpdateEntry(
        Guid boardId,
        int playerId,
        [FromBody] UpdateScoutingBoardEntryRequest request,
        CancellationToken cancellationToken) {
        return (await scouting.UpdateEntryAsync(boardId, playerId, request, User, cancellationToken)).ToActionResult(this);
    }

    /// <summary>Removes several players from the board in one transaction and compacts the ranks. Owners and editors only.</summary>
    [HttpPost("{boardId:guid}/entries/remove")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult> RemoveEntries(
        Guid boardId,
        [FromBody] RemoveScoutingBoardEntriesRequest request,
        CancellationToken cancellationToken) {
        return (await scouting.RemoveEntriesAsync(boardId, request, User, cancellationToken)).ToNoContent(this);
    }

    /// <summary>
    /// Moves a player from one rank to another. <c>FromRank</c> guards against stale reorders.
    /// Owners and editors only.
    /// </summary>
    [HttpPost("{boardId:guid}/entries/move")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult> MoveEntry(
        Guid boardId,
        [FromBody] MoveScoutingBoardEntryRequest request,
        CancellationToken cancellationToken) {
        return (await scouting.MoveEntryAsync(boardId, request, User, cancellationToken)).ToNoContent(this);
    }

    /// <summary>Returns the board-level comment thread.</summary>
    [HttpGet("{boardId:guid}/comments")]
    [ProducesResponseType<IReadOnlyList<ScoutingComment>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<ScoutingComment>>> GetBoardComments(
        Guid boardId,
        CancellationToken cancellationToken) {
        return (await scouting.GetBoardCommentsAsync(boardId, User, cancellationToken)).ToActionResult(this);
    }

    /// <summary>Posts a comment to the board-level thread. Owners and editors only.</summary>
    [HttpPost("{boardId:guid}/comments")]
    [ProducesResponseType<ScoutingComment>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ScoutingComment>> AddBoardComment(
        Guid boardId,
        [FromBody] CreateScoutingCommentRequest request,
        CancellationToken cancellationToken) {
        return (await scouting.AddBoardCommentAsync(boardId, request, User, cancellationToken)).ToActionResult(this);
    }

    /// <summary>Returns a player entry's comment thread.</summary>
    [HttpGet("{boardId:guid}/entries/{playerId:int}/comments")]
    [ProducesResponseType<IReadOnlyList<ScoutingComment>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<ScoutingComment>>> GetEntryComments(
        Guid boardId,
        int playerId,
        CancellationToken cancellationToken) {
        return (await scouting.GetEntryCommentsAsync(boardId, playerId, User, cancellationToken)).ToActionResult(this);
    }

    /// <summary>Posts a comment to a player entry's thread. Owners and editors only.</summary>
    [HttpPost("{boardId:guid}/entries/{playerId:int}/comments")]
    [ProducesResponseType<ScoutingComment>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ScoutingComment>> AddEntryComment(
        Guid boardId,
        int playerId,
        [FromBody] CreateScoutingCommentRequest request,
        CancellationToken cancellationToken) {
        return (await scouting.AddEntryCommentAsync(boardId, playerId, request, User, cancellationToken)).ToActionResult(this);
    }
}
