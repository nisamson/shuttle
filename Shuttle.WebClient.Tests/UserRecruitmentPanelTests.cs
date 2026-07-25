using System.Net;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Refit;
using Shuttle.Api.Client;
using Shuttle.Models.Recruitment;
using Shuttle.WebClient.Components.Users;

namespace Shuttle.WebClient.Tests;

/// <summary>
/// Render tests for <see cref="UserRecruitmentPanel"/> against the offline in-memory recruitment
/// client — no browser, server, or Azure. The panel needs no auth, so it renders directly with the
/// seeded <see cref="WebClientTestContext"/> dependencies. Expectations follow the deterministic seed
/// graph (frostbite has a multi-level lineage; bridge recruited nobody).
/// </summary>
public class UserRecruitmentPanelTests : WebClientTestContext {
    [Fact]
    public void Renders_rollup_stats_for_a_recruiter() {
        var cut = Render<UserRecruitmentPanel>(p => p
            .Add(x => x.UserId, 5001)
            .Add(x => x.Username, "frostbite"));

        var markup = cut.Markup;
        Assert.Contains("Recruited", markup);
        Assert.Contains("Direct TPE", markup);
        Assert.Contains("Lineage members", markup);
        Assert.Contains("Lineage TPE", markup);

        // 6 lineage members and 6,530 lineage TPE (formatted "N0").
        Assert.Contains("6,530", markup);
    }

    [Fact]
    public void Renders_direct_recruits_as_top_level_tree_items_with_profile_links() {
        var cut = Render<UserRecruitmentPanel>(p => p
            .Add(x => x.UserId, 5001)
            .Add(x => x.Username, "frostbite"));

        // The direct recruits appear as links to their profiles; the root user is not itself rendered.
        var links = cut.FindAll("a.emphasized-link");
        var hrefs = links.Select(l => l.GetAttribute("href")).ToList();
        Assert.Contains("/users/5004", hrefs); // dmarsh
        Assert.Contains("/users/5003", hrefs); // cvance
        Assert.DoesNotContain("/users/5001", hrefs); // the profile user (root) is not in the tree
    }

    [Fact]
    public void Renders_multi_level_lineage_nodes() {
        var cut = Render<UserRecruitmentPanel>(p => p
            .Add(x => x.UserId, 5001)
            .Add(x => x.Username, "frostbite"));

        var hrefs = cut.FindAll("a.emphasized-link").Select(l => l.GetAttribute("href")).ToList();
        // Deeper lineage members are present in the rendered tree.
        Assert.Contains("/users/5006", hrefs); // fnolan (level 2)
        Assert.Contains("/users/5010", hrefs); // jquinn (level 3)
    }

    [Fact]
    public void Renders_expand_and_collapse_all_buttons_that_toggle_every_node() {
        var cut = Render<UserRecruitmentPanel>(p => p
            .Add(x => x.UserId, 5001)
            .Add(x => x.Username, "frostbite"));

        var buttons = cut.FindAll("fluent-button");
        var expand = buttons.Single(b => b.TextContent.Contains("Expand all"));
        var collapse = buttons.Single(b => b.TextContent.Contains("Collapse all"));

        // Clicking either bulk-toggle drives every TreeViewItem's Expanded flag and re-renders the tree.
        expand.Click();
        Assert.True(AllTreeItems(cut).All(i => i.Expanded));

        collapse.Click();
        Assert.True(AllTreeItems(cut).All(i => !i.Expanded));
    }

    // The live TreeViewItem instances the panel is currently binding to the FluentTreeView.
    private static IEnumerable<Microsoft.FluentUI.AspNetCore.Components.TreeViewItem> AllTreeItems(
        IRenderedComponent<UserRecruitmentPanel> cut) {
        var field = typeof(UserRecruitmentPanel).GetField(
            "treeItems", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        var roots = (IEnumerable<Microsoft.FluentUI.AspNetCore.Components.ITreeViewItem>)field.GetValue(cut.Instance)!;
        return roots.SelectMany(Flatten);
    }

    private static IEnumerable<Microsoft.FluentUI.AspNetCore.Components.TreeViewItem> Flatten(
        Microsoft.FluentUI.AspNetCore.Components.ITreeViewItem item) {
        if (item is Microsoft.FluentUI.AspNetCore.Components.TreeViewItem tvi) {
            yield return tvi;
        }

        foreach (var child in item.Items ?? []) {
            foreach (var descendant in Flatten(child)) {
                yield return descendant;
            }
        }
    }

    [Fact]
    public void Hides_the_lineage_stats_when_the_recruiter_has_only_direct_recruits() {
        // cvance (5003) recruited only hvega, who recruited nobody — no deeper lineage, so the
        // lineage stats would just duplicate the direct ones and are omitted.
        var cut = Render<UserRecruitmentPanel>(p => p
            .Add(x => x.UserId, 5003)
            .Add(x => x.Username, "cvance"));

        var markup = cut.Markup;
        Assert.Contains("Recruited", markup);
        Assert.Contains("Direct TPE", markup);
        Assert.DoesNotContain("Lineage members", markup);
        Assert.DoesNotContain("Lineage TPE", markup);
    }

    [Fact]
    public void Renders_empty_state_for_a_user_who_recruited_nobody() {
        var cut = Render<UserRecruitmentPanel>(p => p
            .Add(x => x.UserId, 5002)
            .Add(x => x.Username, "bridge"));

        Assert.Contains("hasn't recruited anyone", cut.Markup);
        Assert.Empty(cut.FindAll("fluent-tree-view"));
    }

    [Fact]
    public void Renders_empty_state_when_the_lineage_endpoint_returns_404() {
        // The real Refit client throws a 404 ApiException (not null) for a user who recruited nobody.
        // That is a normal case, not an error — the panel must show the empty state, not an error bar.
        Services.AddSingleton<IShuttleRecruitmentClient>(new NotFoundRecruitmentClient());

        var cut = Render<UserRecruitmentPanel>(p => p
            .Add(x => x.UserId, 5001)
            .Add(x => x.Username, "frostbite"));

        Assert.Contains("hasn't recruited anyone", cut.Markup);
        Assert.DoesNotContain("Failed to load recruitment", cut.Markup);
        Assert.Empty(cut.FindAll("fluent-tree-view"));
    }

    /// <summary>A recruitment client whose lineage lookup always throws a 404 <see cref="ApiException"/>.</summary>
    private sealed class NotFoundRecruitmentClient : IShuttleRecruitmentClient {
        public Task<IReadOnlyList<RecruiterCategorySummary>> GetSummary(CancellationToken token = default) =>
            throw new NotImplementedException();

        public Task<IReadOnlyList<RecruiterTally>> GetRecruiters(
            RecruiterCategory? category = null, RecruiterSortField? sort = null, bool descending = true,
            int? limit = null, CancellationToken token = default) => throw new NotImplementedException();

        public Task<RecruiterDetail?> GetRecruiter(string recruiter, CancellationToken token = default) =>
            throw new NotImplementedException();

        public async Task<RecruitmentTreeNode?> GetRecruiterLineage(
            string recruiter, int? maxDepth = null, CancellationToken token = default) {
            using var response = new HttpResponseMessage(HttpStatusCode.NotFound);
            throw await ApiException.Create(
                new HttpRequestMessage(HttpMethod.Get, "http://localhost/recruitment/recruiters/x/lineage"),
                HttpMethod.Get, response, new RefitSettings());
        }
    }
}
