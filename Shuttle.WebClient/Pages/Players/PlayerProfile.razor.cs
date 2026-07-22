using System.Globalization;
using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;
using Plotly.Blazor;
using Refit;
using Shuttle.Api.Client;
using Shuttle.Models.Leagues;
using Shuttle.Models.Players;
using Shuttle.Shl.Api.Models.Common;
using Shuttle.Shl.Api.Models.Portal.V1;
using Shuttle.WebClient.Components;
using Shuttle.WebClient.Models;
using Shuttle.WebClient.Models.Options;
using Shuttle.WebClient.Services;

namespace Shuttle.WebClient.Pages.Players;

public partial class PlayerProfile : ComponentBase, IDisposable {
    [Parameter] public int PlayerId { get; set; }

    [Inject] private IShuttlePlayerClient PlayerClient { get; set; } = null!;
    [Inject] private IShuttleLeagueClient LeagueClient { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;
    [Inject] private IShuttleOptionsStorage OptionsStorage { get; set; } = null!;

    private bool darkMode;

    private void GoToSearch() => Navigation.NavigateTo(Routes.Players.Root);

    protected override void OnInitialized() {
        darkMode = OptionsStorage.CurrentOptions.DarkMode;
        OptionsStorage.OptionsChanged += OnOptionsChanged;
    }

    private void OnOptionsChanged(ShuttleOptions options) {
        if (options.DarkMode == darkMode) {
            return;
        }

        darkMode = options.DarkMode;
        RethemeCharts();
        InvokeAsync(StateHasChanged);
    }

    // Re-theme the existing chart objects in place: swap each chart's Layout for a freshly themed one
    // while preserving its captured PlotlyChart ref and Data. Rebuilding the AttributeChart objects
    // instead would orphan the @ref captures (Blazor reuses the PlotlyChart instances and does not
    // reassign the refs onto the new objects), leaving group.Chart null so React() — and the new
    // colors — never apply until a full page reload.
    private void RethemeCharts() {
        foreach (var chart in attributeCharts) {
            ApplyTheme(chart);
        }

        ApplyTheme(combinedChart);
        ApplyTheme(timelineChart);

        needsRedraw = true;
    }

    private void ApplyTheme(AttributeChart? chart) {
        if (chart?.LayoutFactory is not null) {
            chart.Layout = chart.LayoutFactory(darkMode);
        }
    }

    public void Dispose() => OptionsStorage.OptionsChanged -= OnOptionsChanged;

    private PlayerCard? card;
    private TeamCard? currentTeam;
    private TeamCard? shlRights;
    private TeamCard? smjhlRights;
    private bool loading;
    private bool notFound;
    private string? error;
    private string? jsonDataUrl;
    private string? jsonFileName;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) {
        WriteIndented = true,
    };

    private readonly Config chartConfig = new() { Responsive = true };
    private readonly List<AttributeChart> attributeCharts = new();
    private AttributeChart? combinedChart;
    private bool groupedView = true;
    private bool needsRedraw;

    private const string AttributesTabId = "attributes";
    private const string TimelineTabId = "tpe-timeline";
    private string activeTabId = AttributesTabId;

    private AttributeChart? timelineChart;
    private bool timelineLoading;
    private bool timelineLoaded;
    private string? timelineError;

    protected override async Task OnParametersSetAsync() => await LoadAsync();

