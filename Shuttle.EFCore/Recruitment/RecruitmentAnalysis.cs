namespace Shuttle.EFCore.Recruitment;

/// <summary>
/// A single recruiter → recruited-member relationship (one row per recruited user).
/// </summary>
/// <param name="Recruiter">The classified/normalized recruiter key.</param>
/// <param name="Category">The recruiter's category.</param>
/// <param name="UserId">The recruited member's user id.</param>
/// <param name="Username">The recruited member's username.</param>
/// <param name="CareerTpe">The recruited member's full-career TPE (sum of latest timeline TPE over their players).</param>
public sealed record RecruitmentEdge(
    string Recruiter,
    RecruiterCategory Category,
    int UserId,
    string Username,
    long CareerTpe);

/// <summary>
/// All members attributed to a single recruiter.
/// </summary>
/// <param name="Recruiter">The classified/normalized recruiter key (canonical member casing for
/// <see cref="RecruiterCategory.Player"/>).</param>
/// <param name="Category">The recruiter's category.</param>
/// <param name="RecruitedUsers">The number of distinct members attributed to this recruiter.</param>
/// <param name="TotalCareerTpe">The combined full-career TPE of every member this recruiter <em>directly</em> recruited.</param>
/// <param name="LineageUsers">The number of distinct members in this recruiter's full downstream lineage —
/// every member reachable transitively (direct recruits, their recruits, and so on). Excludes the recruiter
/// themselves.</param>
/// <param name="LineageCareerTpe">The combined full-career TPE of every member in this recruiter's full
/// downstream lineage (see <paramref name="LineageUsers"/>). Always ≥ <paramref name="TotalCareerTpe"/>.</param>
/// <param name="Edges">The individual <em>directly</em> recruited members, ordered by career TPE (desc) then username.</param>
public sealed record RecruiterTally(
    string Recruiter,
    RecruiterCategory Category,
    int RecruitedUsers,
    long TotalCareerTpe,
    int LineageUsers,
    long LineageCareerTpe,
    IReadOnlyList<RecruitmentEdge> Edges);

/// <summary>
/// Aggregate counts for a single <see cref="RecruiterCategory"/>.
/// </summary>
/// <param name="Category">The category.</param>
/// <param name="DistinctRecruiters">The number of distinct recruiters in this category.</param>
/// <param name="RecruitedUsers">The total members attributed to this category.</param>
/// <param name="TotalCareerTpe">The combined full-career TPE of every member in this category.</param>
public sealed record RecruiterCategoryCount(
    RecruiterCategory Category,
    int DistinctRecruiters,
    int RecruitedUsers,
    long TotalCareerTpe);

/// <summary>
/// The result of a recruitment analysis, consolidated by recruited member: per-recruiter tallies, a
/// per-category summary, and the flat edge list. All collections use a deterministic ordering so
/// downstream reports are stable.
/// </summary>
/// <param name="Tallies">Recruiters ordered by recruited-member count (desc), then total career TPE
/// (desc), then recruiter (asc).</param>
/// <param name="CategorySummary">Per-category totals, ordered by <see cref="RecruiterCategory"/>.</param>
/// <param name="Edges">Every recruiter → member edge, ordered to match <paramref name="Tallies"/>.</param>
public sealed record RecruitmentAnalysis(
    IReadOnlyList<RecruiterTally> Tallies,
    IReadOnlyList<RecruiterCategoryCount> CategorySummary,
    IReadOnlyList<RecruitmentEdge> Edges);
