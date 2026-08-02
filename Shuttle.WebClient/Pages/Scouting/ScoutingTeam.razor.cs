using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.FluentUI.AspNetCore.Components;
using Refit;
using Shuttle.Api.Client;
using Shuttle.Models.Scouting;
using Shuttle.WebClient.Models;
using Shuttle.WebClient.Services;

namespace Shuttle.WebClient.Pages.Scouting;

public partial class ScoutingTeam : ComponentBase {
    [Inject] private IShuttleScoutingClient ScoutingClient { get; set; } = null!;
    [Inject] private ICurrentUserService CurrentUser { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;
    [Inject] private IDialogService DialogService { get; set; } = null!;

    [CascadingParameter] private Task<AuthenticationState>? AuthState { get; set; }

    [Parameter] public Guid TeamId { get; set; }

    private static readonly IReadOnlyList<ScoutingTeamRole> RoleOptions =
        [ScoutingTeamRole.Viewer, ScoutingTeamRole.Editor, ScoutingTeamRole.Owner];

    private ScoutingTeamDetail? team;
    private Guid? currentUserId;
    private bool isAdmin;
    private bool loading = true;
    private bool busy;
    private string? loadError;
    private string? actionError;

    private bool renaming;
    private string renameValue = string.Empty;

    private string addUsername = string.Empty;
    private ScoutingTeamRole addRole = ScoutingTeamRole.Viewer;

    private bool showCreateBoard;
    private string newBoardName = string.Empty;
    private int? newBoardSeason;

    private bool IsMember => team?.MyRole is not null;
    private bool CanManage => isAdmin || team?.MyRole == ScoutingTeamRole.Owner;
    private bool CanEditBoards => isAdmin || team?.MyRole >= ScoutingTeamRole.Editor;
    private int OwnerCount => team?.Members.Count(m => m.Role == ScoutingTeamRole.Owner) ?? 0;
    private bool CanDelete => isAdmin || (team?.MyRole == ScoutingTeamRole.Owner && OwnerCount == 1);
    private bool CanLeave => IsMember && !(team?.MyRole == ScoutingTeamRole.Owner && OwnerCount == 1);

    private bool IsSoleOwner(ScoutingMember member) =>
        member.Role == ScoutingTeamRole.Owner && OwnerCount == 1;

    protected override async Task OnParametersSetAsync() {
        isAdmin = AuthState is not null && (await AuthState).User.IsInRole(RoleNames.Admin);
        currentUserId = (await CurrentUser.GetAsync())?.Id;
        await ReloadAsync();
    }

    private async Task ReloadAsync() {
        loading = true;
        loadError = null;
        try {
            team = await ScoutingClient.GetTeam(TeamId);
        } catch (ApiException ex) {
            team = null;
            loadError = ex.StatusCode == System.Net.HttpStatusCode.NotFound
                ? "That team could not be found."
                : ex.StatusCode == System.Net.HttpStatusCode.Forbidden
                    ? "You do not have access to this team."
                    : $"Failed to load the team ({(int)ex.StatusCode}).";
        } catch (HttpRequestException) {
            team = null;
            loadError = "Failed to reach the server. Please try again.";
        } finally {
            loading = false;
        }
    }

    private void BeginRename() {
        renameValue = team?.Name ?? string.Empty;
        renaming = true;
        actionError = null;
    }

    private async Task SaveRenameAsync() {
        var name = renameValue.Trim();
        if (name.Length == 0) {
            return;
        }

        await RunAsync(async () => {
            await ScoutingClient.RenameTeam(TeamId, new UpdateScoutingTeamRequest { Name = name });
            renaming = false;
            await ReloadAsync();
        });
    }

    private async Task LeaveAsync() {
        await RunAsync(async () => {
            await ScoutingClient.LeaveTeam(TeamId);
            Navigation.NavigateTo(Routes.Scouting.Root);
        });
    }

    private async Task DeleteTeamAsync() {
        await RunAsync(async () => {
            await ScoutingClient.DeleteTeam(TeamId);
            Navigation.NavigateTo(Routes.Scouting.Root);
        });
    }

    private async Task ChangeRoleAsync(ScoutingMember member, ScoutingTeamRole role) {
        if (member.Role == role) {
            return;
        }

        if (role == ScoutingTeamRole.Owner && !await ConfirmOwnerElevationAsync()) {
            // Revert the select back to the member's current role.
            StateHasChanged();
            return;
        }

        await RunAsync(async () => {
            await ScoutingClient.UpdateMemberRole(TeamId, member.UserId, new UpdateScoutingMemberRoleRequest { Role = role });
            await ReloadAsync();
        });
    }

    private async Task RemoveMemberAsync(ScoutingMember member) {
        await RunAsync(async () => {
            await ScoutingClient.RemoveMember(TeamId, member.UserId);
            await ReloadAsync();
        });
    }

    private async Task AddMemberAsync() {
        var username = addUsername.Trim();
        if (username.Length == 0) {
            return;
        }

        if (addRole == ScoutingTeamRole.Owner && !await ConfirmOwnerElevationAsync()) {
            return;
        }

        await RunAsync(async () => {
            await ScoutingClient.AddMember(TeamId, new AddScoutingMemberRequest { Username = username, Role = addRole });
            addUsername = string.Empty;
            addRole = ScoutingTeamRole.Viewer;
            await ReloadAsync();
        });
    }

    // Warns that Owner promotion is irreversible from the manager's side: only the member can leave.
    private async Task<bool> ConfirmOwnerElevationAsync() {
        var result = await DialogService.ShowConfirmationAsync(
            message: "Making this member an Owner cannot be undone by you: you will not be able to demote or remove them. " +
                     "Only they can step down or leave the team.",
            title: "Promote to Owner?",
            primaryButton: "Make Owner",
            secondaryButton: "Cancel");
        return !result.Cancelled;
    }

    private async Task CreateBoardAsync() {
        var name = newBoardName.Trim();
        if (name.Length == 0) {
            return;
        }

        await RunAsync(async () => {
            await ScoutingClient.CreateBoard(TeamId, new CreateScoutingBoardRequest {
                Name = name,
                DraftSeason = newBoardSeason,
            });
            newBoardName = string.Empty;
            newBoardSeason = null;
            showCreateBoard = false;
            await ReloadAsync();
        });
    }

    private async Task DeleteBoardAsync(ScoutingBoardSummary board) {
        await RunAsync(async () => {
            await ScoutingClient.DeleteBoard(board.Id);
            await ReloadAsync();
        });
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

    // Surfaces the server's ProblemDetails message (e.g. the sole-owner guard) when present.
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

    private static string RoleLabel(ScoutingTeamRole role) => role switch {
        ScoutingTeamRole.Owner => "Owner",
        ScoutingTeamRole.Editor => "Editor",
        _ => "Viewer",
    };

    private sealed record ProblemPayload {
        public string? Title { get; init; }
        public string? Detail { get; init; }
    }
}
