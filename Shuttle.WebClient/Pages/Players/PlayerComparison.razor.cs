using System.Net;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;
using Plotly.Blazor;
using Refit;
using Shuttle.Api.Client;
using Shuttle.Models.Players;
using Shuttle.WebClient.Models;
using Shuttle.WebClient.Models.Options;
using Shuttle.WebClient.Services;

namespace Shuttle.WebClient.Pages.Players;

public partial class PlayerComparison : ComponentBase, IDisposable {
    /// <summary>The most players the add-UX will let you accumulate before nudging you to stop.</summary>
    public const int MaxComparison = 3;

    [Parameter, SupplyParameterFromQuery(Name = "ids")] public string? Ids { get; set; }

    [Inject] private IShuttlePlayerClient PlayerClient { get; set; } = null!;
    [Inject] private IPlayerDirectoryService Directory { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;
    [Inject] private IShuttleOptionsStorage OptionsStorage { get; set; } = null!;

    // Player cards and known-missing ids are cached across navigations so re-ordering or removing a
    // player never re-fetches the ones already loaded.
    private readonly Dictionary<int, PlayerCard> cards = new();
    private readonly HashSet<int> missing = new();

    private List<int> requestedIds = new();
    private List<int> notFoundIds = new();
    private List<PlayerCard> chartedPlayers = new();
    private List<(PlayerCard Card, string Reason)> excludedPlayers = new();

    private readonly Config chartConfig = new() { Responsive = true };
    private readonly List<AttributeChart> attributeCharts = new();
    private AttributeChart? combinedChart;
    private bool groupedView;
    private bool needsRedraw;

    private const string AttributesTabId = "attributes";
    private const string TimelineTabId = "tpe-timeline";
    private string activeTabId = AttributesTabId;

    // TPE timelines are fetched lazily the first time the timeline tab is opened, then cached per
    // player so re-ordering or removing a player never re-fetches the ones already loaded.
    private readonly Dictionary<int, IReadOnlyList<TpeTimelinePoint>> timelines = new();
    private AttributeChart? timelineChart;
    private bool timelineLoading;
    private bool timelineLoaded;
    private string? timelineError;
    private bool alignTimelines;

    private PlayerSuggestion? selectedToAdd;
    private bool loading;
    private string? loadError;
    private bool darkMode;

    private bool AddDisabled => requestedIds.Count >= MaxComparison;

    // The requested players that resolved to a card, in requested order (charted or excluded).
    private IReadOnlyList<PlayerCard> SelectedCards =>
        requestedIds.Where(cards.ContainsKey).Select(id => cards[id]).ToList();

    private string ComparisonHeading =>
        chartedPlayers.Count > 0 && PlayerAttributeCharts.IsGoaltender(chartedPlayers[0].Attributes!)
            ? "Goaltender attributes"
            : "Skater attributes";

    protected override void OnInitialized() {
        darkMode = OptionsStorage.CurrentOptions.DarkMode;
        OptionsStorage.OptionsChanged += OnOptionsChanged;
    }

    protected override async Task OnParametersSetAsync() => await LoadAsync();

    private async Task LoadAsync() {
        loading = true;
        loadError = null;
        selectedToAdd = null;
        requestedIds = ParseIds(Ids);

        try {
            var toFetch = requestedIds
                .Where(id => !cards.ContainsKey(id) && !missing.Contains(id))
                .ToList();

            if (toFetch.Count > 0) {
                var results = await Task.WhenAll(toFetch.Select(FetchAsync));
                foreach (var (id, card) in results) {
                    if (card is null) {
                        missing.Add(id);
                    } else {
                        cards[id] = card;
                    }
                }
            }

            ComputeGroups();
            RebuildCharts();

            // If the timeline tab has already been opened, keep it in sync with the current selection
            // (fetching any newly added players and rebuilding the overlay).
            if (activeTabId == TimelineTabId) {
                await LoadTimelinesAsync();
            }
        } catch (Exception ex) {
            loadError = ex.Message;
        } finally {
            loading = false;
        }
    }

    private async Task<(int Id, PlayerCard? Card)> FetchAsync(int id) {
        try {
            return (id, await PlayerClient.GetPlayer(id));
        } catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.NotFound) {
            return (id, null);
        }
    }

