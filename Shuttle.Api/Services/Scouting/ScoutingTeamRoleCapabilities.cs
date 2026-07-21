using Shuttle.Models.Scouting;

namespace Shuttle.Api.Services.Scouting;

/// <summary>
/// Capability helpers for a <see cref="ScoutingTeamRole"/>. Centralises what each role is allowed to
/// do so controllers and the access service share a single definition of the permission model.
/// </summary>
public static class ScoutingTeamRoleCapabilities {
    /// <summary>Whether the role may view the team, its boards, entries, and comments.</summary>
    public static bool CanView(this ScoutingTeamRole role) => true;

    /// <summary>Whether the role may create/edit/delete boards and their entries.</summary>
    public static bool CanEditBoards(this ScoutingTeamRole role) => role >= ScoutingTeamRole.Editor;

    /// <summary>Whether the role may post comments.</summary>
    public static bool CanComment(this ScoutingTeamRole role) => role >= ScoutingTeamRole.Editor;

    /// <summary>Whether the role may rename the team and add/remove members and change their roles.</summary>
    public static bool CanManageTeam(this ScoutingTeamRole role) => role == ScoutingTeamRole.Owner;
}
