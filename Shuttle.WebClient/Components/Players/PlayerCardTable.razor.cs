using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;
using Shuttle.Models.Players;
using Shuttle.Shl.Api.Models.Portal.V1;
using Shuttle.WebClient.Models;

namespace Shuttle.WebClient.Components.Players;

/// <summary>
/// Reusable table that renders a list of <see cref="PlayerCard"/>s with links to each player's
/// profile. When <see cref="SortChanged"/> is wired the sortable column headers become clickable and
/// raise sort requests to the parent (server-side sorting); otherwise the headers are static, so the
/// component can also present plain player lists elsewhere on the site.
/// </summary>
public partial class PlayerCardTable : ComponentBase {
    /// <summary>The players to display.</summary>
    [Parameter] public IReadOnlyList<PlayerCard>? Players { get; set; }

    /// <summary>Whether a load is in progress (shows a spinner row).</summary>
    [Parameter] public bool Loading { get; set; }

    /// <summary>The currently active sort field, if any.</summary>
    [Parameter] public PlayerSortField? SortField { get; set; }

    /// <summary>Whether the active sort is descending.</summary>
    [Parameter] public bool SortDescending { get; set; }

    /// <summary>Raised when the user clicks a sortable header. When unset, sorting is disabled.</summary>
    [Parameter] public EventCallback<PlayerTableSort> SortChanged { get; set; }

    private bool Sortable => SortChanged.HasDelegate;

    private async Task ToggleSort(PlayerSortField field) {
        if (!Sortable) {
            return;
        }

        // Same column flips direction; a new column starts ascending.
        var descending = SortField == field && !SortDescending;
        await SortChanged.InvokeAsync(new PlayerTableSort(field, descending));
    }

    private string ThClass(PlayerSortField field) {
        if (!Sortable) {
            return string.Empty;
        }

        return SortField == field ? "th-sort active" : "th-sort";
    }

    private string SortIndicator(PlayerSortField field) =>
        SortField == field ? (SortDescending ? " \u25be" : " \u25b4") : string.Empty;

    private static string PlayerRoute(int playerId) => Routes.Players.Player(playerId);

    private static BadgeColor StatusColor(PlayerStatus status) => status switch {
        PlayerStatus.Active => BadgeColor.Success,
        PlayerStatus.Retired => BadgeColor.Subtle,
        PlayerStatus.Pending => BadgeColor.Warning,
        PlayerStatus.Denied => BadgeColor.Danger,
        _ => BadgeColor.Subtle,
    };

    private static string StatusText(PlayerStatus status) => status switch {
        PlayerStatus.Active => "Active",
        PlayerStatus.Retired => "Retired",
        PlayerStatus.Pending => "Pending",
        PlayerStatus.Denied => "Denied",
        _ => status.ToString(),
    };

    /// <summary>A sort request raised by a column header.</summary>
    public readonly record struct PlayerTableSort(PlayerSortField Field, bool Descending);
}
