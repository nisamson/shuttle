using Microsoft.AspNetCore.Components;
using Shuttle.Models.Users;
using Shuttle.WebClient.Models;

namespace Shuttle.WebClient.Components.Users;

/// <summary>
/// Reusable table that renders a list of <see cref="UserCard"/>s with links to each user's profile.
/// The Discord column is only shown when <see cref="ShowDiscord"/> is set (i.e. the caller is
/// authenticated). When <see cref="SortChanged"/> is wired the column headers become clickable and
/// raise sort requests to the parent; otherwise the headers are static.
/// </summary>
public partial class UserCardTable : ComponentBase {
    /// <summary>The users to display.</summary>
    [Parameter] public IReadOnlyList<UserCard>? Users { get; set; }

    /// <summary>Whether a load is in progress (shows a spinner row).</summary>
    [Parameter] public bool Loading { get; set; }

    /// <summary>Whether to show the Discord column (only meaningful for authenticated callers).</summary>
    [Parameter] public bool ShowDiscord { get; set; }

    /// <summary>The currently active sort field, if any.</summary>
    [Parameter] public UserSortField? SortField { get; set; }

    /// <summary>Whether the active sort is descending.</summary>
    [Parameter] public bool SortDescending { get; set; }

    /// <summary>Raised when the user clicks a sortable header. When unset, sorting is disabled.</summary>
    [Parameter] public EventCallback<UserTableSort> SortChanged { get; set; }

    private bool Sortable => SortChanged.HasDelegate;

    private int ColumnCount => ShowDiscord ? 3 : 2;

    private async Task ToggleSort(UserSortField field) {
        if (!Sortable) {
            return;
        }

        // Same column flips direction; a new column starts ascending.
        var descending = SortField == field && !SortDescending;
        await SortChanged.InvokeAsync(new UserTableSort(field, descending));
    }

    private string ThClass(UserSortField field) {
        if (!Sortable) {
            return string.Empty;
        }

        return SortField == field ? "th-sort active" : "th-sort";
    }

    private string SortIndicator(UserSortField field) =>
        SortField == field ? (SortDescending ? " \u25be" : " \u25b4") : string.Empty;

    private static string UserRoute(int userId) => Routes.Users.User(userId);

    /// <summary>A sort request raised by a column header.</summary>
    public readonly record struct UserTableSort(UserSortField Field, bool Descending);
}
