using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Shuttle.Api.Services.Scouting;
using Shuttle.Api.Services.Users;
using Shuttle.EFCore;
using Shuttle.EFCore.Entities;
using Shuttle.EFCore.Entities.Portal;
using Shuttle.Models.Scouting;
using Shuttle.Shl.Api.Models.Portal.V1;
using EfEntry = Shuttle.EFCore.Entities.Scouting.ScoutingBoardEntry;
using EfBoard = Shuttle.EFCore.Entities.Scouting.ScoutingBoard;

namespace Shuttle.Tests.Api;

/// <summary>
/// Behavioural tests for <see cref="ScoutingService.AddEntriesAsync"/> — the bulk add of players to a
/// board by upstream id and/or player name. Backed by the EF Core in-memory provider (the model's
/// SQL Server temporal tables and JSON complex properties make a relational test store impractical),
/// so these cover the service's resolution/dedup/rank logic rather than the database's constraints.
/// </summary>
public class ScoutingBoardBulkAddTests {
    private static readonly Guid TeamId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid BoardId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static ShlDbContext CreateContext() {
        var options = new DbContextOptionsBuilder<ShlDbContext>()
            .UseInMemoryDatabase($"scouting-bulk-{Guid.NewGuid()}")
            .Options;
        return new ShlDbContext(options, NullLogger<ShlDbContext>.Instance);
    }

    private static PlayerInformation Player(int id, string name) => new() {
        UserId = id,
        PlayerId = id,
        Username = $"user{id}",
        Name = name,
        CreationTime = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        Status = PlayerStatus.Active,
        Position = default,
        Handedness = default,
        TotalTpe = 0,
        AppliedTpe = 0,
        BankedTpe = 0,
        BankBalance = 0,
    };

    /// <summary>Seeds a board (with the given existing entries) and the supplied players, then returns a service.</summary>
    private static async Task<(ScoutingService Service, ShlDbContext Db)> SetupAsync(
        IEnumerable<PlayerInformation> players,
        IEnumerable<int> existingEntryPlayerIds,
        ScoutingTeamRole? callerRole = ScoutingTeamRole.Editor) {
        var db = CreateContext();

        db.PlayerInformation.AddRange(players);

        var now = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        db.ScoutingBoards.Add(new EfBoard {
            Id = BoardId,
            ScoutingTeamId = TeamId,
            Name = "Board",
            CreatedAt = now,
            UpdatedAt = now,
        });

        var rank = 0;
        foreach (var playerId in existingEntryPlayerIds) {
            rank++;
            db.ScoutingBoardEntries.Add(new EfEntry {
                Id = Guid.CreateVersion7(),
                ScoutingBoardId = BoardId,
                PlayerId = playerId,
                Rank = rank,
                CreatedAt = now,
                UpdatedAt = now,
            });
        }

        await db.SaveChangesAsync(Ct);

        var access = new FakeAccessService(callerRole);
        var service = new ScoutingService(db, access, new StubUserService(), TimeProvider.System);
        return (service, db);
    }

    private static ClaimsPrincipal Caller() => new(new ClaimsIdentity());

    private static async Task<List<(int PlayerId, int Rank)>> EntriesAsync(ShlDbContext db) =>
        (await db.ScoutingBoardEntries
            .Where(e => e.ScoutingBoardId == BoardId)
            .OrderBy(e => e.Rank)
            .Select(e => new { e.PlayerId, e.Rank })
            .ToListAsync(Ct))
        .Select(e => (e.PlayerId, e.Rank))
        .ToList();

    [Fact]
    public async Task Adds_players_by_id_appended_after_max_rank() {
        var (service, db) = await SetupAsync(
            players: [Player(1001, "Alice"), Player(1002, "Bob"), Player(1003, "Carol")],
            existingEntryPlayerIds: [1001]);

        var result = await service.AddEntriesAsync(
            BoardId,
            new AddScoutingBoardEntriesRequest { PlayerIds = [1002, 1003] },
            Caller(),
            Ct);

        Assert.Equal(ScoutingOutcome.Ok, result.Outcome);
        Assert.Equal([1002, 1003], result.Value!.Added);
        Assert.Empty(result.Value.AlreadyOnBoard);
        Assert.Empty(result.Value.NotFound);

        var entries = await EntriesAsync(db);
        Assert.Equal([1001, 1002, 1003], entries.Select(e => e.PlayerId));
        Assert.Equal([1, 2, 3], entries.Select(e => e.Rank));
    }

    [Fact]
    public async Task Resolves_players_by_name() {
        var (service, _) = await SetupAsync(
            players: [Player(1001, "Alice"), Player(1002, "Bob")],
            existingEntryPlayerIds: []);

        var result = await service.AddEntriesAsync(
            BoardId,
            new AddScoutingBoardEntriesRequest { Names = ["bob"] }, // case-insensitive
            Caller(),
            Ct);

        Assert.Equal(ScoutingOutcome.Ok, result.Outcome);
        Assert.Equal([1002], result.Value!.Added);
    }

