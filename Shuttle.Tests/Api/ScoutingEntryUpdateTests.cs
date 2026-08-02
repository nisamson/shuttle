using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Shuttle.Api.Services.Scouting;
using Shuttle.Api.Services.Users;
using Shuttle.EFCore;
using Shuttle.EFCore.Entities;
using Shuttle.Models.Scouting;
using EfEntry = Shuttle.EFCore.Entities.Scouting.ScoutingBoardEntry;
using EfBoard = Shuttle.EFCore.Entities.Scouting.ScoutingBoard;
using EfMember = Shuttle.EFCore.Entities.Scouting.ScoutingTeamMember;

namespace Shuttle.Tests.Api;

/// <summary>
/// Behavioural tests for <see cref="ScoutingService.UpdateEntryAsync"/> — editing a prospect's
/// status, assignment, and rank. Backed by the EF Core in-memory provider (the model's SQL Server
/// temporal tables make a relational test store impractical), so these cover the service's status
/// transitions, rank-compaction/append logic, and assignee validation.
/// </summary>
public class ScoutingEntryUpdateTests {
    private static readonly Guid TeamId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid BoardId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static ShlDbContext CreateContext() {
        var options = new DbContextOptionsBuilder<ShlDbContext>()
            .UseInMemoryDatabase($"scouting-update-{Guid.NewGuid()}")
            .Options;
        return new ShlDbContext(options, NullLogger<ShlDbContext>.Instance);
    }

    private static async Task<(ScoutingService Service, ShlDbContext Db)> SetupAsync(
        IEnumerable<int> entryPlayerIds,
        IEnumerable<EfMember>? members = null,
        ScoutingTeamRole? callerRole = ScoutingTeamRole.Editor) {
        var db = CreateContext();
        var now = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);

        db.ScoutingBoards.Add(new EfBoard {
            Id = BoardId,
            ScoutingTeamId = TeamId,
            Name = "Board",
            CreatedAt = now,
            UpdatedAt = now,
        });

        var rank = 0;
        foreach (var playerId in entryPlayerIds) {
            rank++;
            db.ScoutingBoardEntries.Add(new EfEntry {
                Id = Guid.CreateVersion7(),
                ScoutingBoardId = BoardId,
                PlayerId = playerId,
                Rank = rank,
                Status = ScoutingProspectStatus.Pending,
                CreatedAt = now,
                UpdatedAt = now,
            });
        }

        if (members is not null) {
            foreach (var member in members) {
                db.ShuttleUsers.Add(new ShuttleUser {
                    Id = member.ShuttleUserId,
                    ObjectId = Guid.NewGuid(),
                    Username = $"member-{member.ShuttleUserId:N}"[..12],
                });
                db.ScoutingTeamMembers.Add(member);
            }
        }

        await db.SaveChangesAsync(Ct);

