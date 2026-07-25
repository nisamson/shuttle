namespace Shuttle.Models.Recruitment;

/// <summary>
/// A single recruiter's tallies, returned by <c>GET /recruitment/recruiters</c> and embedded in
/// <see cref="RecruiterDetail"/>. Consolidated by recruited member (not by player).
/// </summary>
public record RecruiterTally {
    /// <summary>The classified/normalized recruiter key (canonical member casing for a Player).</summary>
    public required string Recruiter { get; init; }

    /// <summary>The recruiter's category.</summary>
    public required RecruiterCategory Category { get; init; }

    /// <summary>The number of distinct members this recruiter directly recruited.</summary>
    public required int RecruitedUsers { get; init; }

    /// <summary>The combined full-career TPE of every member this recruiter directly recruited.</summary>
    public required long TotalCareerTpe { get; init; }

    /// <summary>
    /// The number of distinct members in this recruiter's full downstream lineage — every member
    /// reachable transitively. Excludes the recruiter themselves.
    /// </summary>
    public required int LineageUsers { get; init; }

    /// <summary>
    /// The combined full-career TPE of every member in this recruiter's full downstream lineage.
    /// Always greater than or equal to <see cref="TotalCareerTpe"/>.
    /// </summary>
    public required long LineageCareerTpe { get; init; }
}
