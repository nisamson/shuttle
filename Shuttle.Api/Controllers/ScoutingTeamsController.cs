using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shuttle.Api.Services.Scouting;
using Shuttle.Models.Scouting;

namespace Shuttle.Api.Controllers;

/// <summary>
/// User-created scouting teams: the dashboard list, team CRUD, membership management, and board
/// creation. All endpoints require an authenticated caller; per-team permissions are enforced by
/// <see cref="IScoutingService"/> based on the caller's membership role (or the site-admin role).
/// </summary>
[Authorize]
[ApiController]
[Route("scouting/teams")]
[Route("scouting/team")]
public class ScoutingTeamsController : ControllerBase {
    private readonly IScoutingService scouting;

    public ScoutingTeamsController(IScoutingService scouting) {
        this.scouting = scouting;
    }

    /// <summary>Lists the teams the caller is a member of, for the "my teams" dashboard.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<ScoutingTeamSummary>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ScoutingTeamSummary>>> GetMyTeams(CancellationToken cancellationToken) {
        return Ok(await scouting.GetMyTeamsAsync(User, cancellationToken));
    }

    /// <summary>Lists every team. Site-admin only, backing the dashboard's "all teams" view.</summary>
    [HttpGet("all")]
    [Authorize(Policy = Startup.AdminAuthorizationPolicy)]
    [ProducesResponseType<IReadOnlyList<ScoutingTeamSummary>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ScoutingTeamSummary>>> GetAllTeams(CancellationToken cancellationToken) {
        return Ok(await scouting.GetAllTeamsAsync(cancellationToken));
    }

    /// <summary>Returns a single team's detail: metadata, members, and board summaries.</summary>
    [HttpGet("{teamId:guid}")]
    [ProducesResponseType<ScoutingTeamDetail>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ScoutingTeamDetail>> GetTeam(Guid teamId, CancellationToken cancellationToken) {
        return (await scouting.GetTeamAsync(teamId, User, cancellationToken)).ToActionResult(this);
    }

    /// <summary>Creates a team; the caller becomes its first owner.</summary>
    [HttpPost]
    [ProducesResponseType<ScoutingTeamDetail>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ScoutingTeamDetail>> CreateTeam(
        [FromBody] CreateScoutingTeamRequest request,
        CancellationToken cancellationToken) {
        return (await scouting.CreateTeamAsync(request, User, cancellationToken)).ToActionResult(this);
    }

    /// <summary>Renames a team. Owners and site admins only.</summary>
    [HttpPut("{teamId:guid}")]
    [ProducesResponseType<ScoutingTeamDetail>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ScoutingTeamDetail>> RenameTeam(
        Guid teamId,
        [FromBody] UpdateScoutingTeamRequest request,
        CancellationToken cancellationToken) {
        return (await scouting.RenameTeamAsync(teamId, request, User, cancellationToken)).ToActionResult(this);
    }

    /// <summary>Deletes a team. Allowed for a site admin, or the team's sole owner.</summary>
    [HttpDelete("{teamId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult> DeleteTeam(Guid teamId, CancellationToken cancellationToken) {
        return (await scouting.DeleteTeamAsync(teamId, User, cancellationToken)).ToNoContent(this);
    }

    /// <summary>Adds a member by username (immediately, no invite acceptance). Owners only.</summary>
    [HttpPost("{teamId:guid}/members")]
    [ProducesResponseType<ScoutingMember>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ScoutingMember>> AddMember(
        Guid teamId,
        [FromBody] AddScoutingMemberRequest request,
        CancellationToken cancellationToken) {
        return (await scouting.AddMemberAsync(teamId, request, User, cancellationToken)).ToActionResult(this);
    }

    /// <summary>Changes a member's role. Owners only; cannot demote the sole owner.</summary>
    [HttpPut("{teamId:guid}/members/{userId:guid}")]
    [ProducesResponseType<ScoutingMember>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ScoutingMember>> UpdateMemberRole(
        Guid teamId,
        Guid userId,
        [FromBody] UpdateScoutingMemberRoleRequest request,
        CancellationToken cancellationToken) {
        return (await scouting.UpdateMemberRoleAsync(teamId, userId, request, User, cancellationToken)).ToActionResult(this);
    }

    /// <summary>Removes a member. Owners only; cannot remove the sole owner.</summary>
    [HttpDelete("{teamId:guid}/members/{userId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult> RemoveMember(Guid teamId, Guid userId, CancellationToken cancellationToken) {
        return (await scouting.RemoveMemberAsync(teamId, userId, User, cancellationToken)).ToNoContent(this);
    }

    /// <summary>Removes the caller's own membership. The sole owner cannot leave.</summary>
    [HttpPost("{teamId:guid}/leave")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult> LeaveTeam(Guid teamId, CancellationToken cancellationToken) {
        return (await scouting.LeaveTeamAsync(teamId, User, cancellationToken)).ToNoContent(this);
    }

    /// <summary>Creates a board on the team. Owners and editors only.</summary>
    [HttpPost("{teamId:guid}/boards")]
    [ProducesResponseType<ScoutingBoardDetail>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ScoutingBoardDetail>> CreateBoard(
        Guid teamId,
        [FromBody] CreateScoutingBoardRequest request,
        CancellationToken cancellationToken) {
        return (await scouting.CreateBoardAsync(teamId, request, User, cancellationToken)).ToActionResult(this);
    }
}