    // Splits the requested players into the charted set (all of a single type, taken from the first
    // player that has attributes) and an excluded set (wrong type, or no attributes ingested yet).
    private void ComputeGroups() {
        notFoundIds = requestedIds.Where(missing.Contains).ToList();
        chartedPlayers = new List<PlayerCard>();
        excludedPlayers = new List<(PlayerCard, string)>();

        bool? goaltender = null;
        foreach (var card in requestedIds.Where(cards.ContainsKey).Select(id => cards[id])) {
            if (card.Attributes is null) {
                excludedPlayers.Add((card, "No in-game attributes have been ingested yet."));
                continue;
            }

            var isGoaltender = PlayerAttributeCharts.IsGoaltender(card.Attributes);
            if (goaltender is null) {
                goaltender = isGoaltender;
                chartedPlayers.Add(card);
            } else if (isGoaltender == goaltender) {
                chartedPlayers.Add(card);
            } else {
                excludedPlayers.Add((card, isGoaltender
                    ? "Goaltender — excluded from this skater comparison."
                    : "Skater — excluded from this goaltender comparison."));
            }
        }
    }

    private void RebuildCharts() {
        if (chartedPlayers.Count == 0) {
            attributeCharts.Clear();
            combinedChart = null;
            needsRedraw = true;
            return;
        }

        var series = chartedPlayers
            .Select(c => new PlayerAttributeSeries(c.Name, c.Attributes!))
            .ToList();

        var set = PlayerAttributeCharts.BuildOverlay(series, darkMode);

        // Update the charts in place — reuse the existing AttributeChart objects and only swap their
        // Layout/Data — so each PlotlyChart's captured @ref survives and React() redraws with the new
        // traces. Replacing the objects would orphan the refs on the reused chart components (Blazor
        // does not rebind @ref when it reuses a component instance), leaving the graph frozen until a
        // full page reload. This matters on the synchronous rebuild path (removing or reordering an
        // already-loaded player) where no loading state intervenes to recreate the chart components.
        combinedChart = MergeChart(combinedChart, set.Combined);

        if (SameShape(attributeCharts, set.Categories)) {
            for (var i = 0; i < set.Categories.Count; i++) {
                MergeInto(attributeCharts[i], set.Categories[i]);
            }
        } else {
            attributeCharts.Clear();
            attributeCharts.AddRange(set.Categories);
        }

        needsRedraw = true;
    }

    // Two chart sets share the same shape when they have the same categories in the same order; only
    // then is it safe to merge in place. A different shape (e.g. the remaining players switch the
    // comparison between skaters and goaltenders) replaces the objects, and the @key on the chart
    // components forces the reused ones to rebind their refs.
    private static bool SameShape(IReadOnlyList<AttributeChart> existing, IReadOnlyList<AttributeChart> updated) =>
        existing.Count == updated.Count
        && existing.Zip(updated, (a, b) => a.Title == b.Title).All(same => same);

    private static AttributeChart? MergeChart(AttributeChart? existing, AttributeChart? updated) {
        if (existing is null || updated is null) {
            return updated;
        }

        MergeInto(existing, updated);
        return existing;
    }

    private static void MergeInto(AttributeChart target, AttributeChart source) {
        target.Layout = source.Layout;
        target.Data.Clear();
        foreach (var trace in source.Data) {
            target.Data.Add(trace);
        }
    }

    // The timeline is loaded lazily the first time its tab is opened so comparisons that are never
    // inspected for progression don't incur the extra backend calls. Every tab change also flags a
    // redraw so the now-visible chart resizes to its container (hidden charts render at zero size).
    private async Task OnTabChangedAsync() {
        if (activeTabId == TimelineTabId) {
            await LoadTimelinesAsync();
        }

        needsRedraw = true;
    }

    private async Task LoadTimelinesAsync() {
        var toFetch = SelectedCards
            .Where(c => !timelines.ContainsKey(c.PlayerId))
            .Select(c => c.PlayerId)
            .ToList();

        if (toFetch.Count > 0) {
            timelineLoading = true;
            timelineError = null;
            StateHasChanged();

            try {
                var results = await Task.WhenAll(toFetch.Select(FetchTimelineAsync));
                foreach (var (id, points) in results) {
                    timelines[id] = points ?? Array.Empty<TpeTimelinePoint>();
                }
            } catch (Exception ex) {
                timelineError = ex.Message;
            } finally {
                timelineLoading = false;
            }
        }

        timelineLoaded = true;
        BuildTimelineChart();
        needsRedraw = true;
    }

