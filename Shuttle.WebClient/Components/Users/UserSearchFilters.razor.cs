using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;
using Shuttle.Models.Users;
using Shuttle.WebClient.Services;

namespace Shuttle.WebClient.Components.Users;

/// <summary>
/// Filter panel for the user search page. Holds a local, editable filter model and raises
/// <see cref="OnSearch"/> with a <see cref="UserSearchQuery"/> containing only the filter fields (the
/// parent owns paging and sorting). The Discord toggle is only offered when
/// <see cref="CanSearchDiscord"/> is set (i.e. the caller is authenticated).
/// </summary>
public partial class UserSearchFilters : ComponentBase {
    /// <summary>Raised when the user applies the filters. Carries the filter portion of the query.</summary>
    [Parameter] public EventCallback<UserSearchQuery> OnSearch { get; set; }

    /// <summary>Raised when the user clears the filters.</summary>
    [Parameter] public EventCallback OnReset { get; set; }

    /// <summary>Whether a search is currently running (disables the buttons).</summary>
    [Parameter] public bool Busy { get; set; }

    /// <summary>Whether the caller may search by Discord name (authenticated users only).</summary>
    [Parameter] public bool CanSearchDiscord { get; set; }

    /// <summary>
    /// The query whose filter fields seed the editable state. Re-applied whenever a new instance is
    /// supplied (e.g. after URL-driven navigation), so the panel always mirrors the active query.
    /// </summary>
    [Parameter] public UserSearchQuery? Initial { get; set; }

    /// <summary>Directory-backed autocomplete for the username box.</summary>
    [Inject] private IUserDirectoryService Directory { get; set; } = null!;

    private object? loadedInitial;

    private string? text;
    private bool searchDiscord;
    private UserSuggestion? selectedUser;

    protected override void OnParametersSet() {
        if (ReferenceEquals(Initial, loadedInitial)) {
            return;
        }

        loadedInitial = Initial;
        text = Initial?.Text;
        searchDiscord = Initial?.SearchDiscord ?? false;
        selectedUser = null;
    }

    // FluentAutocomplete populates its dropdown from this handler; an empty search matches all.
    private async Task OnNameSearch(OptionsSearchEventArgs<UserSuggestion> e) {
        // Capture the raw typed text so a free-form (unselected) term still drives the search.
        text = string.IsNullOrWhiteSpace(e.Text) ? null : e.Text.Trim();
        e.Items = await Directory.Search(e.Text);
    }

    private void OnNameSelected(UserSuggestion? user) {
        selectedUser = user;
        if (user is not null) {
            text = user.Username;
        }
    }

    private async Task SearchAsync() => await OnSearch.InvokeAsync(BuildQuery());

    private UserSearchQuery BuildQuery() =>
        new() {
            Text = Clean(text),
            SearchDiscord = CanSearchDiscord && searchDiscord,
        };

    private async Task ResetAsync() {
        text = null;
        searchDiscord = false;
        selectedUser = null;
        await OnReset.InvokeAsync();
    }

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
