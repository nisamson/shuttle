using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;
using Shuttle.Models.Players;
using Shuttle.Shl.Api.Models.Common;
using Shuttle.Shl.Api.Models.Portal.V1;
using Shuttle.WebClient.Services;

namespace Shuttle.WebClient.Components.Players;

/// <summary>
/// Filter panel for the player search page. Holds a local, editable filter model and raises
/// <see cref="OnSearch"/> with a <see cref="PlayerSearchQuery"/> containing only the filter fields
/// (the parent owns paging and sorting). The enum-like filters are multiselect chip groups; the
/// less-common filters are hidden behind an "Advanced" toggle.
/// </summary>
public partial class PlayerSearchFilters : ComponentBase {
    /// <summary>Raised when the user applies the filters. Carries the filter portion of the query.</summary>
    [Parameter] public EventCallback<PlayerSearchQuery> OnSearch { get; set; }

    /// <summary>Raised when the user clears the filters.</summary>
    [Parameter] public EventCallback OnReset { get; set; }

    /// <summary>Whether a search is currently running (disables the buttons).</summary>
    [Parameter] public bool Busy { get; set; }

    /// <summary>Directory-backed autocomplete for the name/username box.</summary>
    [Inject] private IPlayerDirectoryService Directory { get; set; } = null!;

    /// <summary>
    /// The query whose filter fields seed the editable state. Re-applied whenever a new instance is
    /// supplied (e.g. after URL-driven navigation), so the panel always mirrors the active query.
    /// </summary>
    [Parameter] public PlayerSearchQuery? Initial { get; set; }

    private object? loadedInitial;

    private static readonly string[] PositionOptions = ["G", "C", "LW", "RW", "LD", "RD"];
    private static readonly PlayerStatus[] StatusOptions = Enum.GetValues<PlayerStatus>();

    // IIHF and WJC are intentionally excluded from the league filter.
    private static readonly KnownLeague[] LeagueOptions = [KnownLeague.Shl, KnownLeague.Smjhl];
    private static readonly PlayerHandedness[] AllHandedness = Enum.GetValues<PlayerHandedness>();

    private string? text;
    private PlayerSuggestion? selectedNamePlayer;
    private IEnumerable<string> selectedPositions = [];
    private IEnumerable<PlayerStatus> selectedStatuses = [];
    private readonly HashSet<KnownLeague> leagues = [];
    private readonly HashSet<PlayerHandedness> handedness = [];
    private string? draftSeasonText;
    private string? minTotalTpeText;
    private string? maxTotalTpeText;
    private string? iihfNation;
    private string? minBankBalanceText;
    private string? maxBankBalanceText;
    private TriState inactiveFilter = TriState.Any;
    private TriState suspendedFilter = TriState.Any;
    private TriState recreateFilter = TriState.Any;
    private bool showAdvanced;

    private void ToggleLeague(KnownLeague league) => Toggle(leagues, league);
    private void ToggleHandedness(PlayerHandedness hand) => Toggle(handedness, hand);

    // FluentAutocomplete populates its dropdown from these handlers; an empty search matches all.
    private async Task OnNameSearch(OptionsSearchEventArgs<PlayerSuggestion> e) {
        // Capture the raw typed text so a free-form (unselected) term still drives the search.
        text = string.IsNullOrWhiteSpace(e.Text) ? null : e.Text.Trim();
        e.Items = await Directory.Search(e.Text);
    }

    private void OnNameSelected(PlayerSuggestion? player) {
        selectedNamePlayer = player;
        if (player is not null) {
            text = player.Name;
        }
    }

    private void OnPositionSearch(OptionsSearchEventArgs<string> e) =>
        e.Items = string.IsNullOrWhiteSpace(e.Text)
            ? PositionOptions
            : PositionOptions.Where(p => p.Contains(e.Text, StringComparison.OrdinalIgnoreCase));

    private void OnStatusSearch(OptionsSearchEventArgs<PlayerStatus> e) =>
        e.Items = string.IsNullOrWhiteSpace(e.Text)
            ? StatusOptions
            : StatusOptions.Where(s => StatusLabel(s).Contains(e.Text, StringComparison.OrdinalIgnoreCase));

    private static void Toggle<T>(HashSet<T> set, T value) {
        if (!set.Add(value)) {
            set.Remove(value);
        }
    }

