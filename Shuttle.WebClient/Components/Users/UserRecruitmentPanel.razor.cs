using System.Net;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;
using Refit;
using Shuttle.Api.Client;
using Shuttle.Models.Recruitment;

namespace Shuttle.WebClient.Components.Users;

/// <summary>
/// Embedded profile panel that surfaces a user's downstream recruitment: rollup stats plus a
/// <see cref="FluentTreeView"/> whose top-level items are the members they directly recruited, each
/// expandable inline to reveal their own recruits (the full transitive lineage). A single
/// <c>GET /recruitment/recruiters/{username}/lineage</c> call powers both the stats and the tree; a
/// user who recruited nobody (the endpoint's 404 → <see langword="null"/>) renders an empty state.
/// </summary>
public partial class UserRecruitmentPanel : ComponentBase {
    /// <summary>The profile user's id (unused for the lookup, kept for clarity/future use).</summary>
    [Parameter] public int UserId { get; set; }

    /// <summary>The profile user's username — the recruiter key the lineage endpoint is addressed by.</summary>
    [Parameter] public string Username { get; set; } = string.Empty;

    [Inject] private IShuttleRecruitmentClient RecruitmentClient { get; set; } = null!;

    // The lineage root represents the profile user themselves; its Recruited are the direct recruits.
    private RecruitmentTreeNode? root;
    private bool loading;
    private string? error;

    // Guards against redundant reloads when the parent re-renders without changing the username.
    private string? loadedFor;

    // The tree's top-level items (the direct recruits) and a lookup back to the source node so the
    // ItemTemplate can render rich per-node content (link + TPE + subtree roll-up).
    private IReadOnlyList<ITreeViewItem> treeItems = [];
    private readonly Dictionary<string, RecruitmentTreeNode> nodesById = [];

    // Bumped to force the FluentTreeView to re-render after a bulk expand/collapse, since the
    // underlying web component otherwise retains its own per-node expansion state.
    private int treeRenderKey;

    private long DirectCareerTpe => root?.Recruited.Sum(r => r.CareerTpe) ?? 0;

    // True when at least one recruit has their own recruits (lineage extends past the direct recruits).
    private bool HasDeeperLineage => root is not null && root.SubtreeUsers > root.Recruited.Count;

    protected override async Task OnParametersSetAsync() {
        if (string.Equals(loadedFor, Username, StringComparison.Ordinal)) {
            return;
        }

        await LoadAsync();
    }

    private async Task LoadAsync() {
        loading = true;
        error = null;
        root = null;
        nodesById.Clear();
        treeItems = [];
        loadedFor = Username;

        try {
            root = await RecruitmentClient.GetRecruiterLineage(Username);
            if (root is not null) {
                treeItems = [.. root.Recruited.Select(MapNode)];
            }
        } catch (HttpRequestException) {
            error = "Failed to reach the server. Please try again.";
        } catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.NotFound) {
            // No recruiter lineage for this user is a normal case, not an error — show the empty state.
            root = null;
        } catch (ApiException) {
            error = "Failed to load recruitment data.";
        } finally {
            loading = false;
        }
    }

    /// <summary>
    /// The rollup stats shown above the tree (label, formatted value, optional tooltip). The lineage
    /// stats are omitted when the recruiter has only direct recruits (no deeper lineage), since they
    /// would just duplicate the direct figures.
    /// </summary>
    private IEnumerable<(string Label, string Value, string? Tooltip)> Stats() {
        var r = root!;
        yield return ("Recruited", r.Recruited.Count.ToString("N0"), null);
        yield return ("Direct TPE", DirectCareerTpe.ToString("N0"),
            "The combined current career TPE of the members this user recruited directly.");

        if (HasDeeperLineage) {
            yield return ("Lineage members", r.SubtreeUsers.ToString("N0"), null);
            yield return ("Lineage TPE", r.SubtreeCareerTpe.ToString("N0"),
                "The combined current career TPE across this user's entire downstream lineage — every member "
                + "they recruited, directly or indirectly (transitively, through the members they recruited).");
        }
    }

    /// <summary>
    /// Maps a lineage node (and its subtree) to a <see cref="TreeViewItem"/>, recording it in
    /// <see cref="nodesById"/> so the template can recover the full node from the item id.
    /// </summary>
    private ITreeViewItem MapNode(RecruitmentTreeNode node) {
        var id = node.UserId?.ToString() ?? node.Name;
        nodesById[id] = node;
        return new TreeViewItem {
            Id = id,
            Text = node.Name,
            Items = node.Recruited.Count > 0 ? [.. node.Recruited.Select(MapNode)] : null,
        };
    }

    /// <summary>Recovers the source lineage node for a rendered tree item, or <c>null</c>.</summary>
    private RecruitmentTreeNode? NodeFor(ITreeViewItem item) =>
        item.Id is { } id && nodesById.TryGetValue(id, out var node) ? node : null;

    /// <summary>Expands or collapses every node in the tree at once.</summary>
    private void SetAllExpanded(bool expanded) {
        foreach (var item in treeItems) {
            SetExpandedRecursive(item, expanded);
        }

        // Force the tree to be re-created so the new Expanded states are applied.
        treeRenderKey++;
    }

    private static void SetExpandedRecursive(ITreeViewItem item, bool expanded) {
        if (item is TreeViewItem tvi) {
            tvi.Expanded = expanded;
        }

        if (item.Items is not null) {
            foreach (var child in item.Items) {
                SetExpandedRecursive(child, expanded);
            }
        }
    }
}
