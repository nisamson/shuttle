using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;
using Shuttle.Models.Scouting;

namespace Shuttle.WebClient.Components.Scouting;

/// <summary>
/// Dialog for applying a scouting status and/or assignee change to several selected board entries at
/// once. Each field independently defaults to "leave unchanged", so the caller can bulk-set only the
/// status, only the assignee, or both. The dialog closes with a <see cref="Result"/> describing which
/// changes to apply (or cancels when nothing was chosen).
/// </summary>
public partial class ScoutingBulkEditDialog : FluentDialogInstance {
    /// <summary>The <see cref="FluentSelect{TOption,TValue}"/> value meaning "leave this field unchanged".</summary>
    private const string LeaveUnchangedValue = "__leave__";

    /// <summary>The assignee <see cref="FluentSelect{TOption,TValue}"/> value meaning "unassign".</summary>
    private const string UnassignedValue = "";

    [Parameter, EditorRequired]
    public required Args Content { get; set; }

    private string statusValue = LeaveUnchangedValue;
    private string assigneeValue = LeaveUnchangedValue;

    private ScoutingProspectStatus? SelectedStatus =>
        Enum.TryParse<ScoutingProspectStatus>(statusValue, out var status) ? status : null;

    private static readonly IReadOnlyList<ScoutingProspectStatus> Statuses = Enum.GetValues<ScoutingProspectStatus>();

    /// <summary>The status dropdown items: "Leave unchanged" followed by each status.</summary>
    private IReadOnlyList<StatusChoice> StatusChoices =>
        [
            new StatusChoice { Value = LeaveUnchangedValue, Text = "Leave unchanged" },
            .. Statuses.Select(s => new StatusChoice { Value = s.ToString(), Text = s.ToString() }),
        ];

    /// <summary>The assignee dropdown items: "Leave unchanged", "Unassigned", then each eligible member.</summary>
    private IReadOnlyList<AssigneeChoice> AssigneeChoices =>
        [
            new AssigneeChoice { Value = LeaveUnchangedValue, Text = "Leave unchanged" },
            new AssigneeChoice { Value = UnassignedValue, Text = "Unassigned" },
            .. Content.EligibleAssignees.Select(a => new AssigneeChoice {
                Value = a.UserId.ToString(),
                Text = a.Username,
            }),
        ];

    protected override void OnInitializeDialog(DialogOptionsHeader header, DialogOptionsFooter footer) {
        header.Title = "Edit selected prospects";
        footer.PrimaryAction.Label = "Apply";
        footer.SecondaryAction.Visible = true;
    }

    protected override async Task OnActionClickedAsync(bool primary) {
        var changeStatus = statusValue != LeaveUnchangedValue;
        var changeAssignee = assigneeValue != LeaveUnchangedValue;

        // Cancel when the user backs out or picked no change, so the caller can no-op.
        if (!primary || (!changeStatus && !changeAssignee)) {
            await DialogInstance.CancelAsync();
            return;
        }

        var result = new Result {
            Status = changeStatus ? SelectedStatus : null,
            ChangeAssignee = changeAssignee,
            AssignedToUserId = changeAssignee && Guid.TryParse(assigneeValue, out var parsed) ? parsed : null,
        };

        await DialogInstance.CloseAsync(result);
    }

    /// <summary>An item in the status dropdown (a status, or the "Leave unchanged" sentinel).</summary>
    private sealed record StatusChoice {
        public required string Value { get; init; }
        public required string Text { get; init; }
    }

    /// <summary>An item in the assignee dropdown (a member, "Unassigned", or "Leave unchanged").</summary>
    private sealed record AssigneeChoice {
        public required string Value { get; init; }
        public required string Text { get; init; }
    }

    /// <summary>The inputs the dialog needs: how many prospects are selected and who can be assigned.</summary>
    public sealed record Args {
        public required int SelectedCount { get; init; }

        /// <summary>Team members eligible to be assigned (those with edit access).</summary>
        public required IReadOnlyList<ScoutingEntryEditDialog.AssigneeOption> EligibleAssignees { get; init; }
    }

    /// <summary>The bulk changes the caller should apply to the selected entries.</summary>
    public sealed record Result {
        /// <summary>The status to apply to all selected prospects, or <c>null</c> to leave statuses unchanged.</summary>
        public ScoutingProspectStatus? Status { get; init; }

        /// <summary>Whether to change the assignee; when <c>false</c> assignees are left unchanged.</summary>
        public bool ChangeAssignee { get; init; }

        /// <summary>The assignee to apply when <see cref="ChangeAssignee"/> is set (<c>null</c> unassigns).</summary>
        public Guid? AssignedToUserId { get; init; }
    }
}
