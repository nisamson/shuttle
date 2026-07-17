using Bunit;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.FluentUI.AspNetCore.Components;
using Shuttle.Models.Leagues;
using Shuttle.WebClient.Components.Players;

namespace Shuttle.WebClient.Tests;

/// <summary>
/// Render tests for <see cref="TeamBadge"/> covering the resolved, unresolved, and empty states.
/// </summary>
public class TeamBadgeTests : WebClientTestContext {
    private static TeamCard SampleTeam() => new() {
        TeamId = 10,
        Season = 72,
        League = "SHL",
        LeagueId = 0,
        Name = "Aurora Frost",
        Abbreviation = "AUR",
        Location = "Aurora",
        PrimaryColor = "#0B3D91",
        SecondaryColor = "#B0C4DE",
        TextColor = "#FFFFFF",
    };

    [Fact]
    public void Renders_abbreviation_and_team_colors_when_resolved() {
        var team = SampleTeam();

        // TeamBadge renders a FluentTooltip, which needs a FluentTooltipProvider in the render
        // tree (supplied app-wide by <FluentProviders/> in MainLayout).
        var cut = Render(builder => {
            builder.OpenComponent<FluentTooltipProvider>(0);
            builder.CloseComponent();
            builder.OpenComponent<TeamBadge>(1);
            builder.AddAttribute(2, nameof(TeamBadge.Team), team);
            builder.AddAttribute(3, nameof(TeamBadge.TeamId), (int?)team.TeamId);
            builder.CloseComponent();
        });

        Assert.Contains("AUR", cut.Markup);
        Assert.Contains("background-color:#0B3D91", cut.Markup);
        // Full team name is surfaced via the tooltip.
        Assert.Contains("Aurora Frost", cut.Markup);
    }

    [Fact]
    public void Falls_back_to_the_raw_id_when_team_is_unresolved() {
        var cut = Render<TeamBadge>(p => p
            .Add(c => c.Team, (TeamCard?)null)
            .Add(c => c.TeamId, 42));

        Assert.Contains("#42", cut.Markup);
    }

    [Fact]
    public void Renders_a_dash_when_no_team_reference() {
        var cut = Render<TeamBadge>(p => p
            .Add(c => c.Team, (TeamCard?)null)
            .Add(c => c.TeamId, (int?)null));

        Assert.Contains("—", cut.Markup);
    }
}