    private async Task<(int Id, IReadOnlyList<TpeTimelinePoint>? Points)> FetchTimelineAsync(int id) {
        try {
            return (id, await PlayerClient.GetPlayerTpeTimeline(id));
        } catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.NotFound) {
            return (id, null);
        }
    }

    // Overlays one step-line TPE series per selected player. When alignment is on, each series is
    // shifted so every player's first recorded point lands on the earliest player's start, letting
    // you compare progression pace from a common origin rather than by absolute calendar date.
    private void BuildTimelineChart() {
        var series = SelectedCards
            .Select(c => (c.Name, Points: timelines.GetValueOrDefault(c.PlayerId)))
            .Where(x => x.Points is not null)
            .Select(x => (x.Name, Points: x.Points!))
            .ToList();

        var rebuilt = PlayerAttributeCharts.BuildTimelineOverlay(series, alignTimelines, darkMode);

        if (rebuilt is null || timelineChart is null) {
            // First build (or nothing to chart): adopt the new object outright; its ref binds on render.
            timelineChart = rebuilt;
            return;
        }

        // Update the existing chart object in place — swap its Layout and refill its Data — so the
        // captured PlotlyChart ref survives and React() picks up the new traces. Replacing the object
        // would orphan the @ref (Blazor reuses the PlotlyChart instance without reassigning the ref),
        // leaving Chart null so the redraw never runs and the graph appears frozen.
        timelineChart.Layout = rebuilt.Layout;
        timelineChart.Data.Clear();
        foreach (var trace in rebuilt.Data) {
            timelineChart.Data.Add(trace);
        }
    }

    private void OnAlignChanged() {
        BuildTimelineChart();
        needsRedraw = true;
    }

    private static List<int> ParseIds(string? ids) {
        if (string.IsNullOrWhiteSpace(ids)) {
            return new List<int>();
        }

        var seen = new HashSet<int>();
        var result = new List<int>();
        foreach (var token in ids.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)) {
            if (int.TryParse(token, out var id) && seen.Add(id)) {
                result.Add(id);
            }
        }

        return result;
    }

    private async Task OnPlayerSearch(OptionsSearchEventArgs<PlayerSuggestion> e) {
        e.Items = await Directory.Search(e.Text);
    }

    private void OnPlayerSelected(PlayerSuggestion? player) {
        selectedToAdd = null;
        if (player is null || requestedIds.Contains(player.PlayerId)) {
            return;
        }

        Navigation.NavigateTo(Routes.Players.CompareWith(requestedIds.Append(player.PlayerId)));
    }

    private void Remove(int playerId) =>
        Navigation.NavigateTo(Routes.Players.CompareWith(requestedIds.Where(id => id != playerId)));

    private void ClearAll() => Navigation.NavigateTo(Routes.Players.Compare);

    private void OnViewChanged() => needsRedraw = true;

    private void OnOptionsChanged(ShuttleOptions options) {
        if (options.DarkMode == darkMode) {
            return;
        }

        darkMode = options.DarkMode;
        foreach (var chart in attributeCharts) {
            ApplyTheme(chart);
        }

        ApplyTheme(combinedChart);
        ApplyTheme(timelineChart);
        needsRedraw = true;
        InvokeAsync(StateHasChanged);
    }

    private void ApplyTheme(AttributeChart? chart) {
        if (chart?.LayoutFactory is not null) {
            chart.Layout = chart.LayoutFactory(darkMode);
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender) {
        if (!needsRedraw) {
            return;
        }

        needsRedraw = false;

        // Only redraw the charts for the currently visible tab/view; the hidden ones may have been
        // disposed, so their captured refs must not be touched.
        var visible = activeTabId == TimelineTabId
            ? timelineChart is null ? Array.Empty<AttributeChart>() : new[] { timelineChart }
            : groupedView
                ? (IEnumerable<AttributeChart>)attributeCharts
                : combinedChart is null ? Array.Empty<AttributeChart>() : new[] { combinedChart };

        foreach (var group in visible) {
            if (group.Chart is null) {
                continue;
            }

            try {
                await group.Chart.React();
            } catch (ObjectDisposedException) {
                // The chart was disposed mid-toggle; the new view will redraw itself.
            }
        }
    }

    public void Dispose() => OptionsStorage.OptionsChanged -= OnOptionsChanged;
}
