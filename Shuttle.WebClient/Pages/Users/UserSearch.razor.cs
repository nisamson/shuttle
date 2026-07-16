using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.FluentUI.AspNetCore.Components;
using Refit;
using Shuttle.Api.Client;
using Shuttle.Models.Users;
using Shuttle.WebClient.Components.Users;
using Shuttle.WebClient.Models;
using Shuttle.WebClient.Services;

namespace Shuttle.WebClient.Pages.Users;

public partial class UserSearch : ComponentBase, IDisposable {
    [Inject] private IShuttleUserClient UserClient { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;
    [Inject] private IUserDirectoryService Directory { get; set; } = null!;
    [Inject] private AuthenticationStateProvider AuthState { get; set; } = null!;

    private readonly PaginationState pagination = new() { ItemsPerPage = 25 };

    private UserSearchQuery query = new();
    private IReadOnlyList<UserCard> results = [];
    private bool loading;
    private bool isAuthenticated;
    private string? errorMessage;

    private UserSuggestion? jumpSelection;

    protected override async Task OnInitializedAsync() {
        pagination.TotalItemCountChanged += OnPaginationTotalChanged;
        Navigation.LocationChanged += OnLocationChanged;

        var authState = await AuthState.GetAuthenticationStateAsync();
        isAuthenticated = authState.User.Identity?.IsAuthenticated == true;

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
    private void OnSearch(UserSearchQuery filters) {
        // A new filter set always starts from the first page but keeps the current sort.
        Navigate(filters with {
            Page = 1,
            PageSize = query.PageSize,
            SortBy = query.SortBy,
            SortDescending = query.SortDescending,
        });
    }

    private void OnReset() {
        // Navigating to the bare route reparses to the default query.
        Navigation.NavigateTo(Routes.Users.Root);
    }

    private void OnSortChanged(UserCardTable.UserTableSort sort) {
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

    private void Navigate(UserSearchQuery target) => Navigation.NavigateTo(BuildUri(target));

    private async Task SearchAsync() {
        loading = true;
        errorMessage = null;
        StateHasChanged();

        try {
            var result = await UserClient.SearchUsers(query);
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
            errorMessage = $"Failed to load users ({(int)ex.StatusCode}).";
            results = [];
        } catch (HttpRequestException) {
            errorMessage = "Failed to reach the server. Please try again.";
            results = [];
        } finally {
            loading = false;
            StateHasChanged();
        }
    }

    private string BuildUri(UserSearchQuery target) {
        var parts = new List<string>();

        void Add(string key, string? value) {
            if (!string.IsNullOrEmpty(value)) {
                parts.Add($"{key}={Uri.EscapeDataString(value)}");
            }
        }

        Add("q", target.Text);

        if (target.SearchDiscord) {
            Add("discord", "true");
        }

        if (target.Page > 1) {
            Add("page", target.Page.ToString(CultureInfo.InvariantCulture));
        }

        if (target.SortBy != UserSortField.Username) {
            Add("sort", target.SortBy.ToString());
        }

        if (target.SortDescending) {
            Add("desc", "true");
        }

        var path = Routes.Users.Root;
        return parts.Count > 0 ? $"{path}?{string.Join('&', parts)}" : path;
    }

    private UserSearchQuery ParseQueryFromUri() {
        var parsed = ParseQueryString(Navigation.ToAbsoluteUri(Navigation.Uri).Query);

        string? First(string key) =>
            parsed.TryGetValue(key, out var values) ? values.FirstOrDefault() : null;

        return new UserSearchQuery {
            Text = First("q"),
            SearchDiscord = ParseBool(First("discord")) ?? false,
            Page = ParseInt(First("page")) ?? 1,
            SortBy = Enum.TryParse<UserSortField>(First("sort"), true, out var sortBy) ? sortBy : UserSortField.Username,
            SortDescending = ParseBool(First("desc")) ?? false,
        };
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

    private static bool QueriesEqual(UserSearchQuery a, UserSearchQuery b) =>
        a.Text == b.Text
        && a.SearchDiscord == b.SearchDiscord
        && a.Page == b.Page
        && a.PageSize == b.PageSize
        && a.SortBy == b.SortBy
        && a.SortDescending == b.SortDescending;

    private async Task OnJumpSearch(OptionsSearchEventArgs<UserSuggestion> e) {
        e.Items = await Directory.Search(e.Text);
    }

    private void OnJumpSelected(UserSuggestion? user) {
        jumpSelection = user;
        if (user is not null) {
            Navigation.NavigateTo(Routes.Users.User(user.UserId));
        }
    }
}
