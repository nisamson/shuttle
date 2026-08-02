namespace Shuttle.Models.Recruitment;

/// <summary>
/// Aggregate recruitment counts for a single <see cref="RecruiterCategory"/>, returned by
/// <c>GET /recruitment/summary</c>.
/// </summary>
public record RecruiterCategorySummary {
    /// <summary>The category these totals cover.</summary>
    public required RecruiterCategory Category { get; init; }

    /// <summary>The number of distinct recruiters in this category.</summary>
    public required int DistinctRecruiters { get; init; }

    /// <summary>The total number of members attributed to this category.</summary>
    public required int RecruitedUsers { get; init; }

    /// <summary>The combined full-career TPE of every member in this category.</summary>
    public required long TotalCareerTpe { get; init; }
}
