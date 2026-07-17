using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.FluentUI.AspNetCore.Components;
using Refit;
using Shuttle.Api.Client;
using Shuttle.Models.Players;
using Shuttle.Shl.Api.Models.Common;
using Shuttle.Shl.Api.Models.Portal.V1;
using Shuttle.WebClient.Components.Players;
using Shuttle.WebClient.Models;
using Shuttle.WebClient.Services;

namespace Shuttle.WebClient.Pages.Players;

public partial class PlayerSearch : ComponentBase, IDisposable {
    [Inject] private IShuttlePlayerClient PlayerClient { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;
    [Inject] private IPlayerDirectoryService Directory { get; set; } = null!;

    private readonly PaginationState pagination = new() { ItemsPerPage = 25 };

    // Statuses default to current + retired players; the sentinel encodes an explicit "all".
    private const string AnyStatusSentinel = "any";
    private static readonly IReadOnlyList<PlayerStatus> DefaultStatuses = [PlayerStatus.Active, PlayerStatus.Retired];

    private PlayerSearchQuery query = new();
    private IReadOnlyList<PlayerCard> results = [];
    private bool loading;
    private string? errorMessage;

    private PlayerSuggestion? jumpSelection;

    protected override async Task OnInitializedAsync() {
        pagination.TotalItemCountChanged += OnPaginationTotalChanged;
        Navigation.LocationChanged += OnLocationChanged;

        // The URL is the single source of truth; seed the initial query from it.
        query = ParseQueryFromUri();
        await SearchAsync();
    }

    public void Dispose() {
        pagination.TotalItemCountChanged -= OnPaginationTotalChanged;
        Navigation.LocationChanged -= OnLocationChanged;
    }

    private void OnPaginationTotalChanged(object? sender, EventArgs e) => StateHasChanged();

    private async void OnLocationChanged(object? sender, LocationChangedEventArgs e) {
        var next = ParseQueryFromUri();
        if (QueriesEqual(next, query)) {
            return;
        }

        await InvokeAsync(async () => {
            query = next;
            await SearchAsync();
        });
    }

    // User actions only update the URL; the LocationChanged handler re-runs the search.
    private void OnSearch(PlayerSearchQuery filters) {
        // A new filter set always starts from the first page but keeps the current sort.
        Navigate(filters with {
            Page = 1,
            PageSize = query.PageSize,
            SortBy = query.SortBy,
            SortDescending = query.SortDescending,
        });
    }

    private void OnReset() {
        // Navigating to the bare route reparses to the default query (Active + Retired statuses).
        Navigation.NavigateTo(Routes.Players.Root);
    }

    private void OnSortChanged(PlayerCardTable.PlayerTableSort sort) {
        Navigate(query with {
            SortBy = sort.Field,
            SortDescending = sort.Descending,
            Page = 1,
        });
    }

    private void OnPageChanged() {
        var page = pagination.CurrentPageIndex + 1;
        if (page == query.Page) {
            return;
        }

        Navigate(query with { Page = page });
    }

    private void Navigate(PlayerSearchQuery target) =>
        Navigation.NavigateTo(BuildUri(target));

    private async Task SearchAsync() {
        loading = true;
        errorMessage = null;
        StateHasChanged();

        try {
            var result = await PlayerClient.SearchPlayers(query);
            results = result.Items;

            if (pagination.ItemsPerPage != result.PageSize) {
                await pagination.SetItemsPerPageAsync(result.PageSize);
            }

            await pagination.SetTotalItemCountAsync(result.TotalCount);

            var targetIndex = Math.Max(0, result.Page - 1);
            if (pagination.CurrentPageIndex != targetIndex) {
                await pagination.SetCurrentPageIndexAsync(targetIndex);
            }
        } catch (ApiException ex) {
            errorMessage = $"Failed to load players ({(int)ex.StatusCode}).";
            results = [];
        } catch (HttpRequestException) {
            errorMessage = "Failed to reach the server. Please try again.";
            results = [];
        } finally {
            loading = false;
            StateHasChanged();
        }
    }

    private string BuildUri(PlayerSearchQuery target) {
        var parts = new List<string>();

        void Add(string key, string? value) {
            if (!string.IsNullOrEmpty(value)) {
                parts.Add($"{key}={Uri.EscapeDataString(value)}");
            }
        }

        void AddAll(string key, IEnumerable<string>? values) {
            if (values is null) {
                return;
            }

            foreach (var value in values) {
                parts.Add($"{key}={Uri.EscapeDataString(value)}");
            }
        }

        void AddStatus(IReadOnlyList<PlayerStatus>? statuses) {
            // null means the user cleared the status filter (match all) -> explicit sentinel so a
            // reparse doesn't fall back to the Active + Retired default.
            if (statuses is null) {
                parts.Add($"status={AnyStatusSentinel}");
            } else {
                foreach (var status in statuses) {
                    parts.Add($"status={Uri.EscapeDataString(status.ToString())}");
                }
            }
        }

        Add("q", target.Text);
        AddAll("pos", target.Positions);
        AddStatus(target.Statuses);
        AddAll("league", target.Leagues?.Select(l => l.ToString()));
        AddAll("hand", target.Handedness?.Select(h => h.ToString()));
        Add("draft", target.DraftSeason?.ToString(CultureInfo.InvariantCulture));
        Add("minTpe", target.MinTotalTpe?.ToString(CultureInfo.InvariantCulture));
        Add("maxTpe", target.MaxTotalTpe?.ToString(CultureInfo.InvariantCulture));
        Add("nation", target.IihfNation);
        Add("inactive", target.Inactive is { } inactive ? (inactive ? "true" : "false") : null);
        Add("suspended", target.Suspended is { } suspended ? (suspended ? "true" : "false") : null);
        Add("recreate", target.Recreate is { } recreate ? (recreate ? "true" : "false") : null);
        Add("minBank", target.MinBankBalance?.ToString(CultureInfo.InvariantCulture));
        Add("maxBank", target.MaxBankBalance?.ToString(CultureInfo.InvariantCulture));

        if (target.Page > 1) {
            Add("page", target.Page.ToString(CultureInfo.InvariantCulture));
        }

        if (target.SortBy != PlayerSortField.Name) {
            Add("sort", target.SortBy.ToString());
        }

        if (target.SortDescending) {
            Add("desc", "true");
        }

        var path = Routes.Players.Root;
        return parts.Count > 0 ? $"{path}?{string.Join('&', parts)}" : path;
    }

    private PlayerSearchQuery ParseQueryFromUri() {
        var parsed = ParseQueryString(Navigation.ToAbsoluteUri(Navigation.Uri).Query);

        string? First(string key) =>
            parsed.TryGetValue(key, out var values) ? values.FirstOrDefault() : null;

        IReadOnlyList<string>? Many(string key) =>
            parsed.TryGetValue(key, out var values) && values.Count > 0 ? values : null;

        return new PlayerSearchQuery {
            Text = First("q"),
            Positions = Many("pos"),
            Statuses = ParseStatuses(Many("status")),
            Leagues = ParseEnums<KnownLeague>(Many("league")),
            Handedness = ParseEnums<PlayerHandedness>(Many("hand")),
            DraftSeason = ParseInt(First("draft")),
            MinTotalTpe = ParseInt(First("minTpe")),
            MaxTotalTpe = ParseInt(First("maxTpe")),
            IihfNation = First("nation"),
            Inactive = ParseBool(First("inactive")),
            Suspended = ParseBool(First("suspended")),
            Recreate = ParseBool(First("recreate")),
            MinBankBalance = ParseInt(First("minBank")),
            MaxBankBalance = ParseInt(First("maxBank")),
            Page = ParseInt(First("page")) ?? 1,
            SortBy = Enum.TryParse<PlayerSortField>(First("sort"), true, out var sortBy) ? sortBy : PlayerSortField.Name,
            SortDescending = ParseBool(First("desc")) ?? false,
        };
    }

    private static IReadOnlyList<PlayerStatus>? ParseStatuses(IReadOnlyList<string>? values) {
        // Absent -> apply the default; explicit sentinel -> match all; otherwise the parsed set.
        if (values is null) {
            return DefaultStatuses;
        }

        if (values.Any(v => string.Equals(v, AnyStatusSentinel, StringComparison.OrdinalIgnoreCase))) {
            return null;
        }

        return ParseEnums<PlayerStatus>(values);
    }

    private static IReadOnlyList<T>? ParseEnums<T>(IReadOnlyList<string>? values) where T : struct, Enum {        if (values is null) {
            return null;
        }

        var result = new List<T>();
        foreach (var value in values) {
            if (Enum.TryParse<T>(value, true, out var parsed)) {
                result.Add(parsed);
            }
        }

        return result.Count > 0 ? result : null;
    }

    private static int? ParseInt(string? value) =>
        int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) ? parsed : null;

