using System.Linq;
using Shuttle.Models.Players;
using Shuttle.Shl.Api.Models.Common;
using Shuttle.Shl.Api.Models.Portal.V1;
using Shuttle.WebClient.Components.Scouting;

namespace Shuttle.WebClient.Tests;

/// <summary>
/// Tests for the "search and add a player" flow on <see cref="ScoutingBulkAddDialog"/>: picking a
/// player appends its (unambiguous) id to the paste box on a new line, and repeated picks of the same
/// player must not duplicate a line. Also covers the sortable resolved-preview grid: the
/// <see cref="Microsoft.FluentUI.AspNetCore.Components.GridSort{T}"/> instances backing each column
/// order rows as expected (with a stable <c>PlayerId</c> tiebreak).
/// </summary>
public class ScoutingBulkAddDialogTests {
    [Fact]
    public void Appends_an_id_to_an_empty_paste_box() {
        Assert.Equal("1042", ScoutingBulkAddDialog.AppendPlayerId(string.Empty, 1042));
    }

    [Fact]
    public void Appends_an_id_on_a_new_line_after_existing_content() {
        Assert.Equal("Wayne Gretzky\n1099",
            ScoutingBulkAddDialog.AppendPlayerId("Wayne Gretzky", 1099));
    }

    [Fact]
    public void Does_not_add_a_trailing_blank_line_when_the_box_ends_with_a_newline() {
        Assert.Equal("1042\n1099",
            ScoutingBulkAddDialog.AppendPlayerId("1042\n", 1099));
    }

    [Fact]
    public void Does_not_duplicate_an_id_already_present() {
        Assert.Equal("1042\n1099",
            ScoutingBulkAddDialog.AppendPlayerId("1042\n1099", 1042));
    }

    [Fact]
    public void Name_sort_orders_alphabetically_ascending_and_descending() {
        var rows = new[] {
            Match(3, "Charlie"),
            Match(1, "alice"),
            Match(2, "Bob"),
        }.AsQueryable();

        var asc = ScoutingBulkAddDialog.NameSort.Apply(rows, ascending: true).ToList();
        Assert.Equal(new[] { "alice", "Bob", "Charlie" }, asc.Select(r => r.Name));

        var desc = ScoutingBulkAddDialog.NameSort.Apply(rows, ascending: false).ToList();
        Assert.Equal(new[] { "Charlie", "Bob", "alice" }, desc.Select(r => r.Name));
    }

    [Fact]
    public void Position_sort_uses_enum_ordering_matching_the_server() {
        var rows = new[] {
            Match(1, "a", PlayerPosition.RightWing),
            Match(2, "b", PlayerPosition.Goalie),
            Match(3, "c", PlayerPosition.Center),
            Match(4, "d", PlayerPosition.LeftDefense),
            Match(5, "e", PlayerPosition.RightDefense),
            Match(6, "f", PlayerPosition.LeftWing),
        }.AsQueryable();

        var asc = ScoutingBulkAddDialog.PositionSort.Apply(rows, ascending: true).ToList();

        // Server ordering is by raw PlayerPosition enum value: G, LD, RD, C, LW, RW.
        Assert.Equal(
            new[] {
                PlayerPosition.Goalie, PlayerPosition.LeftDefense, PlayerPosition.RightDefense,
                PlayerPosition.Center, PlayerPosition.LeftWing, PlayerPosition.RightWing,
            },
            asc.Select(r => r.Position));
    }

    [Fact]
    public void Draft_season_sort_ascending_places_nulls_first() {
        var rows = new[] {
            Match(1, "a", season: 60),
            Match(2, "b", season: null),
            Match(3, "c", season: 55),
        }.AsQueryable();

        var asc = ScoutingBulkAddDialog.DraftSeasonSort.Apply(rows, ascending: true).ToList();
        Assert.Equal(new int?[] { null, 55, 60 }, asc.Select(r => r.DraftSeason));
    }

    [Fact]
    public void Total_tpe_sort_orders_numerically() {
        var rows = new[] {
            Match(1, "a", tpe: 500),
            Match(2, "b", tpe: 1500),
            Match(3, "c", tpe: 900),
        }.AsQueryable();

        var desc = ScoutingBulkAddDialog.TotalTpeSort.Apply(rows, ascending: false).ToList();
        Assert.Equal(new[] { 1500, 900, 500 }, desc.Select(r => r.TotalTpe));
    }

    [Fact]
    public void Ties_break_on_player_id_to_keep_a_stable_order() {
        var rows = new[] {
            Match(30, "same", PlayerPosition.Center),
            Match(10, "same", PlayerPosition.Center),
            Match(20, "same", PlayerPosition.Center),
        }.AsQueryable();

        var asc = ScoutingBulkAddDialog.PositionSort.Apply(rows, ascending: true).ToList();
        Assert.Equal(new[] { 10, 20, 30 }, asc.Select(r => r.PlayerId));
    }

    private static PlayerLookupMatch Match(
        int playerId,
        string name,
        PlayerPosition position = PlayerPosition.Center,
        int? season = null,
        int tpe = 0) =>
        new() {
            PlayerId = playerId,
            Name = name,
            Username = name,
            Status = PlayerStatus.Active,
            Position = position,
            DraftSeason = season,
            TotalTpe = tpe,
        };
}
