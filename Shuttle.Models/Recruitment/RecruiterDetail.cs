namespace Shuttle.Models.Recruitment;

/// <summary>
/// A single recruiter's tally together with the members they directly recruited, returned by
/// <c>GET /recruitment/recruiters/{recruiter}</c>.
/// </summary>
public record RecruiterDetail {
    /// <summary>The recruiter's aggregate tallies.</summary>
    public required RecruiterTally Tally { get; init; }

    /// <summary>
    /// The members this recruiter directly recruited, ordered by career TPE (desc) then username.
    /// </summary>
    public required IReadOnlyList<RecruitedMember> RecruitedMembers { get; init; }
}
