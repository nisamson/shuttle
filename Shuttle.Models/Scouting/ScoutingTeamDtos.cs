using System.ComponentModel.DataAnnotations;

namespace Shuttle.Models.Scouting;

/// <summary>
/// A row in the "my teams" dashboard (and the admin "all teams" view). Carries just enough to list
/// and navigate teams without loading their full membership or boards.
/// </summary>
public record ScoutingTeamSummary {
    /// <summary>The team's stable identifier.</summary>
    public required Guid Id { get; init; }

    /// <summary>The team's display name.</summary>
    public required string Name { get; init; }

    /// <summary>
    /// The caller's role in this team, or <c>null</c> when the caller is not a member (only possible
    /// in the site-admin "all teams" view).
    /// </summary>
    public ScoutingTeamRole? MyRole { get; init; }

    /// <summary>Number of members on the team.</summary>
    public required int MemberCount { get; init; }

    /// <summary>Number of draft boards owned by the team.</summary>
    public required int BoardCount { get; init; }

    /// <summary>When the team was last modified.</summary>
    public required DateTimeOffset UpdatedAt { get; init; }
}

/// <summary>
/// The full detail of a single scouting team: its metadata, the caller's role, its members, and a
/// summary of its boards.
/// </summary>
public record ScoutingTeamDetail {
    public required Guid Id { get; init; }
    public required string Name { get; init; }

    /// <summary>The caller's role, or <c>null</c> when viewed by a non-member site admin.</summary>
    public ScoutingTeamRole? MyRole { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }

    public required IReadOnlyList<ScoutingMember> Members { get; init; }
    public required IReadOnlyList<ScoutingBoardSummary> Boards { get; init; }
}

/// <summary>Payload for creating a scouting team. The caller becomes its first Owner.</summary>
public record CreateScoutingTeamRequest {
    [Required]
    [StringLength(ScoutingLimits.TeamNameMaxLength, MinimumLength = 1)]
    public required string Name { get; init; }
}

/// <summary>Payload for renaming a scouting team.</summary>
public record UpdateScoutingTeamRequest {
    [Required]
    [StringLength(ScoutingLimits.TeamNameMaxLength, MinimumLength = 1)]
    public required string Name { get; init; }
}