    private async Task LoadAsync() {
        loading = true;
        notFound = false;
        error = null;
        card = null;
        currentTeam = null;
        shlRights = null;
        smjhlRights = null;
        attributeCharts.Clear();
        combinedChart = null;
        jsonDataUrl = null;
        jsonFileName = null;
        activeTabId = AttributesTabId;
        timelineChart = null;
        timelineLoading = false;
        timelineLoaded = false;
        timelineError = null;

        try {
            card = await PlayerClient.GetPlayer(PlayerId);
            if (card is null) {
                notFound = true;
            } else {
                BuildDownload(card);
                await LoadTeamsAsync(card);
                if (card.Attributes is not null) {
                    BuildCharts(card);
                    needsRedraw = true;
                }
            }
        } catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.NotFound) {
            notFound = true;
        } catch (Exception ex) {
            error = ex.Message;
        } finally {
            loading = false;
        }
    }

    private void OnViewChanged() => needsRedraw = true;

    // The TPE timeline is loaded lazily the first time its tab is opened so profiles that are never
    // inspected for progression don't incur the extra backend call. Every tab change also flags a
    // redraw so the now-visible chart resizes to its container (hidden charts render at zero size).
    private async Task OnTabChangedAsync() {
        if (activeTabId == TimelineTabId && !timelineLoaded && !timelineLoading) {
            await LoadTimelineAsync();
        }

        needsRedraw = true;
    }

    private async Task LoadTimelineAsync() {
        timelineLoading = true;
        timelineError = null;
        StateHasChanged();

        try {
            var timeline = await PlayerClient.GetPlayerTpeTimeline(PlayerId);
            BuildTimeline(timeline);
        } catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.NotFound) {
            timelineChart = null;
        } catch (Exception ex) {
            timelineError = ex.Message;
        } finally {
            timelineLoading = false;
            timelineLoaded = true;
            needsRedraw = true;
        }
    }

    private void BuildDownload(PlayerCard c) {
        var json = JsonSerializer.Serialize(c, JsonOptions);
        jsonDataUrl = "data:application/json;charset=utf-8," + Uri.EscapeDataString(json);
        jsonFileName = $"player-{c.PlayerId}.json";
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

    private void BuildTimeline(IReadOnlyList<TpeTimelinePoint>? timeline) {
        timelineChart = null;

        if (timeline is null || timeline.Count == 0) {
            return;
        }

        timelineChart = new AttributeChart {
            Title = "Total TPE",
            Layout = BuildTimelineLayout(darkMode),
            LayoutFactory = BuildTimelineLayout,
            Data = new List<ITrace> {
                new Plotly.Blazor.Traces.Scatter {
                    X = timeline.Select(p => (object)p.TaskDate).ToList(),
                    Y = timeline.Select(p => (object)p.TotalTpe).ToList(),
                    Mode = Plotly.Blazor.Traces.ScatterLib.ModeFlag.Lines | Plotly.Blazor.Traces.ScatterLib.ModeFlag.Markers,
                    // A step ("hv") line reflects that the cumulative total holds until the next event.
                    Line = new Plotly.Blazor.Traces.ScatterLib.Line {
                        Shape = Plotly.Blazor.Traces.ScatterLib.LineLib.ShapeEnum.Hv,
                    },
                    Name = "Total TPE",
                },
            },
        };
    }

    private void BuildCharts(PlayerCard c) {
        var set = PlayerAttributeCharts.Build(c.Attributes!, darkMode);
        attributeCharts.Clear();
        combinedChart = set.Combined;
        attributeCharts.AddRange(set.Categories);
    }

    private string? ExcludedAttributeLabels =>
        card?.Attributes is { } attributes ? PlayerAttributeCharts.ExcludedAttributeLabels(attributes) : null;

    // Themed cartesian layout for the TPE timeline: a date x-axis with an auto-ranged TPE y-axis.
    private static Plotly.Blazor.Layout BuildTimelineLayout(bool dark) =>
        PlayerAttributeCharts.BuildTimelineLayout(dark);

    private string PositionText => card?.Position.ToShortString() ?? string.Empty;

    private static readonly CultureInfo UsdCulture = CreateUsdCulture();

    private static CultureInfo CreateUsdCulture() {
        var culture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
        culture.NumberFormat.CurrencySymbol = "$";
        culture.NumberFormat.CurrencyPositivePattern = 0;
        culture.NumberFormat.CurrencyNegativePattern = 1;
        return culture;
    }

    private string HandednessText => card is null ? string.Empty : $"Shoots {card.Handedness.ToValueString()}";

    private BadgeColor StatusColor => card?.Status switch {
        PlayerStatus.Active => BadgeColor.Success,
        PlayerStatus.Retired => BadgeColor.Subtle,
        PlayerStatus.Pending => BadgeColor.Warning,
        PlayerStatus.Denied => BadgeColor.Danger,
        _ => BadgeColor.Subtle,
    };

    private string StatusText => card?.Status switch {
        PlayerStatus.Active => "Active",
        PlayerStatus.Retired => "Retired",
        PlayerStatus.Pending => "Pending",
        PlayerStatus.Denied => "Denied",
        _ => card?.Status.ToString() ?? string.Empty,
    };

    private string StatusTooltip => card?.Status switch {
        PlayerStatus.Active => "Current, non-retired player.",
        PlayerStatus.Retired => "This player has retired.",
        PlayerStatus.Pending => "Player creation is pending approval.",
        PlayerStatus.Denied => "Player creation was denied.",
        _ => string.Empty,
    };

    private IEnumerable<StatRow> Progression {
        get {
            if (card is null) {
                yield break;
            }

            yield return new StatRow("Total TPE", card.TotalTpe.ToString("N0", CultureInfo.InvariantCulture));
            yield return new StatRow("Applied TPE", card.AppliedTpe.ToString("N0", CultureInfo.InvariantCulture));

            var bankedNegative = card.BankedTpe < 0;
            yield return new StatRow(
                "Banked TPE",
                card.BankedTpe.ToString("N0", CultureInfo.InvariantCulture),
                Negative: bankedNegative,
                Tooltip: bankedNegative ? "Regression penalties have not been applied yet." : null,
                AnchorId: bankedNegative ? "banked-tpe-value" : null);

            yield return new StatRow("Bank balance", card.BankBalance.ToString("C0", UsdCulture));
        }
    }

    private sealed record StatRow(string Label, string Value, bool Negative = false, string? Tooltip = null, string? AnchorId = null);

    // Resolves the team badges for the player's current team and SHL/SMJHL rights. Each lookup is
    // independent and failure-tolerant: an unresolved team simply falls back to its raw id badge, so
    // a team endpoint hiccup never blocks the rest of the profile from rendering.
    private async Task LoadTeamsAsync(PlayerCard c) {
        var tasks = new List<Task>();

        if (c is { CurrentTeamId: { } currentTeamId, CurrentLeague: { } currentLeague }) {
            tasks.Add(ResolveTeamAsync(currentLeague.Abbreviation, currentTeamId, team => currentTeam = team));
        }

        if (c.ShlRightsTeamId is { } shlTeamId) {
            tasks.Add(ResolveTeamAsync(KnownLeague.Shl.Abbreviation, shlTeamId, team => shlRights = team));
        }

        if (c.SmjhlRightsTeamId is { } smjhlTeamId) {
            tasks.Add(ResolveTeamAsync(KnownLeague.Smjhl.Abbreviation, smjhlTeamId, team => smjhlRights = team));
        }

        if (tasks.Count > 0) {
            await Task.WhenAll(tasks);
        }
    }

    private async Task ResolveTeamAsync(string league, int teamId, Action<TeamCard?> assign) {
        try {
            assign(await LeagueClient.GetTeam(league, teamId));
        } catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.NotFound) {
            assign(null);
        } catch (Exception ex) {
            // A failed team lookup is non-fatal; log and fall back to the raw id badge.
            Console.Error.WriteLine($"Failed to resolve team {league}/{teamId}: {ex.Message}");
            assign(null);
        }
    }

    private IEnumerable<(string Label, string Value)> Details {
        get {
            if (card is null) {
                yield break;
            }

            yield return ("Handedness", card.Handedness.ToString());
            yield return ("Height", card.Height?.ToString() ?? "—");
            yield return ("Weight", card.Weight is int w ? $"{w} lbs" : "—");
            yield return ("Birthplace", string.IsNullOrWhiteSpace(card.Birthplace) ? "—" : card.Birthplace);
            yield return ("Nation", string.IsNullOrWhiteSpace(card.IihfNation) ? "—" : card.IihfNation);
            yield return ("Created", card.CreationDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));

            if (card.RetirementDate is DateTime retired) {
                yield return ("Retired", retired.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
            }
        }
    }
}
