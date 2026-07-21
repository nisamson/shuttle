using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;
using Shuttle.Models.Scouting;

namespace Shuttle.WebClient.Components.Scouting;

/// <summary>
/// Dialog for editing a single board entry's scouting status, assignee, and (for active prospects)
/// rank. The dialog closes with a <see cref="Result"/> describing the desired state (or cancels when
/// nothing changed), leaving the caller to apply the changes via the update endpoint.
/// </summary>
public partial class ScoutingEntryEditDialog : FluentDialogInstance {
    /// <summary>The sentinel <see cref="FluentSelect{TOption,TValue}"/> value for "unassigned".</summary>
    private const string UnassignedValue = "";

    [Parameter, EditorRequired]
    public required Args Content { get; set; }

    private int rank;
    private string statusValue = nameof(ScoutingProspectStatus.Pending);
    private string assigneeValue = UnassignedValue;

    private ScoutingProspectStatus SelectedStatus =>
        Enum.TryParse<ScoutingProspectStatus>(statusValue, out var status) ? status : ScoutingProspectStatus.Pending;

    private bool IsRejected => SelectedStatus == ScoutingProspectStatus.Rejected;

    private static readonly IReadOnlyList<ScoutingProspectStatus> Statuses = Enum.GetValues<ScoutingProspectStatus>();

    /// <summary>The assignee dropdown items: "Unassigned" followed by each eligible team member.</summary>
    private IReadOnlyList<AssigneeChoice> AssigneeChoices =>
        [
            new AssigneeChoice { Value = UnassignedValue, Text = "Unassigned" },
            .. Content.EligibleAssignees.Select(a => new AssigneeChoice {
                Value = a.UserId.ToString(),
                Text = a.Username,
            }),
        ];

    protected override void OnInitializeDialog(DialogOptionsHeader header, DialogOptionsFooter footer) {
        header.Title = $"Edit {Content.PlayerName}";
        footer.PrimaryAction.Label = "Save";
        footer.SecondaryAction.Visible = true;
    }

    protected override void OnParametersSet() {
        rank = Content.CurrentRank;
        statusValue = Content.CurrentStatus.ToString();
        assigneeValue = Content.CurrentAssigneeUserId?.ToString() ?? UnassignedValue;
    }

    protected override async Task OnActionClickedAsync(bool primary) {
        var assigneeUserId = Guid.TryParse(assigneeValue, out var parsed) ? parsed : (Guid?)null;
        var result = new Result {
            Status = SelectedStatus,
            AssignedToUserId = assigneeUserId,
            // Rejected prospects are unranked; the server ignores rank in that case.
            Rank = IsRejected ? null : Math.Clamp(rank, 1, Math.Max(Content.MaxRank, 1)),
        };

        // Cancel when the user backs out or leaves every field unchanged, so the caller can no-op.
        if (!primary || !result.DiffersFrom(Content)) {
            await DialogInstance.CancelAsync();
            return;
        }

        await DialogInstance.CloseAsync(result);
    }

    /// <summary>An assignee choice offered by the dialog.</summary>
    public sealed record AssigneeOption {
        public required Guid UserId { get; init; }
        public required string Username { get; init; }
    }

    /// <summary>An item in the assignee dropdown (a member, or the "Unassigned" sentinel).</summary>
    private sealed record AssigneeChoice {
        public required string Value { get; init; }
        public required string Text { get; init; }
    }

    /// <summary>The current state of the entry being edited, used to seed the dialog's fields.</summary>
    public sealed record Args {
        public required string PlayerName { get; init; }
        public required int CurrentRank { get; init; }
        public required int MaxRank { get; init; }
        public required ScoutingProspectStatus CurrentStatus { get; init; }
        public Guid? CurrentAssigneeUserId { get; init; }

        /// <summary>Team members eligible to be assigned (those with edit access).</summary>
        public required IReadOnlyList<AssigneeOption> EligibleAssignees { get; init; }
    }

    /// <summary>The edited values the caller should apply to the entry.</summary>
    public sealed record Result {
        public required ScoutingProspectStatus Status { get; init; }
        public Guid? AssignedToUserId { get; init; }

        /// <summary>The desired active rank, or <c>null</c> when the prospect is rejected (unranked).</summary>
        public int? Rank { get; init; }

        /// <summary>Whether any edited value differs from the entry's original state.</summary>
        public bool DiffersFrom(Args original) =>
            Status != original.CurrentStatus
            || AssignedToUserId != original.CurrentAssigneeUserId
            || (Rank is { } rank && rank != original.CurrentRank);
    }
}
