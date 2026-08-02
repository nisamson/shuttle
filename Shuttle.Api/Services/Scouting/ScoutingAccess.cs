using Shuttle.EFCore.Entities;
using Shuttle.Models.Scouting;

namespace Shuttle.Api.Services.Scouting;

/// <summary>
/// A resolved authorization context for a caller against a single <see cref="EFCore.Entities.Scouting.ScoutingTeam"/>.
/// Combines the caller's own account, whether they hold the site-admin role (a superuser over every
/// team), and their membership role in this team (if any). All capability checks fold the site-admin
/// override in, so callers should consult these members rather than inspecting <see cref="Role"/>.
/// </summary>
/// <param name="User">The caller's own account.</param>
/// <param name="IsSiteAdmin">Whether the caller holds the site-admin role and is therefore a superuser.</param>
/// <param name="Role">The caller's membership role in the team, or <c>null</c> if they are not a member.</param>
public sealed record ScoutingAccess(ShuttleUser User, bool IsSiteAdmin, ScoutingTeamRole? Role) {
    /// <summary>Whether the caller is a member of the team (regardless of role).</summary>
    public bool IsMember => Role.HasValue;

    /// <summary>Whether the caller may view the team, its boards, entries, and comments.</summary>
    public bool CanView => IsSiteAdmin || (Role?.CanView() ?? false);

    /// <summary>Whether the caller may create/edit/delete boards and their entries.</summary>
    public bool CanEditBoards => IsSiteAdmin || (Role?.CanEditBoards() ?? false);

    /// <summary>Whether the caller may post comments on this team's threads.</summary>
    public bool CanComment => IsSiteAdmin || (Role?.CanComment() ?? false);

    /// <summary>Whether the caller may rename the team and manage its members and their roles.</summary>
    public bool CanManageTeam => IsSiteAdmin || (Role?.CanManageTeam() ?? false);

    /// <summary>
    /// Whether the caller may delete any comment, including ones they did not author (moderation).
    /// Site admins and team owners can moderate; authors delete their own via a separate check.
    /// </summary>
    public bool CanModerateComments => IsSiteAdmin || Role == ScoutingTeamRole.Owner;
}
