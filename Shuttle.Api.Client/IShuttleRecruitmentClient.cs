using Refit;
using Shuttle.Models.Recruitment;

namespace Shuttle.Api.Client;

/// <summary>
/// Typed Refit client for the Shuttle backend API (<c>Shuttle.Api</c>) recruitment endpoints. The
/// base address is supplied at registration time (see
/// <see cref="ShuttleApiClientExtensions.AddShuttleRecruitmentClient"/>).
/// <para>
/// Every recruitment endpoint is public (anonymous), so this client does not require an auth message
/// handler.
/// </para>
/// </summary>
public interface IShuttleRecruitmentClient {
    /// <summary>
    /// Fetches the per-<see cref="RecruiterCategory"/> totals (<c>GET /recruitment/summary</c>).
    /// </summary>
    /// <param name="token">A cancellation token.</param>
    [Get("/recruitment/summary")]
    Task<IReadOnlyList<RecruiterCategorySummary>> GetSummary(CancellationToken token = default);

    /// <summary>
    /// Fetches recruiter tallies (<c>GET /recruitment/recruiters</c>), optionally filtered by
    /// category, sorted, and limited to the top N.
    /// </summary>
    /// <param name="category">Restrict to a single category; omitted = all categories.</param>
    /// <param name="sort">The field to sort by; omitted = the server default (recruits).</param>
    /// <param name="descending">Sort descending (default <see langword="true"/>).</param>
    /// <param name="limit">Return only the top N recruiters (server clamps to [1, 500]); omitted = all.</param>
    /// <param name="token">A cancellation token.</param>
    [Get("/recruitment/recruiters")]
    Task<IReadOnlyList<RecruiterTally>> GetRecruiters(
        [Query] RecruiterCategory? category = null,
        [Query] RecruiterSortField? sort = null,
        [Query] bool descending = true,
        [Query] int? limit = null,
        CancellationToken token = default);

    /// <summary>
    /// Fetches a single recruiter's tally plus the members they directly recruited
    /// (<c>GET /recruitment/recruiters/{recruiter}</c>). Only Player/External recruiters are
    /// addressable; returns <see langword="null"/> when no such recruiter exists (HTTP 404).
    /// </summary>
    /// <param name="recruiter">The recruiter key (matched case-insensitively).</param>
    /// <param name="token">A cancellation token.</param>
    [Get("/recruitment/recruiters/{recruiter}")]
    Task<RecruiterDetail?> GetRecruiter(string recruiter, CancellationToken token = default);

    /// <summary>
    /// Fetches a recruiter's transitive downstream lineage as a nested tree with per-subtree roll-up
    /// totals (<c>GET /recruitment/recruiters/{recruiter}/lineage</c>). Only Player/External
    /// recruiters are addressable; returns <see langword="null"/> when no such recruiter exists
    /// (HTTP 404).
    /// </summary>
    /// <param name="recruiter">The recruiter key (matched case-insensitively).</param>
    /// <param name="maxDepth">Cap the traversal depth (direct recruits are depth 1; server clamps to
    /// [1, 32]); omitted = full depth.</param>
    /// <param name="token">A cancellation token.</param>
    [Get("/recruitment/recruiters/{recruiter}/lineage")]
    Task<RecruitmentTreeNode?> GetRecruiterLineage(
        string recruiter,
        [Query] int? maxDepth = null,
        CancellationToken token = default);
}
