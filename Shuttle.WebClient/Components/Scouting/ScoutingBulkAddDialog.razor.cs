using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;
using Refit;
using Shuttle.Api.Client;
using Shuttle.Models.Players;
using Shuttle.WebClient.Services;

namespace Shuttle.WebClient.Components.Scouting;

/// <summary>
/// Dialog for bulk-adding players to a scouting board. The user pastes player names and/or ids (one
/// per line); the dialog resolves them via <c>QUERY /players/resolve</c> and shows a preview of the
/// matched players (plus any not-found or ambiguous inputs) before the caller commits the add. It
/// closes with the resolved player ids, leaving the caller to perform the bulk add.
/// </summary>
public partial class ScoutingBulkAddDialog : FluentDialogInstance {
    [Inject] private IShuttlePlayerClient PlayerClient { get; set; } = null!;
    [Inject] private IPlayerDirectoryService Directory { get; set; } = null!;

    [Parameter, EditorRequired]
    public required Args Content { get; set; }

    private string rawText = string.Empty;
    private bool resolving;
    private string? resolveError;
    private ResolvePlayersResult? result;
    private PlayerSuggestion? picked;

    private IQueryable<ResolvedPlayer> ResolvedRows => (result?.Resolved ?? []).AsQueryable();

    // Enabled only once resolution succeeded with at least one player and no ambiguous names, so the
    // user is nudged to disambiguate (by id) rather than silently dropping an ambiguous name.
    private bool CanAdd => result is not null && result.Ambiguous.Count == 0 && result.Resolved.Count > 0;

    private string AddButtonLabel {
        get {
            var count = result?.Resolved.Count ?? 0;
            return count > 0 ? $"Add {count} player{(count == 1 ? string.Empty : "s")}" : "Add players";
        }
    }

    protected override void OnInitializeDialog(DialogOptionsHeader header, DialogOptionsFooter footer) {
        header.Title = "Bulk add players";
        // The add flow closes via the in-body "Add" button; the footer only offers a way to back out.
        footer.PrimaryAction.Visible = false;
        footer.SecondaryAction.Visible = true;
        footer.SecondaryAction.Label = "Close";
    }

    protected override async Task OnActionClickedAsync(bool primary) {
        await DialogInstance.CancelAsync();
    }

    private async Task ResolveAsync() {
        resolving = true;
        resolveError = null;
        result = null;
        try {
            result = await PlayerClient.ResolvePlayers(ParseInput(rawText));
        } catch (ApiException ex) {
            resolveError = DescribeError(ex);
        } catch (HttpRequestException) {
            resolveError = "Failed to reach the server. Please try again.";
        } finally {
            resolving = false;
        }
    }

    private async Task OnPlayerSearch(OptionsSearchEventArgs<PlayerSuggestion> e) {
        e.Items = await Directory.Search(e.Text);
    }

    // Appends the picked player's id (unambiguous) to the paste box as a new line, then clears the
    // picker so the field is ready for the next search.
    private void OnPlayerPicked(PlayerSuggestion? player) {
        picked = null;
        if (player is not null) {
            rawText = AppendPlayerId(rawText, player.PlayerId);
        }
    }

    // Appends a player id on its own line, skipping ids already present so repeated picks don't
    // duplicate a line. Kept static/internal so the append+dedup behaviour is unit-testable without
    // rendering the dialog (which requires a live FluentUI dialog host).
    internal static string AppendPlayerId(string rawText, int playerId) {
        var existing = ParseInput(rawText).PlayerIds;
        if (existing is not null && existing.Contains(playerId)) {
            return rawText;
        }

        var id = playerId.ToString(CultureInfo.InvariantCulture);
        var trimmed = rawText.TrimEnd('\r', '\n');
        return trimmed.Length == 0 ? id : $"{trimmed}\n{id}";
    }

    private async Task AddAsync() {
        if (!CanAdd || result is null) {
            return;
        }

        var ids = result.Resolved.Select(r => r.PlayerId).ToList();
        await DialogInstance.CloseAsync(new Result { PlayerIds = ids });
    }

    // Splits the textarea into ids and names: a line that parses as a positive integer is treated as a
    // player id, everything else as a name. Blank lines are ignored.
    private static ResolvePlayersRequest ParseInput(string raw) {
        var ids = new List<int>();
        var names = new List<string>();
        foreach (var line in raw.Split('\n')) {
            var token = line.Trim();
            if (token.Length == 0) {
                continue;
            }

            if (int.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id) && id > 0) {
                ids.Add(id);
            } else {
                names.Add(token);
            }
        }

        return new ResolvePlayersRequest { PlayerIds = ids, Names = names };
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

    /// <summary>Inputs the caller seeds the dialog with.</summary>
    public sealed record Args {
        /// <summary>Ids already on the board, so resolved players can be tagged as such in the preview.</summary>
        public required IReadOnlySet<int> ExistingPlayerIds { get; init; }
    }

    /// <summary>The resolved player ids the caller should bulk-add to the board.</summary>
    public sealed record Result {
        public required IReadOnlyList<int> PlayerIds { get; init; }
    }
}