    private static bool? ParseBool(string? value) =>
        bool.TryParse(value, out var parsed) ? parsed : null;

    private static Dictionary<string, List<string>> ParseQueryString(string queryString) {
        var result = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrEmpty(queryString)) {
            return result;
        }

        foreach (var pair in queryString.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries)) {
            var separator = pair.IndexOf('=');
            var key = separator >= 0 ? pair[..separator] : pair;
            var value = separator >= 0 ? pair[(separator + 1)..] : string.Empty;
            key = Uri.UnescapeDataString(key);
            value = Uri.UnescapeDataString(value);

            if (!result.TryGetValue(key, out var list)) {
                list = [];
                result[key] = list;
            }

            list.Add(value);
        }

        return result;
    }

    private static bool QueriesEqual(PlayerSearchQuery a, PlayerSearchQuery b) =>
        a.Text == b.Text
        && a.DraftSeason == b.DraftSeason
        && a.MinTotalTpe == b.MinTotalTpe
        && a.MaxTotalTpe == b.MaxTotalTpe
        && a.IihfNation == b.IihfNation
        && a.Inactive == b.Inactive
        && a.Suspended == b.Suspended
        && a.Recreate == b.Recreate
        && a.MinBankBalance == b.MinBankBalance
        && a.MaxBankBalance == b.MaxBankBalance
        && a.Page == b.Page
        && a.PageSize == b.PageSize
        && a.SortBy == b.SortBy
        && a.SortDescending == b.SortDescending
        && SequenceEqual(a.Positions, b.Positions)
        && SequenceEqual(a.Statuses, b.Statuses)
        && SequenceEqual(a.Leagues, b.Leagues)
        && SequenceEqual(a.Handedness, b.Handedness);

    private static bool SequenceEqual<T>(IReadOnlyList<T>? a, IReadOnlyList<T>? b) {
        if (a is null || b is null) {
            return a is null && b is null;
        }

        return a.SequenceEqual(b);
    }

    private async Task OnJumpSearch(OptionsSearchEventArgs<PlayerSuggestion> e) {
        e.Items = await Directory.Search(e.Text);
    }

    private void OnJumpSelected(PlayerSuggestion? player) {
        jumpSelection = player;
        if (player is not null) {
            Navigation.NavigateTo(Routes.Players.Player(player.PlayerId));
        }
    }
}
