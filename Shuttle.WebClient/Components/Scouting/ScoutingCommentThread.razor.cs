using Microsoft.AspNetCore.Components;
using Refit;
using Shuttle.Api.Client;
using Shuttle.Models.Scouting;

namespace Shuttle.WebClient.Components.Scouting;

public partial class ScoutingCommentThread : ComponentBase {
    [Inject] private IShuttleScoutingClient ScoutingClient { get; set; } = null!;

    /// <summary>Heading shown above the thread.</summary>
    [Parameter] public string Title { get; set; } = "Comments";

    /// <summary>The comments to display, in chronological order.</summary>
    [Parameter] public IReadOnlyList<ScoutingComment> Comments { get; set; } = [];

    /// <summary>Whether the caller may post and edit their own comments.</summary>
    [Parameter] public bool CanComment { get; set; }

    /// <summary>Whether the caller may delete other members' comments (owner/admin).</summary>
    [Parameter] public bool CanModerate { get; set; }

    /// <summary>The caller's own <c>ShuttleUser</c> id, used to detect their own comments.</summary>
    [Parameter] public Guid? CurrentUserId { get; set; }

    /// <summary>Invoked with the new comment body when the caller posts. The parent owns the endpoint.</summary>
    [Parameter] public EventCallback<string> OnAdd { get; set; }

    /// <summary>Invoked after an edit or delete so the parent can reload the thread.</summary>
    [Parameter] public EventCallback OnChanged { get; set; }

    private string newBody = string.Empty;
    private string editBody = string.Empty;
    private Guid? editingId;
    private bool busy;
    private string? errorMessage;

    private bool CanEdit(ScoutingComment comment) =>
        CanComment && CurrentUserId is { } id && comment.AuthorUserId == id;

    private bool CanDelete(ScoutingComment comment) =>
        CanModerate || (CurrentUserId is { } id && comment.AuthorUserId == id);

    private void BeginEdit(ScoutingComment comment) {
        editingId = comment.Id;
        editBody = comment.Body;
        errorMessage = null;
    }

    private void CancelEdit() {
        editingId = null;
        editBody = string.Empty;
    }

    private async Task AddAsync() {
        if (string.IsNullOrWhiteSpace(newBody)) {
            return;
        }

        var body = newBody;
        busy = true;
        errorMessage = null;
        try {
            await OnAdd.InvokeAsync(body);
            newBody = string.Empty;
        } catch (ApiException ex) {
            errorMessage = Describe(ex);
        } catch (HttpRequestException) {
            errorMessage = "Failed to reach the server. Please try again.";
        } finally {
            busy = false;
        }
    }

    private async Task SaveEditAsync(ScoutingComment comment) {
        if (string.IsNullOrWhiteSpace(editBody)) {
            return;
        }

        busy = true;
        errorMessage = null;
        try {
            await ScoutingClient.EditComment(comment.Id, new UpdateScoutingCommentRequest { Body = editBody });
            editingId = null;
            editBody = string.Empty;
            await OnChanged.InvokeAsync();
        } catch (ApiException ex) {
            errorMessage = Describe(ex);
        } catch (HttpRequestException) {
            errorMessage = "Failed to reach the server. Please try again.";
        } finally {
            busy = false;
        }
    }

    private async Task DeleteAsync(ScoutingComment comment) {
        busy = true;
        errorMessage = null;
        try {
            await ScoutingClient.DeleteComment(comment.Id);
            await OnChanged.InvokeAsync();
        } catch (ApiException ex) {
            errorMessage = Describe(ex);
        } catch (HttpRequestException) {
            errorMessage = "Failed to reach the server. Please try again.";
        } finally {
            busy = false;
        }
    }

    private static string Describe(ApiException ex) =>
        $"The request failed ({(int)ex.StatusCode}). Please try again.";
}
