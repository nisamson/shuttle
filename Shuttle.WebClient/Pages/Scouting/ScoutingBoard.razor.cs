using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.FluentUI.AspNetCore.Components;
using Refit;
using Shuttle.Api.Client;
using Shuttle.Models.Players;
using Shuttle.Models.Scouting;
using Shuttle.Shl.Api.Models.Common;
using Shuttle.WebClient.Models;
using Shuttle.WebClient.Services;

namespace Shuttle.WebClient.Pages.Scouting;

public partial class ScoutingBoard : ComponentBase {
    [Inject] private IShuttleScoutingClient ScoutingClient { get; set; } = null!;
    [Inject] private IShuttlePlayerClient PlayerClient { get; set; } = null!;
    [Inject] private ICurrentUserService CurrentUser { get; set; } = null!;
    [Inject] private IPlayerDirectoryService Directory { get; set; } = null!;

    [CascadingParameter] private Task<AuthenticationState>? AuthState { get; set; }

    [Parameter] public Guid BoardId { get; set; }

    private ScoutingBoardDetail? board;
    private ScoutingTeamRole? myRole;
    private Guid? currentUserId;
    private bool isAdmin;
    private bool loading = true;
    private bool busy;
    private string? loadError;
    private string? actionError;

    private IReadOnlyList<ScoutingComment> boardComments = [];
    private readonly Dictionary<int, IReadOnlyList<ScoutingComment>> entryComments = new();
    private readonly HashSet<int> expandedEntries = [];
    private readonly Dictionary<int, PlayerCard> playerCards = new();

    private PlayerSuggestion? pendingPlayer;

    private bool CanEditBoards => isAdmin || myRole >= ScoutingTeamRole.Editor;
    private bool CanComment => isAdmin || myRole >= ScoutingTeamRole.Editor;
    private bool CanModerate => isAdmin || myRole == ScoutingTeamRole.Owner;

    protected override async Task OnParametersSetAsync() {
        isAdmin = AuthState is not null && (await AuthState).User.IsInRole(RoleNames.Admin);
        currentUserId = (await CurrentUser.GetAsync())?.Id;
        await LoadAsync();
    }

    private async Task LoadAsync() {
        loading = true;
        loadError = null;
        try {
            board = await ScoutingClient.GetBoard(BoardId);
            await ResolveRoleAsync();
            await ResolvePlayerCardsAsync();
            boardComments = await ScoutingClient.GetBoardComments(BoardId);
        } catch (ApiException ex) {
            board = null;
            loadError = ex.StatusCode == System.Net.HttpStatusCode.NotFound
                ? "That board could not be found."
                : ex.StatusCode == System.Net.HttpStatusCode.Forbidden
                    ? "You do not have access to this board."
                    : $"Failed to load the board ({(int)ex.StatusCode}).";
        } catch (HttpRequestException) {
            board = null;
            loadError = "Failed to reach the server. Please try again.";
        } finally {
            loading = false;
        }
    }

    private async Task ResolveRoleAsync() {
        if (board is null) {
            return;
        }

        try {
            var team = await ScoutingClient.GetTeam(board.ScoutingTeamId);
            myRole = team.MyRole;
        } catch (ApiException) {
            myRole = null;
        }
    }

    private async Task ResolvePlayerCardsAsync() {
        if (board is null) {
            return;
        }

        foreach (var entry in board.Entries) {
            if (playerCards.ContainsKey(entry.PlayerId)) {
                continue;
            }

            try {
                var card = await PlayerClient.GetPlayer(entry.PlayerId);
                if (card is not null) {
                    playerCards[entry.PlayerId] = card;
                }
            } catch (ApiException) {
                // Leave the entry without an enriched card; the row falls back to the player id.
            }
        }
    }

    private async Task ReloadBoardAsync() {
        try {
            board = await ScoutingClient.GetBoard(BoardId);
            await ResolvePlayerCardsAsync();
        } catch (ApiException ex) {
            actionError = DescribeError(ex);
        } catch (HttpRequestException) {
            actionError = "Failed to reach the server. Please try again.";
        }
    }

    private PlayerCard? PlayerCardFor(int playerId) =>
        playerCards.TryGetValue(playerId, out var card) ? card : null;

    private string PageTitleText =>
        board is null
            ? "Scouting board"
            : board.DraftSeason is { } season
                ? $"{board.Name} - S{season}"
                : board.Name;

    private string PlayerName(int playerId) =>
        playerCards.TryGetValue(playerId, out var card) ? card.Name : $"Player #{playerId}";

    private static string PositionShort(PlayerCard card) => card.Position.ToShortString();

    private static string FormatTpe(PlayerCard card) =>
        card.TotalTpe.ToString("N0", CultureInfo.InvariantCulture);

    private static string FormatBank(PlayerCard card) =>
        card.BankBalance.ToString("C0", UsdCulture);

