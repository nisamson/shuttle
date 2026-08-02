using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shuttle.Api.Services;
using Shuttle.Api.Services.Recruitment;
using Shuttle.Models.Recruitment;
using EfCore = Shuttle.EFCore.Recruitment;

namespace Shuttle.Api.Controllers;

/// <summary>
/// Public, unauthenticated read access to the recruitment analysis: per-category totals, a
/// filterable/sortable recruiter list, per-recruiter detail, and a recruiter's transitive downstream
/// lineage tree. All data derives from the same public portal data the rest of the API exposes.
/// </summary>
[AllowAnonymous]
[ApiController]
[Route("recruitment")]
public class RecruitmentController : ControllerBase {
    /// <summary>Upper bound on the recruiter list <c>limit</c> (silently clamped).</summary>
    private const int MaxLimit = 500;

    /// <summary>Upper bound on the lineage <c>maxDepth</c> (silently clamped).</summary>
    private const int MaxDepth = 32;

    private static readonly TimeSpan CacheMaxAge = TimeSpan.FromMinutes(5);

    private readonly IRecruitmentAnalysisCache cache;

    public RecruitmentController(IRecruitmentAnalysisCache cache) {
        this.cache = cache;
    }

    /// <summary>
    /// Returns the per-<see cref="RecruiterCategory"/> totals, ordered by category.
    /// </summary>
    [HttpGet("summary")]
    [ProducesResponseType<IReadOnlyList<RecruiterCategorySummary>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status304NotModified)]
    public async Task<ActionResult<IReadOnlyList<RecruiterCategorySummary>>> GetSummary(
        CancellationToken cancellationToken) {
        var snapshot = await cache.GetAsync(cancellationToken);

        IReadOnlyList<RecruiterCategorySummary> summary =
            [.. snapshot.Analysis.CategorySummary.Select(c => c.ToSummaryDto())];

        return this.DbVersionedOk(summary, snapshot.LastUpdated, CacheMaxAge);
    }

    /// <summary>
    /// Returns recruiter tallies, optionally filtered by category, sorted, and limited to the top N.
    /// </summary>
    /// <param name="category">Restrict to a single category. Omitted = all categories.</param>
    /// <param name="sort">The field to sort by (default <see cref="RecruiterSortField.Recruits"/>).</param>
    /// <param name="descending">Sort descending (default <see langword="true"/>).</param>
    /// <param name="limit">Return only the top <paramref name="limit"/> recruiters (clamped to
    /// [1, 500]). Omitted = all.</param>
    [HttpGet("recruiters")]
    [ProducesResponseType<IReadOnlyList<RecruiterTally>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status304NotModified)]
    public async Task<ActionResult<IReadOnlyList<RecruiterTally>>> GetRecruiters(
        [FromQuery] RecruiterCategory? category,
        [FromQuery] RecruiterSortField? sort,
        [FromQuery] bool descending = true,
        [FromQuery] int? limit = null,
        CancellationToken cancellationToken = default) {
        var snapshot = await cache.GetAsync(cancellationToken);

        IReadOnlyList<RecruiterTally> tallies =
            [.. SelectRecruiters(snapshot.Analysis.Tallies, category, sort ?? RecruiterSortField.Recruits, descending, limit)
                .Select(t => t.ToTallyDto())];

        return this.DbVersionedOk(tallies, snapshot.LastUpdated, CacheMaxAge);
    }

    /// <summary>
    /// Returns a single recruiter's tally plus the members they directly recruited. Only
    /// <see cref="RecruiterCategory.Player"/> and <see cref="RecruiterCategory.External"/> recruiters
    /// are addressable; <c>404</c> when no such recruiter exists.
    /// </summary>
    /// <param name="recruiter">The recruiter key (URL-encoded; matched case-insensitively).</param>
    [HttpGet("recruiters/{recruiter}")]
    [ProducesResponseType<RecruiterDetail>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status304NotModified)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RecruiterDetail>> GetRecruiter(
        string recruiter,
        CancellationToken cancellationToken) {
        var snapshot = await cache.GetAsync(cancellationToken);

        var tally = FindAddressable(snapshot.Analysis, recruiter);
        if (tally is null) {
            return NotFound();
        }

        return this.DbVersionedOk(tally.ToDetailDto(), snapshot.LastUpdated, CacheMaxAge);
    }

    /// <summary>
    /// Returns a recruiter's transitive downstream lineage as a nested tree with per-subtree roll-up
    /// totals. Only Player/External recruiters are addressable; <c>404</c> when no such recruiter
    /// exists.
    /// </summary>
    /// <param name="recruiter">The recruiter key (URL-encoded; matched case-insensitively).</param>
    /// <param name="maxDepth">Cap the traversal depth (direct recruits are depth 1; clamped to
    /// [1, 32]). Omitted = full depth.</param>
    [HttpGet("recruiters/{recruiter}/lineage")]
    [ProducesResponseType<RecruitmentTreeNode>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status304NotModified)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RecruitmentTreeNode>> GetRecruiterLineage(
        string recruiter,
        [FromQuery] int? maxDepth,
        CancellationToken cancellationToken) {
        var snapshot = await cache.GetAsync(cancellationToken);

        var tally = FindAddressable(snapshot.Analysis, recruiter);
        if (tally is null) {
            return NotFound();
        }

        var childLookup = EfCore.RecruitmentAnalyzer.BuildChildLookup(snapshot.Analysis);
        var depthCap = maxDepth is { } d ? Math.Clamp(d, 1, MaxDepth) : (int?)null;
        var tree = EfCore.RecruitmentAnalyzer.BuildLineageTree(tally, childLookup, depthCap);

        return this.DbVersionedOk(tree.ToTreeDto(), snapshot.LastUpdated, CacheMaxAge);
    }

    /// <summary>
    /// Applies the optional category filter, the requested sort (with a stable recruiter/category
    /// tiebreak so <paramref name="limit"/> is deterministic), and the optional top-N limit.
    /// </summary>
    internal static IEnumerable<EfCore.RecruiterTally> SelectRecruiters(
        IReadOnlyList<EfCore.RecruiterTally> tallies,
        RecruiterCategory? category,
        RecruiterSortField sort,
        bool descending,
        int? limit) {
        IEnumerable<EfCore.RecruiterTally> filtered = tallies;
        if (category is { } cat) {
            filtered = filtered.Where(t => t.Category.ToDto() == cat);
        }

        Func<EfCore.RecruiterTally, long> key = sort switch {
            RecruiterSortField.CareerTpe => t => t.TotalCareerTpe,
            RecruiterSortField.LineageUsers => t => t.LineageUsers,
            RecruiterSortField.LineageTpe => t => t.LineageCareerTpe,
            _ => t => t.RecruitedUsers,
        };

        var ordered = (descending ? filtered.OrderByDescending(key) : filtered.OrderBy(key))
            .ThenBy(t => t.Recruiter, StringComparer.OrdinalIgnoreCase)
            .ThenBy(t => t.Category);

        return limit is { } n ? ordered.Take(Math.Clamp(n, 1, MaxLimit)) : ordered;
    }

    /// <summary>
    /// Finds the addressable recruiter (Player/External only) whose key matches
    /// <paramref name="recruiter"/> case-insensitively, or <c>null</c>. Keys are unique across the
    /// Player/External categories, so at most one tally matches.
    /// </summary>
    private static EfCore.RecruiterTally? FindAddressable(EfCore.RecruitmentAnalysis analysis, string recruiter) =>
        analysis.Tallies.FirstOrDefault(t =>
            (t.Category == EfCore.RecruiterCategory.Player || t.Category == EfCore.RecruiterCategory.External)
            && string.Equals(t.Recruiter, recruiter, StringComparison.OrdinalIgnoreCase));
}
