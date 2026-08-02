namespace Shuttle.EFCore.Recruitment;

/// <summary>
/// A minimal per-player projection used for recruitment analysis. Recruitment is consolidated by
/// <em>user</em>, so each player carries its owning user, creation time (to pick the user's first
/// player, which determines who recruited the member), the raw recruiter value, and the player's
/// latest cumulative total TPE taken from its TPE timeline (<c>TpeEvent</c>).
/// </summary>
/// <param name="UserId">The id of the SHL member that owns the player.</param>
/// <param name="OwnerUsername">The SHL member (username) that owns the player.</param>
/// <param name="PlayerId">The player's unique id.</param>
/// <param name="Name">The player's display name.</param>
/// <param name="CreationTime">When the player was created (earliest player fixes the user's recruiter).</param>
/// <param name="Recruiter">The raw, unclassified recruiter value from the player's profile (may be blank).</param>
/// <param name="LatestTimelineTpe">
/// The player's latest cumulative total TPE from its TPE timeline (the <c>TotalTpe</c> of the most
/// recent <c>TpeEvent</c>), or <c>0</c> when the player has no timeline entries.
/// </param>
public sealed record RecruitedPlayer(
    int UserId,
    string OwnerUsername,
    int PlayerId,
    string Name,
    DateTime CreationTime,
    string? Recruiter,
    long LatestTimelineTpe);

/// <summary>
/// A recruited SHL member (the unit of recruitment): the member, who recruited them (from their first
/// player), and their full-career TPE summed across all of their players.
/// </summary>
/// <param name="UserId">The recruited member's user id.</param>
/// <param name="Username">The recruited member's username.</param>
/// <param name="Recruiter">The raw recruiter value from the member's earliest-created player (may be blank).</param>
/// <param name="CareerTpe">
/// The member's full-career TPE: the sum, across every player they have made, of that player's latest
/// timeline total TPE (see <see cref="RecruitedPlayer.LatestTimelineTpe"/>).
/// </param>
public sealed record RecruitedUser(int UserId, string Username, string? Recruiter, long CareerTpe);
