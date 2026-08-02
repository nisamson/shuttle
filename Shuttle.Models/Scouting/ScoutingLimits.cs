namespace Shuttle.Models.Scouting;

/// <summary>
/// Shared length limits for user-authored scouting content. Referenced by DTO data-annotations
/// (server <c>ModelState</c> + client <c>DataAnnotationsValidator</c>) and by EF Core
/// <c>HasMaxLength</c> configuration so the database, API, and UI stay in sync.
/// </summary>
public static class ScoutingLimits {
    /// <summary>Maximum length of a scouting team name.</summary>
    public const int TeamNameMaxLength = 80;

    /// <summary>Maximum length of a scouting board name.</summary>
    public const int BoardNameMaxLength = 80;

    /// <summary>Maximum length of a scouting comment body.</summary>
    public const int CommentBodyMaxLength = 2000;
}

/// <summary>
/// A member's role within a scouting team, in descending order of privilege.
/// </summary>
public enum ScoutingTeamRole {
    /// <summary>Read-only access: can view boards, entries, and comments but not modify anything.</summary>
    Viewer = 0,

    /// <summary>Can edit boards and entries and post comments, but cannot manage members or the team.</summary>
    Editor = 1,

    /// <summary>Full control: manage the team, its members and roles, boards, entries, and comments.</summary>
    Owner = 2
}
