using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.FluentUI.AspNetCore.Components;
using Refit;
using Shuttle.Api.Client;
using Shuttle.Models.Scouting;
using Shuttle.WebClient.Models;

namespace Shuttle.WebClient.Pages.Scouting;

public partial class ScoutingDashboard : ComponentBase {
    [Inject] private IShuttleScoutingClient ScoutingClient { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;

    [CascadingParameter] private Task<AuthenticationState>? AuthState { get; set; }

    private readonly GridSort<ScoutingTeamSummary> nameSort =
        GridSort<ScoutingTeamSummary>.ByAscending(t => t.Name);

    private IReadOnlyList<ScoutingTeamSummary> myTeams = [];
    private IReadOnlyList<ScoutingTeamSummary> allTeams = [];
    private bool loading = true;
    private bool loadingAllTeams;
    private bool busy;
    private bool showCreate;
    private string newTeamName = string.Empty;
    private string? errorMessage;

    protected override async Task OnInitializedAsync() {
        await LoadAsync();

        if (await IsAdminAsync()) {
            await LoadAllTeamsAsync();
        }
    }

    private async Task LoadAsync() {
        loading = true;
        errorMessage = null;
        try {
            myTeams = await ScoutingClient.GetMyTeams();
        } catch (ApiException ex) {
            errorMessage = $"Failed to load teams ({(int)ex.StatusCode}).";
        } catch (HttpRequestException) {
            errorMessage = "Failed to reach the server. Please try again.";
        } finally {
            loading = false;
        }
    }

    private async Task LoadAllTeamsAsync() {
        loadingAllTeams = true;
        try {
            allTeams = await ScoutingClient.GetAllTeams();
        } catch (ApiException) {
            allTeams = [];
        } catch (HttpRequestException) {
            allTeams = [];
        } finally {
            loadingAllTeams = false;
        }
    }

    private void ToggleCreate() {
        showCreate = !showCreate;
        newTeamName = string.Empty;
        errorMessage = null;
    }

    private async Task CreateTeamAsync() {
        var name = newTeamName.Trim();
        if (name.Length == 0) {
            return;
        }

        busy = true;
        errorMessage = null;
        try {
            var team = await ScoutingClient.CreateTeam(new CreateScoutingTeamRequest { Name = name });
            Navigation.NavigateTo(Routes.Scouting.Team(team.Id));
        } catch (ApiException ex) {
            errorMessage = $"Failed to create the team ({(int)ex.StatusCode}).";
        } catch (HttpRequestException) {
            errorMessage = "Failed to reach the server. Please try again.";
        } finally {
            busy = false;
        }
    }

    private async Task<bool> IsAdminAsync() {
        if (AuthState is null) {
            return false;
        }

        var state = await AuthState;
        return state.User.IsInRole(RoleNames.Admin);
    }

    private static string RoleLabel(ScoutingTeamRole? role) => role switch {
        ScoutingTeamRole.Owner => "Owner",
        ScoutingTeamRole.Editor => "Editor",
        ScoutingTeamRole.Viewer => "Viewer",
        _ => "—",
    };
}
