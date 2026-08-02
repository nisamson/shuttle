using System.ComponentModel.DataAnnotations;

namespace Shuttle.Models.Scouting;

/// <summary>A member of a scouting team, with their account identity and role.</summary>
public record ScoutingMember {
    /// <summary>The member's <c>ShuttleUser</c> id.</summary>
    public required Guid UserId { get; init; }

    /// <summary>The member's username.</summary>
    public required string Username { get; init; }

    /// <summary>The member's role in the team.</summary>
    public required ScoutingTeamRole Role { get; init; }

    /// <summary>When the member was added to the team.</summary>
    public required DateTimeOffset CreatedAt { get; init; }
}

/// <summary>
/// Payload for adding a member to a team by username. The named user must already have a Shuttle
/// account (i.e. have signed in at least once); addition takes effect immediately with no invite
/// acceptance step.
/// </summary>
public record AddScoutingMemberRequest {
    [Required]
    public required string Username { get; init; }

    /// <summary>The role to grant the new member. Defaults to <see cref="ScoutingTeamRole.Viewer"/>.</summary>
    public ScoutingTeamRole Role { get; init; } = ScoutingTeamRole.Viewer;
}

/// <summary>Payload for changing an existing member's role.</summary>
public record UpdateScoutingMemberRoleRequest {
    public required ScoutingTeamRole Role { get; init; }
}
