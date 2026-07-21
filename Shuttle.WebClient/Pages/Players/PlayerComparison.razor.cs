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
    public const int MaxComparison = 6;

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
    private bool groupedView = true;
    private bool needsRedraw;

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
        attributeCharts.Clear();
        combinedChart = null;

        if (chartedPlayers.Count == 0) {
            return;
        }

        var series = chartedPlayers
            .Select(c => new PlayerAttributeSeries(c.Name, c.Attributes!))
            .ToList();

        var set = PlayerAttributeCharts.BuildOverlay(series, darkMode);
        combinedChart = set.Combined;
        attributeCharts.AddRange(set.Categories);
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

        // Only redraw the charts for the currently visible view; the hidden ones may have been
        // disposed, so their captured refs must not be touched.
        var visible = groupedView
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
