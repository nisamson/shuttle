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
using Shuttle.WebClient.Components.Scouting;
using Shuttle.WebClient.Models;
using Shuttle.WebClient.Services;

namespace Shuttle.WebClient.Pages.Scouting;

public partial class ScoutingBoard : ComponentBase {
    [Inject] private IShuttleScoutingClient ScoutingClient { get; set; } = null!;
    [Inject] private IShuttlePlayerClient PlayerClient { get; set; } = null!;
    [Inject] private ICurrentUserService CurrentUser { get; set; } = null!;
    [Inject] private IPlayerDirectoryService Directory { get; set; } = null!;
    [Inject] private IDialogService DialogService { get; set; } = null!;

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
    private string? actionMessage;

    private IReadOnlyList<ScoutingComment> boardComments = [];
    private readonly Dictionary<int, PlayerCard> playerCards = new();
    private IReadOnlyList<ScoutingEntryEditDialog.AssigneeOption> eligibleAssignees = [];

    // The grid's rows: entries joined with their resolved player cards, rebuilt on every reload.
    private List<BoardRow> rows = [];
    private IEnumerable<BoardRow> selectedRows = [];
    private IEnumerable<BoardRow> selectedRejectedRows = [];
    private string nameFilter = string.Empty;
    private readonly HashSet<PlayerPosition> selectedPositions = [];

    private PlayerSuggestion? pendingPlayer;

    private bool CanEditBoards => isAdmin || myRole >= ScoutingTeamRole.Editor;
    private bool CanComment => isAdmin || myRole >= ScoutingTeamRole.Editor;
    private bool CanModerate => isAdmin || myRole == ScoutingTeamRole.Owner;

    private int SelectedCount => selectedRows.Count();
    private int SelectedRejectedCount => selectedRejectedRows.Count();

    // Only active (non-rejected) prospects participate in the ranked list.
    private int ActiveCount => rows.Count(r => r.Status != ScoutingProspectStatus.Rejected);

    private bool HasRejected => rows.Any(r => r.Status == ScoutingProspectStatus.Rejected);

    private IQueryable<BoardRow> FilteredRows {
        get {
            var query = rows.Where(r => r.Status != ScoutingProspectStatus.Rejected).AsQueryable();
            if (!string.IsNullOrWhiteSpace(nameFilter)) {
                var text = nameFilter.Trim();
                query = query.Where(r => r.Name.Contains(text, StringComparison.OrdinalIgnoreCase));
            }

            if (selectedPositions.Count > 0) {
                query = query.Where(r => r.Position != null && selectedPositions.Contains(r.Position.Value));
            }

            return query;
        }
    }

    // Rejected prospects, shown in their own list below the active board, in rejection order.
    private IQueryable<BoardRow> RejectedRows =>
        rows.Where(r => r.Status == ScoutingProspectStatus.Rejected)
            .OrderBy(r => r.Name)
            .AsQueryable();

    // The distinct positions present among the active prospects, in a stable canonical order, for the column filter.
    private IEnumerable<PlayerPosition> AvailablePositions =>
        rows.Where(r => r.Status != ScoutingProspectStatus.Rejected && r.Position is not null)
            .Select(r => r.Position!.Value)
            .Distinct()
            .OrderBy(p => p);

    private void TogglePosition(PlayerPosition position, bool selected) {
        if (selected) {
            selectedPositions.Add(position);
        } else {
            selectedPositions.Remove(position);
        }

        StateHasChanged();
    }

