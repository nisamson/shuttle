using System.Net;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Refit;
using Shuttle.Api.Client;
using Shuttle.Models.Players;
using Shuttle.Models.Users;
using Shuttle.WebClient.Components.Players;
using Shuttle.WebClient.Models;

namespace Shuttle.WebClient.Pages.Users;

public partial class UserProfile : ComponentBase {
    /// <summary>The numeric user id or the username, taken from the route.</summary>
    [Parameter] public string Id { get; set; } = string.Empty;

    [Inject] private IShuttleUserClient UserClient { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;
    [Inject] private AuthenticationStateProvider AuthState { get; set; } = null!;

    private UserCard? card;
    private bool loading;
    private bool notFound;
    private bool isAuthenticated;
    private string? error;

    // Locally-sorted copy of the user's players (the profile sorts in-memory, no server round trip).
    private List<PlayerCard>? sortedPlayers;
    private PlayerSortField sortField = PlayerSortField.Created;
    private bool sortDescending = true;

    private void GoToSearch() => Navigation.NavigateTo(Routes.Users.Root);

    protected override async Task OnInitializedAsync() {
        var authState = await AuthState.GetAuthenticationStateAsync();
        isAuthenticated = authState.User.Identity?.IsAuthenticated == true;
    }

    protected override async Task OnParametersSetAsync() => await LoadAsync();

    private async Task LoadAsync() {
        loading = true;
        notFound = false;
        error = null;
        card = null;
        sortedPlayers = null;

        try {
            card = await UserClient.GetUser(Id, players: true);
            if (card is null) {
                notFound = true;
            } else {
                sortedPlayers = SortPlayers(card.Players ?? []);
            }
        } catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.NotFound) {
            notFound = true;
        } catch (ApiException ex) {
            error = $"Failed to load user ({(int)ex.StatusCode}).";
        } catch (HttpRequestException) {
            error = "Failed to reach the server. Please try again.";
        } finally {
            loading = false;
        }
    }

    private void OnSortChanged(PlayerCardTable.PlayerTableSort sort) {
        sortField = sort.Field;
        sortDescending = sort.Descending;
        if (card?.Players is not null) {
            sortedPlayers = SortPlayers(card.Players);
        }
    }

    private List<PlayerCard> SortPlayers(IReadOnlyList<PlayerCard> players) {
        // PlayerId is a stable tiebreaker so the ordering is deterministic.
        IEnumerable<PlayerCard> Order<TKey>(Func<PlayerCard, TKey> key) =>
            (sortDescending ? players.OrderByDescending(key) : players.OrderBy(key))
            .ThenBy(p => p.PlayerId);

        return (sortField switch {
            PlayerSortField.Name => Order(p => p.Name),
            PlayerSortField.Username => Order(p => p.Username),
            PlayerSortField.Position => Order(p => p.Position),
            PlayerSortField.Status => Order(p => p.Status),
            PlayerSortField.League => Order(p => p.CurrentLeague),
            PlayerSortField.DraftSeason => Order(p => p.DraftSeason),
            PlayerSortField.TotalTpe => Order(p => p.TotalTpe),
            _ => Order(p => p.CreationDate),
        }).ToList();
    }
}
