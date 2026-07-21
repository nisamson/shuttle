using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;
using Refit;
using Shuttle.Api.Client;
using Shuttle.Models.Scouting;

namespace Shuttle.WebClient.Components.Scouting;

/// <summary>
/// Dialog that hosts a single board entry's <see cref="ScoutingCommentThread"/>. It loads and
/// mutates the entry's comments directly through <see cref="IShuttleScoutingClient"/>, so callers
/// only need to reload the board (to refresh comment counts) once the dialog closes.
/// </summary>
public partial class ScoutingEntryCommentsDialog : FluentDialogInstance {
    [Inject] private IShuttleScoutingClient ScoutingClient { get; set; } = null!;

    [Parameter, EditorRequired]
    public required Args Content { get; set; }

    private IReadOnlyList<ScoutingComment> comments = [];
    private bool loading = true;

    protected override void OnInitializeDialog(DialogOptionsHeader header, DialogOptionsFooter footer) {
        header.Title = Content.Title;
        footer.PrimaryAction.Label = "Close";
        footer.SecondaryAction.Visible = false;
    }

    protected override async Task OnInitializedAsync() {
        await ReloadAsync();
        loading = false;
    }

    protected override async Task OnActionClickedAsync(bool primary) {
        await DialogInstance.CloseAsync();
    }

    private async Task ReloadAsync() {
        try {
            comments = await ScoutingClient.GetEntryComments(Content.BoardId, Content.PlayerId);
        } catch (ApiException) {
            comments = [];
        }
    }

    private async Task AddAsync(string body) {
        await ScoutingClient.AddEntryComment(Content.BoardId, Content.PlayerId, new CreateScoutingCommentRequest { Body = body });
        await ReloadAsync();
    }

    /// <summary>Parameters identifying the entry whose comment thread the dialog shows.</summary>
    public sealed record Args {
        public required Guid BoardId { get; init; }
        public required int PlayerId { get; init; }
        public required string Title { get; init; }
        public required bool CanComment { get; init; }
        public required bool CanModerate { get; init; }
        public required Guid? CurrentUserId { get; init; }
    }
}
