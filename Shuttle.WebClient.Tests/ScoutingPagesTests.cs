using Bunit;
using Bunit.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using Shuttle.Api.Client;
using Shuttle.Models.Scouting;
using Shuttle.WebClient.Models;
using Shuttle.WebClient.Pages.Scouting;

namespace Shuttle.WebClient.Tests;

/// <summary>
/// Render tests for the three scouting pages (dashboard, team detail, board detail). They run against
/// the shared in-memory scouting client from <see cref="WebClientTestContext"/>, which resolves the
/// caller from bUnit's authorization provider, so seeding data through the client and then rendering a
/// page reflect the same signed-in identity.
/// </summary>
public class ScoutingPagesTests : WebClientTestContext {
    private IShuttleScoutingClient Scouting => Services.GetRequiredService<IShuttleScoutingClient>();

    [Fact]
    public void Dashboard_shows_the_empty_state_when_the_user_has_no_teams() {
        this.AddAuthorization().SetAuthorized("Test Scout");

        var cut = Render<ScoutingDashboard>();

        cut.WaitForState(() => !cut.Markup.Contains("Loading your teams"));
        Assert.Contains("not on any scouting teams", cut.Markup);
    }

    [Fact]
    public async Task Dashboard_lists_a_team_the_user_belongs_to() {
        this.AddAuthorization().SetAuthorized("Test Scout");
        var team = await Scouting.CreateTeam(new CreateScoutingTeamRequest { Name = "Blizzard Scouts" });

        var cut = Render<ScoutingDashboard>();

        cut.WaitForState(() => cut.Markup.Contains("Blizzard Scouts"));
        Assert.Contains(Routes.Scouting.Team(team.Id), cut.Markup);
    }

    [Fact]
    public void Dashboard_shows_the_admin_all_teams_section_for_admins() {
        this.AddAuthorization().SetAuthorized("Admin Scout").SetRoles(RoleNames.Admin);

        var cut = Render<ScoutingDashboard>();

        cut.WaitForState(() => cut.Markup.Contains("All teams (admin)"));
        Assert.Contains("All teams (admin)", cut.Markup);
    }

    [Fact]
    public void Dashboard_hides_the_admin_section_for_non_admins() {
        this.AddAuthorization().SetAuthorized("Test Scout");

        var cut = Render<ScoutingDashboard>();

        cut.WaitForState(() => !cut.Markup.Contains("Loading your teams"));
        Assert.DoesNotContain("All teams (admin)", cut.Markup);
    }

    [Fact]
    public async Task TeamPage_renders_the_team_name_and_the_owner_member() {
        this.AddAuthorization().SetAuthorized("Test Scout");
        var team = await Scouting.CreateTeam(new CreateScoutingTeamRequest { Name = "Falcons Scouting" });

        var cut = Render<ScoutingTeam>(p => p.Add(c => c.TeamId, team.Id));

        cut.WaitForState(() => cut.Markup.Contains("Falcons Scouting"));
        Assert.Contains("Falcons Scouting", cut.Markup);
        // The creator is listed as a member.
        Assert.Contains("Test Scout", cut.Markup);
    }

    [Fact]
    public void TeamPage_shows_an_error_for_a_missing_team() {
        this.AddAuthorization().SetAuthorized("Test Scout");

        var cut = Render<ScoutingTeam>(p => p.Add(c => c.TeamId, Guid.NewGuid()));

        cut.WaitForState(() => !cut.Markup.Contains("Loading team"));
        Assert.Contains(Routes.Scouting.Root, cut.Markup);
    }

    [Fact]
    public async Task BoardPage_renders_the_board_name_and_a_ranked_entry() {
        this.AddAuthorization().SetAuthorized("Test Scout");
        var team = await Scouting.CreateTeam(new CreateScoutingTeamRequest { Name = "Board Owners" });
        var board = await Scouting.CreateBoard(team.Id,
            new CreateScoutingBoardRequest { Name = "Top Prospects", DraftSeason = 73 });
        await Scouting.AddEntry(board.Id, new AddScoutingBoardEntryRequest { PlayerId = 1001 });

        var cut = Render<ScoutingBoard>(p => p.Add(c => c.BoardId, board.Id));

        cut.WaitForState(() => cut.Markup.Contains("Top Prospects"));
        Assert.Contains("Top Prospects", cut.Markup);
        Assert.Contains("Draft season 73", cut.Markup);
    }

    [Fact]
    public async Task BoardPage_grid_shows_the_player_position_tpe_and_bank() {
        this.AddAuthorization().SetAuthorized("Test Scout");
        var team = await Scouting.CreateTeam(new CreateScoutingTeamRequest { Name = "Stat Scouts" });
        var board = await Scouting.CreateBoard(team.Id,
            new CreateScoutingBoardRequest { Name = "Stat Board", DraftSeason = 73 });
        // Aaron Frost (1001): Center, TPE 1,450, bank $12,500 in the seed data.
        await Scouting.AddEntry(board.Id, new AddScoutingBoardEntryRequest { PlayerId = 1001 });

        var cut = Render<ScoutingBoard>(p => p.Add(c => c.BoardId, board.Id));

        cut.WaitForState(() => cut.Markup.Contains("Aaron Frost"));
        Assert.Contains("Aaron Frost", cut.Markup);
        Assert.Contains("1,450", cut.Markup);
        Assert.Contains("12,500", cut.Markup);
    }

    [Fact]
    public async Task BoardPage_shows_bulk_add_button_for_an_editor() {
        this.AddAuthorization().SetAuthorized("Test Scout");
        var team = await Scouting.CreateTeam(new CreateScoutingTeamRequest { Name = "Bulk Scouts" });
        var board = await Scouting.CreateBoard(team.Id,
            new CreateScoutingBoardRequest { Name = "Bulk Board", DraftSeason = 73 });

        var cut = Render<ScoutingBoard>(p => p.Add(c => c.BoardId, board.Id));

        cut.WaitForState(() => cut.Markup.Contains("Bulk Board"));
        Assert.Contains("Bulk add", cut.Markup);
    }
}
