using Shuttle.Models.Players;
using Shuttle.Shl.Api.Models.Common;
using Shuttle.Shl.Api.Models.Portal.V1;
using Shuttle.WebClient.Testing;

namespace Shuttle.WebClient.Tests;

/// <summary>
/// Pure (no-render) tests for <see cref="InMemoryShuttlePlayerClient"/> proving its
/// filter/sort/paging behaviour matches what the WebClient expects from the real API.
/// </summary>
public class InMemoryShuttlePlayerClientTests {
    private readonly InMemoryShuttlePlayerClient client = new();

    [Fact]
    public async Task GetPlayers_returns_seed_ordered_by_name() {
        var players = await client.GetPlayers();

        Assert.NotEmpty(players);
        Assert.Equal(
            players.Select(p => p.Name).OrderBy(n => n, StringComparer.OrdinalIgnoreCase),
            players.Select(p => p.Name));
    }

    [Fact]
    public async Task GetPlayer_returns_null_for_unknown_id() {
        Assert.Null(await client.GetPlayer(-1));
    }

    [Fact]
    public async Task GetPlayer_returns_card_for_known_id() {
        var first = (await client.GetPlayers())[0];

        var fetched = await client.GetPlayer(first.PlayerId);

        Assert.NotNull(fetched);
        Assert.Equal(first.PlayerId, fetched!.PlayerId);
    }

    [Fact]
    public async Task SearchPlayers_filters_by_status() {
        var result = await client.SearchPlayers(new PlayerSearchQuery {
            Statuses = [PlayerStatus.Retired],
            PageSize = 100,
        });

        Assert.NotEmpty(result.Items);
        Assert.All(result.Items, p => Assert.Equal(PlayerStatus.Retired, p.Status));
    }

    [Fact]
    public async Task SearchPlayers_filters_by_position_short_code() {
        var result = await client.SearchPlayers(new PlayerSearchQuery {
            Positions = ["G"],
            PageSize = 100,
        });

        Assert.NotEmpty(result.Items);
        Assert.All(result.Items, p => Assert.Equal(PlayerPosition.Goalie, p.Position));
    }

    [Fact]
    public async Task SearchPlayers_filters_by_free_text_case_insensitively() {
        var result = await client.SearchPlayers(new PlayerSearchQuery {
            Text = "frost",
            PageSize = 100,
        });

        Assert.NotEmpty(result.Items);
        Assert.All(result.Items, p =>
            Assert.True(
                p.Name.Contains("frost", StringComparison.OrdinalIgnoreCase)
                || p.Username.Contains("frost", StringComparison.OrdinalIgnoreCase)));
    }

    [Fact]
    public async Task SearchPlayers_sorts_by_total_tpe_descending() {
        var result = await client.SearchPlayers(new PlayerSearchQuery {
            SortBy = PlayerSortField.TotalTpe,
            SortDescending = true,
            PageSize = 100,
        });

        var tpe = result.Items.Select(p => p.TotalTpe).ToList();
        Assert.Equal(tpe.OrderByDescending(t => t), tpe);
    }

    [Fact]
    public async Task SearchPlayers_paginates_and_reports_total() {
        var all = await client.SearchPlayers(new PlayerSearchQuery { PageSize = 100 });

        var firstPage = await client.SearchPlayers(new PlayerSearchQuery { Page = 1, PageSize = 5 });

        Assert.Equal(5, firstPage.PageSize);
        Assert.Equal(1, firstPage.Page);
        Assert.Equal(all.TotalCount, firstPage.TotalCount);
        Assert.Equal(Math.Min(5, all.TotalCount), firstPage.Items.Count);
    }

    [Fact]
    public async Task SearchPlayers_clamps_page_size() {
        var result = await client.SearchPlayers(new PlayerSearchQuery { PageSize = 10_000 });

        Assert.Equal(100, result.PageSize);
    }

    [Fact]
    public async Task ResolvePlayers_resolves_by_id_and_name_ids_first() {
        // 1001 = Aaron Frost, 1002 = Bella Ridge in the seed data.
        var result = await client.ResolvePlayers(new ResolvePlayersRequest {
            PlayerIds = [1002],
            Names = ["aaron frost"], // case-insensitive
        });

        Assert.Equal([1002, 1001], result.Resolved.Select(p => p.PlayerId));
        Assert.Empty(result.NotFound);
        Assert.Empty(result.Ambiguous);

        var frost = result.Resolved.Single(p => p.PlayerId == 1001);
        Assert.Equal("Aaron Frost", frost.Name);
        Assert.Equal(70, frost.DraftSeason);
        Assert.Equal(1450, frost.TotalTpe);
    }

    [Fact]
    public async Task ResolvePlayers_reports_unknown_inputs_as_not_found() {
        var result = await client.ResolvePlayers(new ResolvePlayersRequest {
            PlayerIds = [999_999],
            Names = ["No Such Player"],
        });

        Assert.Empty(result.Resolved);
        Assert.Contains("999999", result.NotFound);
        Assert.Contains("No Such Player", result.NotFound);
    }

    [Fact]
    public async Task ResolvePlayers_dedups_id_and_name_for_same_player() {
        var result = await client.ResolvePlayers(new ResolvePlayersRequest {
            PlayerIds = [1001],
            Names = ["Aaron Frost"],
        });

        Assert.Equal([1001], result.Resolved.Select(p => p.PlayerId));
    }

    [Fact]
    public async Task ResolvePlayers_flags_ambiguous_names() {
        var custom = new InMemoryShuttlePlayerClient([Card(2001, "Dup"), Card(2002, "Dup"), Card(2003, "Solo")]);

        var result = await custom.ResolvePlayers(new ResolvePlayersRequest { Names = ["Dup", "Solo"] });

        Assert.Contains("Dup", result.Ambiguous);
        Assert.Equal([2003], result.Resolved.Select(p => p.PlayerId));
        Assert.Empty(result.NotFound);
    }

    private static PlayerCard Card(int id, string name) => new() {
        PlayerId = id,
        UserId = id,
        Username = $"user{id}",
        Name = name,
        Status = PlayerStatus.Active,
        Position = PlayerPosition.Center,
        Handedness = PlayerHandedness.Left,
        TotalTpe = 0,
        AppliedTpe = 0,
        BankedTpe = 0,
        BankBalance = 0,
    };
}
