namespace Shuttle.Models.Recruitment;

/// <summary>
/// A single member a recruiter directly recruited, listed in <see cref="RecruiterDetail"/>.
/// </summary>
public record RecruitedMember {
    /// <summary>The recruited member's user id.</summary>
    public required int UserId { get; init; }

    /// <summary>The recruited member's username.</summary>
    public required string Username { get; init; }

    /// <summary>The recruited member's full-career TPE (summed across all of their players).</summary>
    public required long CareerTpe { get; init; }
}
