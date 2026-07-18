using Shuttle.WebClient.Testing;

namespace Shuttle.WebClient.Tests;

/// <summary>
/// Pure (no-render) tests for <see cref="InMemoryShuttleLeagueClient"/> proving its team lookup
/// matches what the WebClient expects from the real <c>GET /leagues/{league}/teams/{teamId}</c>.
/// </summary>
public class InMemoryShuttleLeagueClientTests {
    private readonly InMemoryShuttleLeagueClient client = new();

    [Fact]
    public async Task GetTeam_returns_a_seeded_team_by_league_and_id() {
        var team = await client.GetTeam("SHL", 10);

        Assert.NotNull(team);
        Assert.Equal(10, team!.TeamId);
        Assert.Equal("SHL", team.League);
        Assert.False(string.IsNullOrWhiteSpace(team.Abbreviation));
        Assert.StartsWith("#", team.PrimaryColor);
    }

    [Fact]
    public async Task GetTeam_is_case_insensitive_on_league() {
        var upper = await client.GetTeam("SMJHL", 12);
        var lower = await client.GetTeam("smjhl", 12);

        Assert.NotNull(upper);
        Assert.NotNull(lower);
        Assert.Equal(upper!.Name, lower!.Name);
    }

    [Fact]
    public async Task GetTeam_returns_the_same_id_in_different_leagues() {
        var shl = await client.GetTeam("SHL", 15);
        var smjhl = await client.GetTeam("SMJHL", 15);

        Assert.NotNull(shl);
        Assert.NotNull(smjhl);
        Assert.Equal("SHL", shl!.League);
        Assert.Equal("SMJHL", smjhl!.League);
    }

    [Fact]
    public async Task GetTeam_returns_null_for_unknown_team() {
        Assert.Null(await client.GetTeam("SHL", -1));
    }

    [Fact]
    public async Task GetTeam_returns_null_for_unknown_league() {
        Assert.Null(await client.GetTeam("NHL", 10));
    }
}
