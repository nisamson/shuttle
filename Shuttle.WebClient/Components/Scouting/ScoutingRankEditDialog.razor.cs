using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Shuttle.WebClient.Components.Scouting;

/// <summary>
/// Dialog for editing a single board entry's rank. It closes with the chosen rank (an
/// <see cref="int"/>) when the user saves a changed value, or cancels otherwise, leaving the caller
/// to perform the actual move.
/// </summary>
public partial class ScoutingRankEditDialog : FluentDialogInstance {
    [Parameter, EditorRequired]
    public required Args Content { get; set; }

    private int newRank;

    protected override void OnInitializeDialog(DialogOptionsHeader header, DialogOptionsFooter footer) {
        header.Title = $"Move {Content.PlayerName}";
        footer.PrimaryAction.Label = "Save";
        footer.SecondaryAction.Visible = true;
    }

    protected override void OnParametersSet() {
        newRank = Content.CurrentRank;
    }

    protected override async Task OnActionClickedAsync(bool primary) {
        var clamped = Math.Clamp(newRank, 1, Content.MaxRank);
        if (!primary || clamped == Content.CurrentRank) {
            await DialogInstance.CancelAsync();
            return;
        }

        await DialogInstance.CloseAsync(clamped);
    }

    /// <summary>Parameters describing the entry being re-ranked.</summary>
    public sealed record Args {
        public required string PlayerName { get; init; }
        public required int CurrentRank { get; init; }
        public required int MaxRank { get; init; }
    }
}
