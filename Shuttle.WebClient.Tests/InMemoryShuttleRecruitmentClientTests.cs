using Shuttle.Models.Recruitment;
using Shuttle.WebClient.Testing;

namespace Shuttle.WebClient.Tests;

/// <summary>
/// Pure (no-render) tests for <see cref="InMemoryShuttleRecruitmentClient"/> proving it reproduces
/// the server's recruitment semantics the WebClient relies on: per-category summary, recruiter
/// tallies, direct-recruit detail, and the transitive downstream lineage tree with per-subtree
/// roll-ups. Expectations are derived from the deterministic seed graph in
/// <see cref="SeedData.RecruitmentEdges"/> (rooted at <c>frostbite → dmarsh → fnolan → jquinn</c>,
/// <c>frostbite → cvance → hvega</c>, <c>ipope → kalder</c>, plus External Reddit/Google).
/// </summary>
public class InMemoryShuttleRecruitmentClientTests {
    private readonly InMemoryShuttleRecruitmentClient client = new();

    [Fact]
    public async Task GetRecruiter_returns_direct_recruits_with_tally() {
        var detail = await client.GetRecruiter("frostbite");

        Assert.NotNull(detail);
        Assert.Equal("frostbite", detail!.Tally.Recruiter);
        Assert.Equal(RecruiterCategory.Player, detail.Tally.Category);
        Assert.Equal(2, detail.Tally.RecruitedUsers);
        Assert.Equal(3090, detail.Tally.TotalCareerTpe);
        Assert.Equal(6, detail.Tally.LineageUsers);
        Assert.Equal(6530, detail.Tally.LineageCareerTpe);

        // Ordered by career TPE desc: dmarsh (1710) before cvance (1380).
        Assert.Equal(new[] { "dmarsh", "cvance" }, detail.RecruitedMembers.Select(m => m.Username));
    }

    [Fact]
    public async Task GetRecruiter_matches_case_insensitively() {
        var lower = await client.GetRecruiter("frostbite");
        var upper = await client.GetRecruiter("FROSTBITE");

        Assert.NotNull(upper);
        Assert.Equal(lower!.Tally.RecruitedUsers, upper!.Tally.RecruitedUsers);
        Assert.Equal(lower.Tally.LineageCareerTpe, upper.Tally.LineageCareerTpe);
    }

    [Fact]
    public async Task GetRecruiter_returns_null_for_a_user_who_recruited_nobody() {
        Assert.Null(await client.GetRecruiter("bridge"));
    }

    [Fact]
    public async Task GetRecruiter_returns_null_for_unknown_recruiter() {
        Assert.Null(await client.GetRecruiter("nobody-here"));
    }

    [Fact]
    public async Task GetRecruiterLineage_builds_the_multi_level_downstream_tree() {
        var root = await client.GetRecruiterLineage("frostbite");

        Assert.NotNull(root);
        // The root represents the recruiter itself: null id, zero own TPE.
        Assert.Null(root!.UserId);
        Assert.Equal("frostbite", root.Name);
        Assert.Equal(0, root.CareerTpe);
        Assert.Equal(6, root.SubtreeUsers);
        Assert.Equal(6530, root.SubtreeCareerTpe);

        // Direct recruits ordered by career TPE desc.
        Assert.Equal(new[] { "dmarsh", "cvance" }, root.Recruited.Select(n => n.Name));

        var dmarsh = root.Recruited.First(n => n.Name == "dmarsh");
        Assert.Equal(5004, dmarsh.UserId);
        Assert.Equal(1710, dmarsh.CareerTpe);
        // dmarsh's subtree: fnolan (1490) + gholt (640) + jquinn (590) = 3 users, 2720 TPE.
        Assert.Equal(3, dmarsh.SubtreeUsers);
        Assert.Equal(2720, dmarsh.SubtreeCareerTpe);

        // The multi-level chain frostbite → dmarsh → fnolan → jquinn is present.
        var fnolan = dmarsh.Recruited.First(n => n.Name == "fnolan");
        var jquinn = fnolan.Recruited.Single();
        Assert.Equal("jquinn", jquinn.Name);
        Assert.Equal(5010, jquinn.UserId);
        Assert.Empty(jquinn.Recruited);
    }

    [Fact]
    public async Task GetRecruiterLineage_returns_null_for_a_user_who_recruited_nobody() {
        Assert.Null(await client.GetRecruiterLineage("bridge"));
    }

    [Fact]
    public async Task GetRecruiterLineage_honours_a_depth_cap() {
        var capped = await client.GetRecruiterLineage("frostbite", maxDepth: 1);

        Assert.NotNull(capped);
        Assert.All(capped!.Recruited, node => Assert.Empty(node.Recruited));
    }

    [Fact]
    public async Task GetSummary_aggregates_per_category_totals() {
        var summary = await client.GetSummary();

        var player = summary.Single(s => s.Category == RecruiterCategory.Player);
        // Player recruiters: frostbite, dmarsh, fnolan, cvance, ipope.
        Assert.Equal(5, player.DistinctRecruiters);
        Assert.Equal(7, player.RecruitedUsers);

        var external = summary.Single(s => s.Category == RecruiterCategory.External);
        // External recruiters: Reddit (2 recruits), Google (1 recruit).
        Assert.Equal(2, external.DistinctRecruiters);
        Assert.Equal(3, external.RecruitedUsers);
    }

    [Fact]
    public async Task GetRecruiters_can_filter_by_category_and_limit() {
        var topExternal = await client.GetRecruiters(
            category: RecruiterCategory.External,
            sort: RecruiterSortField.Recruits,
            limit: 1);

        var only = Assert.Single(topExternal);
        // Reddit has 2 recruits vs Google's 1, so it sorts first.
        Assert.Equal("Reddit", only.Recruiter);
        Assert.Equal(RecruiterCategory.External, only.Category);
    }
}
