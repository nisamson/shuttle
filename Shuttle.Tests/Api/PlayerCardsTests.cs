using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Shuttle.Api.Controllers;
using Shuttle.Api.Services;
using Shuttle.EFCore;
using Shuttle.EFCore.Entities.Portal;
using Shuttle.Models.Players;
using Shuttle.Shl.Api.Models.Portal.V1;

namespace Shuttle.Tests.Api;

/// <summary>
/// Behavioural tests for <see cref="PlayerController.GetPlayerCards"/> — the bulk card lookup used by
/// the scouting board and player comparison to resolve many cards in one round trip. These cover the
/// controller's request-guard behaviour (empty/null id sets short-circuit to an empty list; oversized
/// sets are rejected) which runs entirely before touching the database. The card-materialization path
/// itself cannot use the EF Core in-memory provider — projecting the full <c>PlayerInformation</c>
/// entity trips the in-memory shaper on the model's complex attribute properties (see
/// <see cref="PlayerLookupTests"/>) — so the filter/order/dedup behaviour of the card fetch is covered
/// against the in-memory fake client in the WebClient test suite instead.
/// </summary>
public class PlayerCardsTests {
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private sealed class StubFreshnessProvider : IDatabaseFreshnessProvider {
        public Task<DateTimeOffset?> GetLastUpdatedAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<DateTimeOffset?>(null);
    }

    private static ShlDbContext CreateContext() {
        var options = new DbContextOptionsBuilder<ShlDbContext>()
            .UseInMemoryDatabase($"player-cards-{Guid.NewGuid()}")
            .Options;
        return new ShlDbContext(options, NullLogger<ShlDbContext>.Instance);
    }

    private static PlayerInformation Player(int id, string name, int totalTpe = 0, int bankBalance = 0) => new() {
        UserId = id,
        PlayerId = id,
        Username = $"user{id}",
        Name = name,
        CreationTime = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        Status = PlayerStatus.Active,
        Position = default,
        Handedness = default,
        DraftSeason = null,
        TotalTpe = totalTpe,
        AppliedTpe = 0,
        BankedTpe = 0,
        BankBalance = bankBalance,
    };

    private static async Task<PlayerController> SetupAsync(params PlayerInformation[] players) {
        var db = CreateContext();
        db.PlayerInformation.AddRange(players);
        await db.SaveChangesAsync(Ct);
        return new PlayerController(db, new StubFreshnessProvider(), NullLogger<PlayerController>.Instance);
    }

    private static IReadOnlyList<PlayerCard> OkCards(ActionResult<IReadOnlyList<PlayerCard>> action) {
        var ok = Assert.IsType<OkObjectResult>(action.Result);
        return Assert.IsAssignableFrom<IReadOnlyList<PlayerCard>>(ok.Value);
    }

    [Fact]
    public async Task Empty_request_returns_empty_list() {
        var controller = await SetupAsync(Player(1001, "Alice"));

        var result = await controller.GetPlayerCards(new PlayerCardsRequest { PlayerIds = [] }, Ct);

        Assert.Empty(OkCards(result));
    }

    [Fact]
    public async Task Null_ids_returns_empty_list() {
        var controller = await SetupAsync(Player(1001, "Alice"));

        var result = await controller.GetPlayerCards(new PlayerCardsRequest(), Ct);

        Assert.Empty(OkCards(result));
    }

    [Fact]
    public async Task Rejects_too_many_ids_with_400() {
        var controller = await SetupAsync(Player(1001, "Alice"));

        var ids = Enumerable.Range(1, 501).ToList();
        var result = await controller.GetPlayerCards(new PlayerCardsRequest { PlayerIds = ids }, Ct);

        var bad = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status400BadRequest, ((ProblemDetails)bad.Value!).Status);
    }
}