    // Renders bank balances as USD ($12,500) regardless of the browser's locale, matching PlayerProfile.
    private static readonly CultureInfo UsdCulture = CreateUsdCulture();

    private static CultureInfo CreateUsdCulture() {
        var culture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
        culture.NumberFormat.CurrencySymbol = "$";
        culture.NumberFormat.CurrencyPositivePattern = 0;
        culture.NumberFormat.CurrencyNegativePattern = 1;
        return culture;
    }

    private IReadOnlyList<ScoutingComment> EntryComments(int playerId) =>
        entryComments.TryGetValue(playerId, out var comments) ? comments : [];

    private async Task OnPlayerSearch(OptionsSearchEventArgs<PlayerSuggestion> e) {
        e.Items = await Directory.Search(e.Text);
    }

    private void OnPlayerSelected(PlayerSuggestion? player) {
        pendingPlayer = player;
    }

    private async Task AddEntryAsync() {
        if (pendingPlayer is null) {
            return;
        }

        var playerId = pendingPlayer.PlayerId;
        await RunAsync(async () => {
            await ScoutingClient.AddEntry(BoardId, new AddScoutingBoardEntryRequest { PlayerId = playerId });
            pendingPlayer = null;
            await ReloadBoardAsync();
        });
    }

    private async Task MoveAsync(ScoutingBoardEntry entry, int toRank) {
        await RunAsync(async () => {
            await ScoutingClient.MoveEntry(BoardId, new MoveScoutingBoardEntryRequest {
                PlayerId = entry.PlayerId,
                FromRank = entry.Rank,
                ToRank = toRank,
            });
            await ReloadBoardAsync();
        });
    }

    // Reorders entries when a card is dropped onto another: the dragged player takes the target's rank.
    private async Task OnEntryDropEnd(FluentDragEventArgs<ScoutingBoardEntry> e) {
        var source = e.Source?.Item;
        var target = e.Target?.Item;
        if (!CanEditBoards || busy || source is null || target is null || source.PlayerId == target.PlayerId) {
            return;
        }

        await MoveAsync(source, target.Rank);
    }

    private async Task RemoveAsync(ScoutingBoardEntry entry) {
        await RunAsync(async () => {
            await ScoutingClient.RemoveEntry(BoardId, entry.PlayerId);
            expandedEntries.Remove(entry.PlayerId);
            entryComments.Remove(entry.PlayerId);
            await ReloadBoardAsync();
        });
    }

    private async Task ToggleEntryCommentsAsync(ScoutingBoardEntry entry) {
        if (!expandedEntries.Add(entry.PlayerId)) {
            expandedEntries.Remove(entry.PlayerId);
            return;
        }

        await ReloadEntryCommentsAsync(entry.PlayerId);
    }

    private async Task ReloadEntryCommentsAsync(int playerId) {
        try {
            entryComments[playerId] = await ScoutingClient.GetEntryComments(BoardId, playerId);
        } catch (ApiException) {
            entryComments[playerId] = [];
        }

        await ReloadBoardAsync();
    }

    private async Task AddEntryCommentAsync(ScoutingBoardEntry entry, string body) {
        await ScoutingClient.AddEntryComment(BoardId, entry.PlayerId, new CreateScoutingCommentRequest { Body = body });
        await ReloadEntryCommentsAsync(entry.PlayerId);
    }

    private async Task AddBoardCommentAsync(string body) {
        await ScoutingClient.AddBoardComment(BoardId, new CreateScoutingCommentRequest { Body = body });
        await ReloadBoardCommentsAsync();
    }

    private async Task ReloadBoardCommentsAsync() {
        try {
            boardComments = await ScoutingClient.GetBoardComments(BoardId);
        } catch (ApiException) {
            // Leave the existing list in place on a transient failure.
        }
    }

    private async Task RunAsync(Func<Task> action) {
        busy = true;
        actionError = null;
        try {
            await action();
        } catch (ApiException ex) {
            actionError = DescribeError(ex);
        } catch (HttpRequestException) {
            actionError = "Failed to reach the server. Please try again.";
        } finally {
            busy = false;
        }
    }

    private static string DescribeError(ApiException ex) {
        if (!string.IsNullOrEmpty(ex.Content)) {
            try {
                var problem = JsonSerializer.Deserialize<ProblemPayload>(
                    ex.Content, ShuttleApiClientExtensions.JsonSerializerOptions);
                if (!string.IsNullOrWhiteSpace(problem?.Detail)) {
                    return problem.Detail;
                }
            } catch (JsonException) {
                // Fall through to the generic message.
            }
        }

        return $"The request failed ({(int)ex.StatusCode}). Please try again.";
    }

    private sealed record ProblemPayload {
        public string? Title { get; init; }
        public string? Detail { get; init; }
    }
}
