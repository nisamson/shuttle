using Bunit;
using Shuttle.Models.Players;
using Shuttle.WebClient.Components.Players;
using Shuttle.WebClient.Testing;

namespace Shuttle.WebClient.Tests;

/// <summary>
/// Render tests for <see cref="PlayerCardTable"/> using seeded data — no browser, server, or Azure.
/// </summary>
public class PlayerCardTableTests : WebClientTestContext {
    [Fact]
    public void Renders_a_row_per_player() {
        var players = SeedData.Players().Take(3).ToList();

        var cut = Render<PlayerCardTable>(p => p.Add(c => c.Players, players));

        var rows = cut.FindAll("tbody tr");
        Assert.Equal(players.Count, rows.Count);
        Assert.Contains(players[0].Name, cut.Markup);
    }

    [Fact]
    public void Shows_loading_row_when_loading() {
        var cut = Render<PlayerCardTable>(p => p
            .Add(c => c.Players, (IReadOnlyList<PlayerCard>?)null)
            .Add(c => c.Loading, true));

        Assert.Contains("Loading players", cut.Markup);
    }

    [Fact]
    public void Shows_empty_message_when_no_players() {
        var cut = Render<PlayerCardTable>(p => p
            .Add(c => c.Players, new List<PlayerCard>())
            .Add(c => c.Loading, false));

        Assert.Contains("No players match your filters", cut.Markup);
    }

    [Fact]
    public void Links_each_row_to_the_player_profile() {
        var player = SeedData.Players()[0];

        var cut = Render<PlayerCardTable>(p => p.Add(c => c.Players, new List<PlayerCard> { player }));

        var link = cut.Find("a.player-link");
        Assert.Equal($"/players/{player.PlayerId}", link.GetAttribute("href"));
    }
}
