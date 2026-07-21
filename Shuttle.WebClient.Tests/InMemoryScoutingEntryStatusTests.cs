using System.Net;
using Refit;
using Shuttle.Models.Scouting;
using Shuttle.WebClient.Testing;

namespace Shuttle.WebClient.Tests;

/// <summary>
/// Tests for <see cref="InMemoryShuttleScoutingClient"/>'s prospect status/assignment behaviour,
/// proving the fake mirrors the server's status transitions (reject unranks and compacts, restore
/// appends) and assignee validation the WebClient relies on.
/// </summary>
public class InMemoryScoutingEntryStatusTests {
    private const string OwnerId = "00000000-0000-0000-0000-0000000000b1";

    private readonly InMemoryShuttleScoutingClient client;

    public InMemoryScoutingEntryStatusTests() {
        var auth = new FakeAuthenticationStateProvider(new FakeAuthOptions {
            IsAuthenticated = true,
            UserId = OwnerId,
            UserName = "Owner",
            Roles = [],
        });
        client = new InMemoryShuttleScoutingClient(auth);
    }

    private async Task<ScoutingBoardDetail> BoardWithEntriesAsync(params int[] playerIds) {
        var team = await client.CreateTeam(new CreateScoutingTeamRequest { Name = "Scouts" });
        var board = await client.CreateBoard(team.Id, new CreateScoutingBoardRequest { Name = "Board" });
        foreach (var id in playerIds) {
            await client.AddEntry(board.Id, new AddScoutingBoardEntryRequest { PlayerId = id });
        }

        return await client.GetBoard(board.Id);
    }

    [Fact]
    public async Task Rejecting_unranks_the_prospect_and_compacts_active_ranks() {
        var board = await BoardWithEntriesAsync(1001, 1002, 1003);

        await client.UpdateEntry(board.Id, 1002, new UpdateScoutingBoardEntryRequest {
            Status = ScoutingProspectStatus.Rejected,
        });

        var refreshed = await client.GetBoard(board.Id);
        Assert.Equal(ScoutingProspectStatus.Rejected, refreshed.Entries.Single(e => e.PlayerId == 1002).Status);
        Assert.Equal(0, refreshed.Entries.Single(e => e.PlayerId == 1002).Rank);
        Assert.Equal(1, refreshed.Entries.Single(e => e.PlayerId == 1001).Rank);
        Assert.Equal(2, refreshed.Entries.Single(e => e.PlayerId == 1003).Rank);
    }

    [Fact]
    public async Task Restoring_a_rejected_prospect_appends_it_to_the_active_end() {
        var board = await BoardWithEntriesAsync(1001, 1002, 1003);
        await client.UpdateEntry(board.Id, 1002, new UpdateScoutingBoardEntryRequest {
            Status = ScoutingProspectStatus.Rejected,
        });

        await client.UpdateEntry(board.Id, 1002, new UpdateScoutingBoardEntryRequest {
            Status = ScoutingProspectStatus.Scouted,
        });

        var refreshed = await client.GetBoard(board.Id);
        Assert.Equal(3, refreshed.Entries.Single(e => e.PlayerId == 1002).Rank);
        Assert.Equal(ScoutingProspectStatus.Scouted, refreshed.Entries.Single(e => e.PlayerId == 1002).Status);
    }

    [Fact]
    public async Task Assigning_to_a_non_member_is_rejected() {
        var board = await BoardWithEntriesAsync(1001);

        var ex = await Assert.ThrowsAnyAsync<ApiException>(() =>
            client.UpdateEntry(board.Id, 1001, new UpdateScoutingBoardEntryRequest {
                Status = ScoutingProspectStatus.Scouted,
                AssignedToUserId = Guid.NewGuid(),
            }));

        Assert.Equal(HttpStatusCode.BadRequest, ex.StatusCode);
    }
}
