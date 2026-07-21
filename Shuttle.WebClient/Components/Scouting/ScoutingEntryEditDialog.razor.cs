using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Shuttle.WebClient.Components.Scouting;

/// <summary>
/// Dialog for editing a single board entry. It currently exposes the entry's rank, but is structured
/// so that additional editable fields can be added over time: the dialog closes with a
/// <see cref="Result"/> describing the desired state (or cancels when nothing changed), leaving the
/// caller to apply the changes.
/// </summary>
public partial class ScoutingEntryEditDialog : FluentDialogInstance {
    [Parameter, EditorRequired]
    public required Args Content { get; set; }

    private int rank;

    protected override void OnInitializeDialog(DialogOptionsHeader header, DialogOptionsFooter footer) {
        header.Title = $"Edit {Content.PlayerName}";
        footer.PrimaryAction.Label = "Save";
        footer.SecondaryAction.Visible = true;
    }

    protected override void OnParametersSet() {
        rank = Content.CurrentRank;
    }

    protected override async Task OnActionClickedAsync(bool primary) {
        var clampedRank = Math.Clamp(rank, 1, Content.MaxRank);
        var result = new Result { Rank = clampedRank };

        // Cancel when the user backs out or leaves every field unchanged, so the caller can no-op.
        if (!primary || !result.DiffersFrom(Content)) {
            await DialogInstance.CancelAsync();
            return;
        }

        await DialogInstance.CloseAsync(result);
    }

    /// <summary>The current state of the entry being edited, used to seed the dialog's fields.</summary>
    public sealed record Args {
        public required string PlayerName { get; init; }
        public required int CurrentRank { get; init; }
        public required int MaxRank { get; init; }
    }

    /// <summary>The edited values the caller should apply to the entry.</summary>
    public sealed record Result {
        public required int Rank { get; init; }

        /// <summary>Whether any edited value differs from the entry's original state.</summary>
        public bool DiffersFrom(Args original) => Rank != original.CurrentRank;
    }
}