    [Fact]
    public async Task Ambiguous_name_rejects_whole_request() {
        var (service, db) = await SetupAsync(
            players: [Player(1001, "Dupe"), Player(1002, "Dupe"), Player(1003, "Unique")],
            existingEntryPlayerIds: []);

        var result = await service.AddEntriesAsync(
            BoardId,
            new AddScoutingBoardEntriesRequest { PlayerIds = [1003], Names = ["Dupe"] },
            Caller(),
            Ct);

        Assert.Equal(ScoutingOutcome.Invalid, result.Outcome);
        Assert.Contains("Dupe", result.Error);
        // Nothing was added, including the otherwise-valid id.
        Assert.Empty(await EntriesAsync(db));
    }

    [Fact]
    public async Task Unknown_ids_and_names_are_reported_not_found() {
        var (service, _) = await SetupAsync(
            players: [Player(1001, "Alice")],
            existingEntryPlayerIds: []);

        var result = await service.AddEntriesAsync(
            BoardId,
            new AddScoutingBoardEntriesRequest { PlayerIds = [9999], Names = ["Nobody"] },
            Caller(),
            Ct);

        Assert.Equal(ScoutingOutcome.Ok, result.Outcome);
        Assert.Empty(result.Value!.Added);
        Assert.Contains("9999", result.Value.NotFound);
        Assert.Contains("Nobody", result.Value.NotFound);
    }

    [Fact]
    public async Task Skips_players_already_on_board() {
        var (service, _) = await SetupAsync(
            players: [Player(1001, "Alice"), Player(1002, "Bob")],
            existingEntryPlayerIds: [1001]);

        var result = await service.AddEntriesAsync(
            BoardId,
            new AddScoutingBoardEntriesRequest { PlayerIds = [1001, 1002] },
            Caller(),
            Ct);

        Assert.Equal(ScoutingOutcome.Ok, result.Outcome);
        Assert.Equal([1002], result.Value!.Added);
        Assert.Equal([1001], result.Value.AlreadyOnBoard);
    }

    [Fact]
    public async Task Dedups_same_player_supplied_by_id_and_name() {
        var (service, db) = await SetupAsync(
            players: [Player(1001, "Alice")],
            existingEntryPlayerIds: []);

        var result = await service.AddEntriesAsync(
            BoardId,
            new AddScoutingBoardEntriesRequest { PlayerIds = [1001], Names = ["Alice"] },
            Caller(),
            Ct);

        Assert.Equal(ScoutingOutcome.Ok, result.Outcome);
        Assert.Equal([1001], result.Value!.Added);
        Assert.Single(await EntriesAsync(db));
    }

    [Fact]
    public async Task Empty_request_is_invalid() {
        var (service, _) = await SetupAsync(players: [Player(1001, "Alice")], existingEntryPlayerIds: []);

        var result = await service.AddEntriesAsync(
            BoardId,
            new AddScoutingBoardEntriesRequest(),
            Caller(),
            Ct);

        Assert.Equal(ScoutingOutcome.Invalid, result.Outcome);
    }

    [Fact]
    public async Task Forbidden_when_caller_cannot_edit() {
        var (service, _) = await SetupAsync(
            players: [Player(1001, "Alice")],
            existingEntryPlayerIds: [],
            callerRole: ScoutingTeamRole.Viewer);

        var result = await service.AddEntriesAsync(
            BoardId,
            new AddScoutingBoardEntriesRequest { PlayerIds = [1001] },
            Caller(),
            Ct);

        Assert.Equal(ScoutingOutcome.Forbidden, result.Outcome);
    }

    [Fact]
    public async Task Not_found_when_board_missing() {
        var (service, _) = await SetupAsync(players: [Player(1001, "Alice")], existingEntryPlayerIds: []);

        var result = await service.AddEntriesAsync(
            Guid.NewGuid(),
            new AddScoutingBoardEntriesRequest { PlayerIds = [1001] },
            Caller(),
            Ct);

        Assert.Equal(ScoutingOutcome.NotFound, result.Outcome);
    }

    private sealed class FakeAccessService : IScoutingAccessService {
        private readonly ScoutingTeamRole? role;

        public FakeAccessService(ScoutingTeamRole? role) => this.role = role;

        public Task<ScoutingAccess?> ResolveAsync(Guid teamId, ClaimsPrincipal principal, CancellationToken cancellationToken = default) {
            var user = new ShuttleUser { Id = Guid.NewGuid(), ObjectId = Guid.NewGuid(), Username = "caller" };
            return Task.FromResult<ScoutingAccess?>(new ScoutingAccess(user, IsSiteAdmin: false, role));
        }

        public Task<bool> IsSoleOwnerAsync(Guid teamId, Guid shuttleUserId, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);
    }

    private sealed class StubUserService : IUserService {
        public Task<ShuttleUser> GetOrCreateAsync(Guid objectId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<UpdateUsernameResult> UpdateUsernameAsync(Guid objectId, string username, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
