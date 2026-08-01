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
/// per line); the dialog resolves them via <c>QUERY /players/lookup</c> and shows a preview of the
/// matched players (plus any not-found or ambiguous inputs) before the caller commits the add. The
/// footer's "Add players" action resolves the input if needed and then closes with the resolved
/// player ids, leaving the caller to perform the bulk add.
/// </summary>
public partial class ScoutingBulkAddDialog : FluentDialogInstance {
    [Inject] private IShuttlePlayerClient PlayerClient { get; set; } = null!;
    [Inject] private IPlayerDirectoryService Directory { get; set; } = null!;

    [Parameter, EditorRequired]
    public required Args Content { get; set; }

    private string rawText = string.Empty;
    private bool resolving;
    private string? resolveError;
    private PlayerLookupResult? result;
    private PlayerSuggestion? picked;

    private IQueryable<PlayerLookupMatch> ResolvedRows => (result?.Resolved ?? []).AsQueryable();

    // Built-in FluentDataGrid client-side sorts over the resolved preview rows. Position sorts by the
    // raw PlayerPosition enum value to match the server's ordering (see PlayerController), and every
    // sort adds a PlayerId tiebreak so ties keep a stable order. Kept internal so the ordering is
    // unit-testable via GridSort.Apply without rendering the FluentUI dialog host.
    internal static readonly GridSort<PlayerLookupMatch> NameSort =
        GridSort<PlayerLookupMatch>.ByAscending(p => p.Name).ThenAscending(p => p.PlayerId);

    internal static readonly GridSort<PlayerLookupMatch> PositionSort =
        GridSort<PlayerLookupMatch>.ByAscending(p => p.Position).ThenAscending(p => p.PlayerId);

    internal static readonly GridSort<PlayerLookupMatch> DraftSeasonSort =
        GridSort<PlayerLookupMatch>.ByAscending(p => p.DraftSeason).ThenAscending(p => p.PlayerId);

    internal static readonly GridSort<PlayerLookupMatch> TotalTpeSort =
        GridSort<PlayerLookupMatch>.ByAscending(p => p.TotalTpe).ThenAscending(p => p.PlayerId);

    // Enabled only once resolution succeeded with at least one player and no ambiguous names, so the
    // user is nudged to disambiguate (by id) rather than silently dropping an ambiguous name.
    private bool CanAdd => result is not null && result.Ambiguous.Count == 0 && result.Resolved.Count > 0;

    protected override void OnInitializeDialog(DialogOptionsHeader header, DialogOptionsFooter footer) {
        header.Title = "Bulk add players";
        // "Add players" (primary) sits next to "Close" (secondary) in the footer; it resolves the
        // pasted input if the user hasn't previewed yet, then commits the add when everything resolves.
        footer.PrimaryAction.Visible = true;
        footer.PrimaryAction.Label = "Add players";
        // The dialog is dominated by a multi-line paste box, so Enter must insert a newline rather
        // than submit; clearing the default "Enter" shortcut stops it triggering the add.
        footer.PrimaryAction.ShortCut = string.Empty;
        footer.SecondaryAction.Visible = true;
        footer.SecondaryAction.Label = "Close";
    }

    protected override async Task OnActionClickedAsync(bool primary) {
        if (!primary) {
            await DialogInstance.CancelAsync();
            return;
        }

        // Resolve on demand so the user can add straight from the paste box without first clicking
        // "Resolve preview". If anything is ambiguous (or nothing resolved), stay open so the body can
        // surface the ambiguity/warnings instead of silently dropping input.
        if (result is null && !string.IsNullOrWhiteSpace(rawText)) {
            await ResolveAsync();
        }

        if (CanAdd && result is not null) {
            var ids = result.Resolved.Select(r => r.PlayerId).ToList();
            await DialogInstance.CloseAsync(new Result { PlayerIds = ids });
        }
    }

    private async Task ResolveAsync() {
        resolving = true;
        resolveError = null;
        result = null;
        try {
            result = await PlayerClient.LookupPlayers(ParseInput(rawText));
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

    // Clears a stale preview when the paste box changes, so a subsequent "Add players" re-resolves the
    // edited input rather than committing the previous resolution.
    private void InvalidatePreview() {
        result = null;
        resolveError = null;
    }

    // Splits the textarea into ids and names: a line that parses as a positive integer is treated as a
    // player id, everything else as a name. Blank lines are ignored.
    private static PlayerLookupRequest ParseInput(string raw) {
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

        return new PlayerLookupRequest { PlayerIds = ids, Names = names };
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