        var access = new FakeAccessService(callerRole);
        var service = new ScoutingService(db, access, new StubUserService(), TimeProvider.System);
        return (service, db);
    }

    private static EfMember Member(Guid userId, ScoutingTeamRole role) => new() {
        Id = Guid.NewGuid(),
        ScoutingTeamId = TeamId,
        ShuttleUserId = userId,
        Role = role,
        CreatedAt = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero),
    };

    private static ClaimsPrincipal Caller() => new(new ClaimsIdentity());

    private static async Task<List<(int PlayerId, int Rank, ScoutingProspectStatus Status, Guid? Assignee)>> EntriesAsync(ShlDbContext db) =>
        (await db.ScoutingBoardEntries
            .Where(e => e.ScoutingBoardId == BoardId)
            .Select(e => new { e.PlayerId, e.Rank, e.Status, e.AssignedToUserId })
            .ToListAsync(Ct))
        .Select(e => (e.PlayerId, e.Rank, e.Status, e.AssignedToUserId))
        .ToList();

    private static UpdateScoutingBoardEntryRequest Request(
        ScoutingProspectStatus status,
        Guid? assignee = null,
        int? rank = null) => new() { Status = status, AssignedToUserId = assignee, Rank = rank };

    [Fact]
    public async Task Rejecting_unranks_the_prospect_and_compacts_active_ranks() {
        var (service, db) = await SetupAsync([1001, 1002, 1003]);

        var result = await service.UpdateEntryAsync(BoardId, 1002, Request(ScoutingProspectStatus.Rejected), Caller(), Ct);

        Assert.Equal(ScoutingOutcome.Ok, result.Outcome);
        var entries = await EntriesAsync(db);
        Assert.Equal(0, entries.Single(e => e.PlayerId == 1002).Rank);
        Assert.Equal(ScoutingProspectStatus.Rejected, entries.Single(e => e.PlayerId == 1002).Status);
        Assert.Equal(1, entries.Single(e => e.PlayerId == 1001).Rank);
        Assert.Equal(2, entries.Single(e => e.PlayerId == 1003).Rank);
    }

    [Fact]
    public async Task Restoring_a_rejected_prospect_appends_it_to_the_active_end() {
        var (service, db) = await SetupAsync([1001, 1002, 1003]);
        await service.UpdateEntryAsync(BoardId, 1002, Request(ScoutingProspectStatus.Rejected), Caller(), Ct);

        var result = await service.UpdateEntryAsync(BoardId, 1002, Request(ScoutingProspectStatus.Scouted), Caller(), Ct);

        Assert.Equal(ScoutingOutcome.Ok, result.Outcome);
        var entries = await EntriesAsync(db);
        // 1001->1, 1003->2 after rejection; restored 1002 appends at 3.
        Assert.Equal(3, entries.Single(e => e.PlayerId == 1002).Rank);
        Assert.Equal(ScoutingProspectStatus.Scouted, entries.Single(e => e.PlayerId == 1002).Status);
    }

    [Fact]
    public async Task Restoring_with_an_explicit_rank_places_the_prospect_there() {
        var (service, db) = await SetupAsync([1001, 1002, 1003]);
        await service.UpdateEntryAsync(BoardId, 1002, Request(ScoutingProspectStatus.Rejected), Caller(), Ct);

        var result = await service.UpdateEntryAsync(
            BoardId, 1002, Request(ScoutingProspectStatus.Scouted, rank: 1), Caller(), Ct);

        Assert.Equal(ScoutingOutcome.Ok, result.Outcome);
        var entries = await EntriesAsync(db);
        Assert.Equal(1, entries.Single(e => e.PlayerId == 1002).Rank);
        Assert.Equal(2, entries.Single(e => e.PlayerId == 1001).Rank);
        Assert.Equal(3, entries.Single(e => e.PlayerId == 1003).Rank);
    }

    [Fact]
    public async Task Status_only_change_keeps_the_rank() {
        var (service, db) = await SetupAsync([1001, 1002]);

        var result = await service.UpdateEntryAsync(BoardId, 1001, Request(ScoutingProspectStatus.Scouted), Caller(), Ct);

        Assert.Equal(ScoutingOutcome.Ok, result.Outcome);
        var entries = await EntriesAsync(db);
        Assert.Equal(1, entries.Single(e => e.PlayerId == 1001).Rank);
        Assert.Equal(ScoutingProspectStatus.Scouted, entries.Single(e => e.PlayerId == 1001).Status);
    }

    [Fact]
    public async Task Rank_change_reorders_active_prospects() {
        var (service, db) = await SetupAsync([1001, 1002, 1003]);

        var result = await service.UpdateEntryAsync(
            BoardId, 1003, Request(ScoutingProspectStatus.Pending, rank: 1), Caller(), Ct);

        Assert.Equal(ScoutingOutcome.Ok, result.Outcome);
        var entries = await EntriesAsync(db);
        Assert.Equal(1, entries.Single(e => e.PlayerId == 1003).Rank);
        Assert.Equal(2, entries.Single(e => e.PlayerId == 1001).Rank);
        Assert.Equal(3, entries.Single(e => e.PlayerId == 1002).Rank);
    }

    [Fact]
    public async Task Assigning_to_an_editor_member_succeeds() {
        var editor = Guid.NewGuid();
        var (service, db) = await SetupAsync([1001], members: [Member(editor, ScoutingTeamRole.Editor)]);

        var result = await service.UpdateEntryAsync(
            BoardId, 1001, Request(ScoutingProspectStatus.Scouted, assignee: editor), Caller(), Ct);

        Assert.Equal(ScoutingOutcome.Ok, result.Outcome);
        Assert.Equal(editor, result.Value!.AssignedToUserId);
        Assert.Equal(editor, (await EntriesAsync(db)).Single().Assignee);
    }

    [Fact]
    public async Task Assigning_to_a_viewer_member_is_invalid() {
        var viewer = Guid.NewGuid();
        var (service, _) = await SetupAsync([1001], members: [Member(viewer, ScoutingTeamRole.Viewer)]);

        var result = await service.UpdateEntryAsync(
            BoardId, 1001, Request(ScoutingProspectStatus.Scouted, assignee: viewer), Caller(), Ct);

        Assert.Equal(ScoutingOutcome.Invalid, result.Outcome);
        Assert.Contains("edit access", result.Error);
    }

    [Fact]
    public async Task Assigning_to_a_non_member_is_invalid() {
        var (service, _) = await SetupAsync([1001]);

        var result = await service.UpdateEntryAsync(
            BoardId, 1001, Request(ScoutingProspectStatus.Scouted, assignee: Guid.NewGuid()), Caller(), Ct);

        Assert.Equal(ScoutingOutcome.Invalid, result.Outcome);
        Assert.Contains("not a member", result.Error);
    }

    [Fact]
    public async Task Assignment_can_be_cleared() {
        var editor = Guid.NewGuid();
        var (service, db) = await SetupAsync([1001], members: [Member(editor, ScoutingTeamRole.Editor)]);
        await service.UpdateEntryAsync(BoardId, 1001, Request(ScoutingProspectStatus.Scouted, assignee: editor), Caller(), Ct);

        var result = await service.UpdateEntryAsync(BoardId, 1001, Request(ScoutingProspectStatus.Scouted), Caller(), Ct);

        Assert.Equal(ScoutingOutcome.Ok, result.Outcome);
        Assert.Null((await EntriesAsync(db)).Single().Assignee);
    }

    [Fact]
    public async Task Forbidden_when_caller_cannot_edit() {
        var (service, _) = await SetupAsync([1001], callerRole: ScoutingTeamRole.Viewer);

        var result = await service.UpdateEntryAsync(BoardId, 1001, Request(ScoutingProspectStatus.Scouted), Caller(), Ct);

        Assert.Equal(ScoutingOutcome.Forbidden, result.Outcome);
    }

    [Fact]
    public async Task Not_found_when_player_not_on_board() {
        var (service, _) = await SetupAsync([1001]);

        var result = await service.UpdateEntryAsync(BoardId, 9999, Request(ScoutingProspectStatus.Scouted), Caller(), Ct);

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
