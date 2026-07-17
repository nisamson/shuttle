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
    private static TeamCard SampleTeam(string? textColor = "#FFFFFF") => new() {
        TeamId = 10,
        Season = 72,
        League = "SHL",
        LeagueId = 0,
        Name = "Aurora Frost",
        Abbreviation = "AUR",
        Location = "Aurora",
        PrimaryColor = "#0B3D91",
        SecondaryColor = "#B0C4DE",
        TextColor = textColor,
    };

    private string RenderBadge(TeamCard? team, int? teamId) =>
        Render(builder => {
            // TeamBadge renders a FluentTooltip, which needs a FluentTooltipProvider in the render
            // tree (supplied app-wide by <FluentProviders/> in MainLayout).
            builder.OpenComponent<FluentTooltipProvider>(0);
            builder.CloseComponent();
            builder.OpenComponent<TeamBadge>(1);
            builder.AddAttribute(2, nameof(TeamBadge.Team), team);
            builder.AddAttribute(3, nameof(TeamBadge.TeamId), teamId);
            builder.CloseComponent();
        }).Markup;

    [Fact]
    public void Renders_abbreviation_and_team_colors_when_resolved() {
        var team = SampleTeam();

        var markup = RenderBadge(team, team.TeamId);

        Assert.Contains("AUR", markup);
        Assert.Contains("background-color:#0B3D91", markup);
        Assert.Contains("color:#FFFFFF", markup);
        // Full team name is surfaced via the tooltip.
        Assert.Contains("Aurora Frost", markup);
    }

    [Fact]
    public void Uses_secondary_color_for_text_and_outline_when_no_text_color() {
        var team = SampleTeam(textColor: null);

        var markup = RenderBadge(team, team.TeamId);

        Assert.Contains("background-color:#0B3D91", markup);
        Assert.Contains("color:#B0C4DE", markup);
        Assert.Contains("border:1px solid #B0C4DE", markup);
    }

    [Fact]
    public void Falls_back_to_accessible_black_or_white_when_secondary_is_low_contrast() {
        // Primary and secondary are both dark blues with far too little contrast to read.
        var team = SampleTeam(textColor: null) with {
            PrimaryColor = "#0B3D91",
            SecondaryColor = "#123A80",
        };

        var markup = RenderBadge(team, team.TeamId);

        Assert.Contains("background-color:#0B3D91", markup);
        // White is the accessible choice against a dark background; the outline keeps the team color.
        Assert.Contains("color:#FFFFFF", markup);
        Assert.Contains("border:1px solid #123A80", markup);
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