    private static readonly GridSort<BoardRow> RankSort = GridSort<BoardRow>.ByAscending(r => r.Rank);
    private static readonly GridSort<BoardRow> NameSort = GridSort<BoardRow>.ByAscending(r => r.Name);
    private static readonly GridSort<BoardRow> PositionSort = GridSort<BoardRow>.ByAscending(r => r.Position);
    private static readonly GridSort<BoardRow> TpeSort = GridSort<BoardRow>.ByAscending(r => r.Tpe);
    private static readonly GridSort<BoardRow> BankSort = GridSort<BoardRow>.ByAscending(r => r.Bank);

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
            BuildRows();
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
            eligibleAssignees = team.Members
                .Where(m => m.Role >= ScoutingTeamRole.Editor)
                .OrderBy(m => m.Username, StringComparer.OrdinalIgnoreCase)
                .Select(m => new ScoutingEntryEditDialog.AssigneeOption {
                    UserId = m.UserId,
                    Username = m.Username,
                })
                .ToList();
        } catch (ApiException) {
            myRole = null;
            eligibleAssignees = [];
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

    // Joins each ranked entry with its resolved player card into a flat grid row and clears the
    // selection, since the previous row instances no longer correspond to the reloaded board.
    private void BuildRows() {
        rows = board is null
            ? []
            : board.Entries
                .Select(entry => {
                    var card = playerCards.GetValueOrDefault(entry.PlayerId);
                    return new BoardRow {
                        Id = entry.Id,
                        PlayerId = entry.PlayerId,
                        Rank = entry.Rank,
                        Status = entry.Status,
                        AssignedToUserId = entry.AssignedToUserId,
                        AssignedToUsername = entry.AssignedToUsername,
                        CommentCount = entry.CommentCount,
                        Name = card?.Name ?? $"Player #{entry.PlayerId}",
                        Position = card?.Position,
                        Tpe = card?.TotalTpe,
                        Bank = card?.BankBalance,
                    };
                })
                .ToList();
        selectedRows = [];
        selectedRejectedRows = [];
    }

    private async Task ReloadBoardAsync() {
        try {
            board = await ScoutingClient.GetBoard(BoardId);
            await ResolvePlayerCardsAsync();
            BuildRows();
        } catch (ApiException ex) {
            actionError = DescribeError(ex);
        } catch (HttpRequestException) {
            actionError = "Failed to reach the server. Please try again.";
        }
    }

    private string PageTitleText =>
        board is null
            ? "Scouting board"
            : board.DraftSeason is { } season
                ? $"{board.Name} - S{season}"
                : board.Name;

    private static string PositionText(BoardRow row) =>
        row.Position is { } position ? position.ToShortString() : "—";

    private static string TpeText(BoardRow row) =>
        row.Tpe is { } tpe ? tpe.ToString("N0", CultureInfo.InvariantCulture) : "—";

    private static string BankText(BoardRow row) =>
        row.Bank is { } bank ? bank.ToString("C0", UsdCulture) : "—";

    // Renders bank balances as USD ($12,500) regardless of the browser's locale, matching PlayerProfile.
    private static readonly CultureInfo UsdCulture = CreateUsdCulture();

    private static CultureInfo CreateUsdCulture() {
        var culture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
        culture.NumberFormat.CurrencySymbol = "$";
        culture.NumberFormat.CurrencyPositivePattern = 0;
        culture.NumberFormat.CurrencyNegativePattern = 1;
        return culture;
    }

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

    private async Task MoveAsync(int playerId, int fromRank, int toRank) {
        if (toRank == fromRank || toRank < 1 || toRank > ActiveCount) {
            return;
        }

        await RunAsync(async () => {
            await ScoutingClient.MoveEntry(BoardId, new MoveScoutingBoardEntryRequest {
                PlayerId = playerId,
                FromRank = fromRank,
                ToRank = toRank,
            });
            await ReloadBoardAsync();
        });
    }

    private async Task EditEntryAsync(BoardRow row) {
        var result = await DialogService.ShowDialogAsync<ScoutingEntryEditDialog>(options => {
            options.Modal = true;
            options.Width = "360px";
            options.Parameters.Add(nameof(ScoutingEntryEditDialog.Content), new ScoutingEntryEditDialog.Args {
                PlayerName = row.Name,
                CurrentRank = row.Rank,
                MaxRank = Math.Max(ActiveCount, 1),
                CurrentStatus = row.Status,
                CurrentAssigneeUserId = row.AssignedToUserId,
                EligibleAssignees = eligibleAssignees,
            });
        });

        if (result.Cancelled || result.Value is not ScoutingEntryEditDialog.Result edited) {
            return;
        }

        await RunAsync(async () => {
            await ScoutingClient.UpdateEntry(BoardId, row.PlayerId, new UpdateScoutingBoardEntryRequest {
                Status = edited.Status,
                AssignedToUserId = edited.AssignedToUserId,
                Rank = edited.Rank,
            });
            await ReloadBoardAsync();
        });
    }

    private static string StatusText(ScoutingProspectStatus status) => status switch {
        ScoutingProspectStatus.Pending => "Pending",
        ScoutingProspectStatus.Scouted => "Scouted",
        ScoutingProspectStatus.Approved => "Approved",
        ScoutingProspectStatus.Rejected => "Rejected",
        _ => status.ToString(),
    };

    private static BadgeAppearance StatusAppearance(ScoutingProspectStatus status) => status switch {
        ScoutingProspectStatus.Pending => BadgeAppearance.Ghost,
        ScoutingProspectStatus.Scouted => BadgeAppearance.Outline,
        ScoutingProspectStatus.Approved => BadgeAppearance.Filled,
        ScoutingProspectStatus.Rejected => BadgeAppearance.Tint,
        _ => BadgeAppearance.Outline,
    };

    private async Task RemoveAsync(BoardRow row) {
        var confirm = await DialogService.ShowConfirmationAsync(
            message: $"Remove {row.Name} from this board? Their notes will also be deleted. This cannot be undone.",
            title: "Remove player?",
            primaryButton: "Remove",
            secondaryButton: "Cancel");
        if (confirm.Cancelled) {
            return;
        }

        await RunAsync(async () => {
            await ScoutingClient.RemoveEntry(BoardId, row.PlayerId);
            await ReloadBoardAsync();
        });
    }

    private Task BulkDeleteAsync() =>
        BulkDeleteCoreAsync(selectedRows.Select(r => r.PlayerId).Distinct().ToList());

    private Task BulkDeleteRejectedAsync() =>
        BulkDeleteCoreAsync(selectedRejectedRows.Select(r => r.PlayerId).Distinct().ToList());

    private async Task BulkDeleteCoreAsync(List<int> ids) {
        if (ids.Count == 0) {
            return;
        }

        var confirm = await DialogService.ShowConfirmationAsync(
            message: $"Remove {ids.Count} player{(ids.Count == 1 ? string.Empty : "s")} from this board? " +
                     "Their notes will also be deleted. This cannot be undone.",
            title: "Remove selected players?",
            primaryButton: "Remove",
            secondaryButton: "Cancel");
        if (confirm.Cancelled) {
            return;
        }

        await RunAsync(async () => {
            await ScoutingClient.RemoveEntries(BoardId, new RemoveScoutingBoardEntriesRequest { PlayerIds = ids });
            await ReloadBoardAsync();
        });
    }

    private Task BulkEditAsync() =>
        BulkEditCoreAsync(selectedRows.Select(r => r.PlayerId).Distinct().ToList());

    private Task BulkEditRejectedAsync() =>
        BulkEditCoreAsync(selectedRejectedRows.Select(r => r.PlayerId).Distinct().ToList());

    private async Task BulkEditCoreAsync(List<int> ids) {
        if (ids.Count == 0) {
            return;
        }

        var dialogResult = await DialogService.ShowDialogAsync<ScoutingBulkEditDialog>(options => {
            options.Modal = true;
            options.Width = "420px";
            options.Parameters.Add(nameof(ScoutingBulkEditDialog.Content), new ScoutingBulkEditDialog.Args {
                SelectedCount = ids.Count,
                EligibleAssignees = eligibleAssignees,
            });
        });

        if (dialogResult.Cancelled || dialogResult.Value is not ScoutingBulkEditDialog.Result edited) {
            return;
        }

        await RunAsync(async () => {
            await ScoutingClient.UpdateEntries(BoardId, new BulkUpdateScoutingBoardEntriesRequest {
                PlayerIds = ids,
                Status = edited.Status,
                ChangeAssignee = edited.ChangeAssignee,
                AssignedToUserId = edited.AssignedToUserId,
            });
            await ReloadBoardAsync();
            actionMessage = $"Updated {ids.Count} prospect{(ids.Count == 1 ? string.Empty : "s")}.";
        });
    }

    private async Task BulkAddAsync() {
        var existing = board?.Entries.Select(e => e.PlayerId).ToHashSet() ?? [];
        var dialogResult = await DialogService.ShowDialogAsync<ScoutingBulkAddDialog>(options => {
            options.Modal = true;
            options.Width = "640px";
            options.Parameters.Add(nameof(ScoutingBulkAddDialog.Content), new ScoutingBulkAddDialog.Args {
                ExistingPlayerIds = existing,
            });
        });

        if (dialogResult.Cancelled
            || dialogResult.Value is not ScoutingBulkAddDialog.Result added
            || added.PlayerIds.Count == 0) {
            return;
        }

        await RunAsync(async () => {
            var response = await ScoutingClient.AddEntries(BoardId, new AddScoutingBoardEntriesRequest {
                PlayerIds = added.PlayerIds,
            });
            await ReloadBoardAsync();
            actionMessage = SummarizeBulkAdd(response);
        });
    }

    private static string SummarizeBulkAdd(AddScoutingBoardEntriesResult result) {
        var parts = new List<string> {
            $"Added {result.Added.Count} player{(result.Added.Count == 1 ? string.Empty : "s")}",
        };
        if (result.AlreadyOnBoard.Count > 0) {
            parts.Add($"{result.AlreadyOnBoard.Count} already on board");
        }

        if (result.NotFound.Count > 0) {
            parts.Add($"{result.NotFound.Count} not found");
        }

        return string.Join(", ", parts) + ".";
    }

    private void ClearSelection() {
        selectedRows = [];
    }

    private void ClearRejectedSelection() {
        selectedRejectedRows = [];
    }

    // Comparison is read-only, so it's available regardless of edit rights. It's rendered as an
    // anchor with target="_blank" (rather than a JS window.open) so the browser opens it in a new tab
    // natively — no interop, and no popup-blocker issues — leaving the board and its selection intact.
    private string CompareSelectedUrl =>
        Routes.Players.CompareWith(selectedRows.Select(r => r.PlayerId).Distinct());

    private string CompareSelectedRejectedUrl =>
        Routes.Players.CompareWith(selectedRejectedRows.Select(r => r.PlayerId).Distinct());

    private async Task OpenEntryCommentsAsync(BoardRow row) {
        await DialogService.ShowDialogAsync<ScoutingEntryCommentsDialog>(options => {
            options.Modal = true;
            options.Width = "640px";
            options.Parameters.Add(nameof(ScoutingEntryCommentsDialog.Content), new ScoutingEntryCommentsDialog.Args {
                BoardId = BoardId,
                PlayerId = row.PlayerId,
                Title = $"Notes — {row.Name}",
                CanComment = CanComment,
                CanModerate = CanModerate,
                CurrentUserId = currentUserId,
            });
        });

        // The dialog mutates comments directly; reload so the entry's comment count stays current.
        await ReloadBoardAsync();
        StateHasChanged();
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
        actionMessage = null;
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

    /// <summary>A flattened board entry (entry data joined with its resolved player card) for the grid.</summary>
    private sealed record BoardRow {
        public required Guid Id { get; init; }
        public required int PlayerId { get; init; }
        public required int Rank { get; init; }
        public required ScoutingProspectStatus Status { get; init; }
        public Guid? AssignedToUserId { get; init; }
        public string? AssignedToUsername { get; init; }
        public required int CommentCount { get; init; }
        public required string Name { get; init; }
        public PlayerPosition? Position { get; init; }
        public int? Tpe { get; init; }
        public int? Bank { get; init; }
    }
}
