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
/// Behavioural tests for <see cref="ScoutingService.UpdateEntriesAsync"/> — applying a status and/or
/// assignee change to several prospects at once. Backed by the EF Core in-memory provider (the
/// model's SQL Server temporal tables make a relational store impractical); cover the bulk
/// status/rank recomputation, shared assignee validation, and the change-assignee flag semantics.
/// </summary>
public class ScoutingBulkUpdateTests {
    private static readonly Guid TeamId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid BoardId = Guid.Parse("44444444-4444-4444-4444-444444444444");

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static ShlDbContext CreateContext() {
        var options = new DbContextOptionsBuilder<ShlDbContext>()
            .UseInMemoryDatabase($"scouting-bulk-{Guid.NewGuid()}")
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

    private static BulkUpdateScoutingBoardEntriesRequest Request(
        IReadOnlyList<int> playerIds,
        ScoutingProspectStatus? status = null,
        bool changeAssignee = false,
        Guid? assignee = null) => new() {
            PlayerIds = playerIds,
            Status = status,
            ChangeAssignee = changeAssignee,
            AssignedToUserId = assignee,
        };

    [Fact]
    public async Task Bulk_rejecting_unranks_the_selected_and_compacts_active_ranks() {
        var (service, db) = await SetupAsync([1001, 1002, 1003, 1004]);

        var result = await service.UpdateEntriesAsync(
            BoardId, Request([1002, 1003], ScoutingProspectStatus.Rejected), Caller(), Ct);

        Assert.Equal(ScoutingOutcome.Ok, result.Outcome);
        var entries = await EntriesAsync(db);
        Assert.Equal(0, entries.Single(e => e.PlayerId == 1002).Rank);
        Assert.Equal(0, entries.Single(e => e.PlayerId == 1003).Rank);
        Assert.Equal(ScoutingProspectStatus.Rejected, entries.Single(e => e.PlayerId == 1002).Status);
        // The two survivors keep their relative order and are compacted to 1..2.
        Assert.Equal(1, entries.Single(e => e.PlayerId == 1001).Rank);
        Assert.Equal(2, entries.Single(e => e.PlayerId == 1004).Rank);
    }

    [Fact]
    public async Task Bulk_restoring_rejected_prospects_appends_them_to_the_active_end() {
        var (service, db) = await SetupAsync([1001, 1002, 1003]);
        await service.UpdateEntriesAsync(
            BoardId, Request([1001, 1002], ScoutingProspectStatus.Rejected), Caller(), Ct);

        var result = await service.UpdateEntriesAsync(
            BoardId, Request([1001, 1002], ScoutingProspectStatus.Scouted), Caller(), Ct);

        Assert.Equal(ScoutingOutcome.Ok, result.Outcome);
        var entries = await EntriesAsync(db);
        // 1003 stayed active at rank 1; restored 1001/1002 append after it (ordered by player id).
        Assert.Equal(1, entries.Single(e => e.PlayerId == 1003).Rank);
        Assert.Equal(2, entries.Single(e => e.PlayerId == 1001).Rank);
        Assert.Equal(3, entries.Single(e => e.PlayerId == 1002).Rank);
    }

    [Fact]
    public async Task Bulk_assign_sets_the_assignee_on_every_selected_prospect() {
        var editor = Guid.NewGuid();
        var (service, db) = await SetupAsync([1001, 1002, 1003], members: [Member(editor, ScoutingTeamRole.Editor)]);

        var result = await service.UpdateEntriesAsync(
            BoardId, Request([1001, 1002], changeAssignee: true, assignee: editor), Caller(), Ct);

        Assert.Equal(ScoutingOutcome.Ok, result.Outcome);
        var entries = await EntriesAsync(db);
        Assert.Equal(editor, entries.Single(e => e.PlayerId == 1001).Assignee);
        Assert.Equal(editor, entries.Single(e => e.PlayerId == 1002).Assignee);
        Assert.Null(entries.Single(e => e.PlayerId == 1003).Assignee);
    }

    [Fact]
    public async Task Bulk_assign_to_a_viewer_is_invalid() {
        var viewer = Guid.NewGuid();
        var (service, _) = await SetupAsync([1001, 1002], members: [Member(viewer, ScoutingTeamRole.Viewer)]);

        var result = await service.UpdateEntriesAsync(
            BoardId, Request([1001, 1002], changeAssignee: true, assignee: viewer), Caller(), Ct);

        Assert.Equal(ScoutingOutcome.Invalid, result.Outcome);
        Assert.Contains("edit access", result.Error);
    }

    [Fact]
    public async Task Bulk_assign_to_a_non_member_is_invalid() {
        var (service, _) = await SetupAsync([1001, 1002]);

        var result = await service.UpdateEntriesAsync(
            BoardId, Request([1001, 1002], changeAssignee: true, assignee: Guid.NewGuid()), Caller(), Ct);

        Assert.Equal(ScoutingOutcome.Invalid, result.Outcome);
        Assert.Contains("not a member", result.Error);
    }

    [Fact]
    public async Task Bulk_unassign_clears_the_assignee_when_change_assignee_is_set() {
        var editor = Guid.NewGuid();
        var (service, db) = await SetupAsync([1001, 1002], members: [Member(editor, ScoutingTeamRole.Editor)]);
        await service.UpdateEntriesAsync(BoardId, Request([1001, 1002], changeAssignee: true, assignee: editor), Caller(), Ct);

        var result = await service.UpdateEntriesAsync(
            BoardId, Request([1001, 1002], changeAssignee: true, assignee: null), Caller(), Ct);

        Assert.Equal(ScoutingOutcome.Ok, result.Outcome);
        var entries = await EntriesAsync(db);
        Assert.Null(entries.Single(e => e.PlayerId == 1001).Assignee);
        Assert.Null(entries.Single(e => e.PlayerId == 1002).Assignee);
    }

    [Fact]
    public async Task Status_only_change_leaves_the_assignee_untouched() {
        var editor = Guid.NewGuid();
        var (service, db) = await SetupAsync([1001], members: [Member(editor, ScoutingTeamRole.Editor)]);
        await service.UpdateEntriesAsync(BoardId, Request([1001], changeAssignee: true, assignee: editor), Caller(), Ct);

        var result = await service.UpdateEntriesAsync(
            BoardId, Request([1001], ScoutingProspectStatus.Approved), Caller(), Ct);

        Assert.Equal(ScoutingOutcome.Ok, result.Outcome);
        var entry = (await EntriesAsync(db)).Single();
        Assert.Equal(ScoutingProspectStatus.Approved, entry.Status);
        Assert.Equal(editor, entry.Assignee);
    }

    [Fact]
    public async Task Combined_status_and_assignee_apply_together() {
        var editor = Guid.NewGuid();
        var (service, db) = await SetupAsync([1001, 1002], members: [Member(editor, ScoutingTeamRole.Editor)]);

        var result = await service.UpdateEntriesAsync(
            BoardId, Request([1001, 1002], ScoutingProspectStatus.Approved, changeAssignee: true, assignee: editor),
            Caller(), Ct);

        Assert.Equal(ScoutingOutcome.Ok, result.Outcome);
        var entries = await EntriesAsync(db);
        Assert.All(entries, e => {
            Assert.Equal(ScoutingProspectStatus.Approved, e.Status);
            Assert.Equal(editor, e.Assignee);
        });
    }

    [Fact]
    public async Task No_status_and_no_assignee_change_is_invalid() {
        var (service, _) = await SetupAsync([1001]);

        var result = await service.UpdateEntriesAsync(BoardId, Request([1001]), Caller(), Ct);

        Assert.Equal(ScoutingOutcome.Invalid, result.Outcome);
    }

    [Fact]
    public async Task Forbidden_when_caller_cannot_edit() {
        var (service, _) = await SetupAsync([1001], callerRole: ScoutingTeamRole.Viewer);

        var result = await service.UpdateEntriesAsync(
            BoardId, Request([1001], ScoutingProspectStatus.Scouted), Caller(), Ct);

        Assert.Equal(ScoutingOutcome.Forbidden, result.Outcome);
    }

    [Fact]
    public async Task Not_found_when_none_of_the_selected_are_on_the_board() {
        var (service, _) = await SetupAsync([1001]);

        var result = await service.UpdateEntriesAsync(
            BoardId, Request([9998, 9999], ScoutingProspectStatus.Scouted), Caller(), Ct);

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