    protected override void OnParametersSet() {
        if (ReferenceEquals(Initial, loadedInitial)) {
            return;
        }

        loadedInitial = Initial;
        LoadFrom(Initial);
    }

    private void LoadFrom(PlayerSearchQuery? source) {
        text = source?.Text;
        selectedNamePlayer = null;
        iihfNation = source?.IihfNation;
        draftSeasonText = source?.DraftSeason?.ToString(CultureInfo.InvariantCulture);
        minTotalTpeText = source?.MinTotalTpe?.ToString(CultureInfo.InvariantCulture);
        maxTotalTpeText = source?.MaxTotalTpe?.ToString(CultureInfo.InvariantCulture);
        minBankBalanceText = source?.MinBankBalance?.ToString(CultureInfo.InvariantCulture);
        maxBankBalanceText = source?.MaxBankBalance?.ToString(CultureInfo.InvariantCulture);

        selectedPositions = source?.Positions?.ToList() ?? [];
        selectedStatuses = source?.Statuses?.ToList() ?? [];

        leagues.Clear();
        if (source?.Leagues is not null) {
            leagues.UnionWith(source.Leagues);
        }

        handedness.Clear();
        if (source?.Handedness is not null) {
            handedness.UnionWith(source.Handedness);
        }

        inactiveFilter = FromBool(source?.Inactive);
        suspendedFilter = FromBool(source?.Suspended);
        recreateFilter = FromBool(source?.Recreate);

        // Reveal the advanced section when it holds any active filters.
        showAdvanced = showAdvanced
            || handedness.Count > 0
            || inactiveFilter != TriState.Any
            || suspendedFilter != TriState.Any
            || recreateFilter != TriState.Any
            || !string.IsNullOrEmpty(iihfNation)
            || !string.IsNullOrEmpty(minBankBalanceText)
            || !string.IsNullOrEmpty(maxBankBalanceText);
    }

    private static ButtonAppearance ChipAppearance(bool selected) =>
        selected ? ButtonAppearance.Primary : ButtonAppearance.Outline;

    private static string StatusLabel(PlayerStatus status) => status switch {
        PlayerStatus.Active => "Active",
        PlayerStatus.Retired => "Retired",
        PlayerStatus.Pending => "Pending",
        PlayerStatus.Denied => "Denied",
        _ => status.ToString(),
    };

    private async Task SearchAsync() {
        await OnSearch.InvokeAsync(BuildQuery());
    }

    private PlayerSearchQuery BuildQuery() =>
        new() {
            Text = Clean(text),
            Positions = selectedPositions.Any() ? selectedPositions.ToList() : null,
            Statuses = selectedStatuses.Any() ? selectedStatuses.ToList() : null,
            Leagues = leagues.Count > 0 ? leagues.ToList() : null,
            Handedness = handedness.Count > 0 ? handedness.ToList() : null,
            DraftSeason = ParseInt(draftSeasonText),
            MinTotalTpe = ParseInt(minTotalTpeText),
            MaxTotalTpe = ParseInt(maxTotalTpeText),
            IihfNation = Clean(iihfNation),
            Inactive = ToBool(inactiveFilter),
            Suspended = ToBool(suspendedFilter),
            Recreate = ToBool(recreateFilter),
            MinBankBalance = ParseInt(minBankBalanceText),
            MaxBankBalance = ParseInt(maxBankBalanceText),
        };

    private async Task ResetAsync() {
        text = null;
        selectedNamePlayer = null;
        selectedPositions = [];
        selectedStatuses = [];
        leagues.Clear();
        handedness.Clear();
        draftSeasonText = null;
        minTotalTpeText = null;
        maxTotalTpeText = null;
        iihfNation = null;
        minBankBalanceText = null;
        maxBankBalanceText = null;
        inactiveFilter = TriState.Any;
        suspendedFilter = TriState.Any;
        recreateFilter = TriState.Any;

        await OnReset.InvokeAsync();
    }

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static int? ParseInt(string? value) =>
        int.TryParse(value, out var parsed) ? parsed : null;

    private static bool? ToBool(TriState value) => value switch {
        TriState.Yes => true,
        TriState.No => false,
        _ => null,
    };

    private static TriState FromBool(bool? value) => value switch {
        true => TriState.Yes,
        false => TriState.No,
        _ => TriState.Any,
    };

    private enum TriState {
        Any,
        Yes,
        No,
    }
}
