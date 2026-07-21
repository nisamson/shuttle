using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Shuttle.Api.Controllers;
using Shuttle.EFCore;
using Shuttle.EFCore.Entities.Portal;
using Shuttle.Models.Players;
using Shuttle.Shl.Api.Models.Portal.V1;

namespace Shuttle.Tests.Api;

/// <summary>
/// Behavioural tests for <see cref="PlayerController.LookupPlayers"/> — the batch lookup of
/// player ids and/or names used by the WebClient bulk-add preview. Backed by the EF Core in-memory
/// provider (the model's SQL Server temporal tables and JSON complex properties make a relational
/// test store impractical), so these cover the controller's resolution/dedup ordering rather than
/// database constraints.
/// </summary>
public class PlayerLookupTests {
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static ShlDbContext CreateContext() {
        var options = new DbContextOptionsBuilder<ShlDbContext>()
            .UseInMemoryDatabase($"player-resolve-{Guid.NewGuid()}")
            .Options;
        return new ShlDbContext(options, NullLogger<ShlDbContext>.Instance);
    }

    private static PlayerInformation Player(int id, string name, int? draftSeason = null, int totalTpe = 0) => new() {
        UserId = id,
        PlayerId = id,
        Username = $"user{id}",
        Name = name,
        CreationTime = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        Status = PlayerStatus.Active,
        Position = default,
        Handedness = default,
        DraftSeason = draftSeason,
        TotalTpe = totalTpe,
        AppliedTpe = 0,
        BankedTpe = 0,
        BankBalance = 0,
    };

    private static async Task<PlayerController> SetupAsync(params PlayerInformation[] players) {
        var db = CreateContext();
        db.PlayerInformation.AddRange(players);
        await db.SaveChangesAsync(Ct);
        return new PlayerController(db, NullLogger<PlayerController>.Instance);
    }

    private static async Task<PlayerLookupResult> LookupAsync(PlayerController controller, PlayerLookupRequest request) {
        var action = await controller.LookupPlayers(request, Ct);
        var ok = Assert.IsType<OkObjectResult>(action.Result);
        return Assert.IsType<PlayerLookupResult>(ok.Value);
    }

    [Fact]
    public async Task Resolves_by_id_and_name_preserving_order_ids_first() {
        var controller = await SetupAsync(
            Player(1001, "Alice", draftSeason: 85, totalTpe: 400),
            Player(1002, "Bob", draftSeason: 86, totalTpe: 500),
            Player(1003, "Carol"));

        var result = await LookupAsync(controller, new PlayerLookupRequest {
            PlayerIds = [1002],
            Names = ["alice"], // case-insensitive
        });

        Assert.Equal([1002, 1001], result.Resolved.Select(p => p.PlayerId));
        Assert.Empty(result.NotFound);
        Assert.Empty(result.Ambiguous);

        var bob = result.Resolved[0];
        Assert.Equal("Bob", bob.Name);
        Assert.Equal(86, bob.DraftSeason);
        Assert.Equal(500, bob.TotalTpe);
    }

    [Fact]
    public async Task Reports_unknown_ids_and_names_as_not_found() {
        var controller = await SetupAsync(Player(1001, "Alice"));

        var result = await LookupAsync(controller, new PlayerLookupRequest {
            PlayerIds = [9999],
            Names = ["Nobody"],
        });

        Assert.Empty(result.Resolved);
        Assert.Contains("9999", result.NotFound);
        Assert.Contains("Nobody", result.NotFound);
    }

    [Fact]
    public async Task Ambiguous_name_is_reported_and_not_resolved() {
        var controller = await SetupAsync(
            Player(1001, "Dupe"),
            Player(1002, "Dupe"),
            Player(1003, "Unique"));

        var result = await LookupAsync(controller, new PlayerLookupRequest {
            PlayerIds = [1003],
            Names = ["Dupe"],
        });

        // The ambiguous name is reported; the unrelated id still resolves.
        Assert.Contains("Dupe", result.Ambiguous);
        Assert.Equal([1003], result.Resolved.Select(p => p.PlayerId));
        Assert.Empty(result.NotFound);
    }

    [Fact]
    public async Task Dedups_same_player_supplied_by_id_and_name() {
        var controller = await SetupAsync(Player(1001, "Alice"));

        var result = await LookupAsync(controller, new PlayerLookupRequest {
            PlayerIds = [1001],
            Names = ["Alice"],
        });

        Assert.Equal([1001], result.Resolved.Select(p => p.PlayerId));
    }

    [Fact]
    public async Task Empty_request_is_bad_request() {
        var controller = await SetupAsync(Player(1001, "Alice"));

        var action = await controller.LookupPlayers(new PlayerLookupRequest(), Ct);

        var bad = Assert.IsType<BadRequestObjectResult>(action.Result);
        Assert.Equal(StatusCodes.Status400BadRequest, bad.StatusCode);
    }

    [Fact]
    public async Task Too_many_inputs_is_bad_request() {
        var controller = await SetupAsync(Player(1001, "Alice"));

        var action = await controller.LookupPlayers(
            new PlayerLookupRequest { PlayerIds = Enumerable.Range(1, 201).ToList() },
            Ct);

        Assert.IsType<BadRequestObjectResult>(action.Result);
    }
}
