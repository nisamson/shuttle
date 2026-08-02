using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Shuttle.EFCore;
using Shuttle.Models.Scouting;
using Entities = Shuttle.EFCore.Entities.Scouting;

namespace Shuttle.Api.Services.Scouting;

/// <summary>
/// Default <see cref="IScoutingService"/> implementation backed by <see cref="ShlDbContext"/>. The
/// class is split across partial files by aggregate: teams/members, boards/entries, and comments.
/// </summary>
public sealed partial class ScoutingService : IScoutingService {
    private readonly ShlDbContext db;
    private readonly IScoutingAccessService accessService;
    private readonly Users.IUserService users;
    private readonly TimeProvider timeProvider;

    public ScoutingService(
        ShlDbContext db,
        IScoutingAccessService accessService,
        Users.IUserService users,
        TimeProvider timeProvider) {
        this.db = db;
        this.accessService = accessService;
        this.users = users;
        this.timeProvider = timeProvider;
    }

    private DateTimeOffset Now => timeProvider.GetUtcNow();

    /// <summary>
    /// Saves pending changes, translating an optimistic-concurrency collision (a concurrent mutation
    /// touched the same team/board <c>RowVersion</c>) into <c>false</c> so callers can surface a
    /// <see cref="ScoutingOutcome.Conflict"/> and ask the client to retry. This is what protects the
    /// ownership and rank-contiguity invariants from racing requests.
    /// </summary>
    private async Task<bool> TrySaveChangesAsync(CancellationToken cancellationToken) {
        try {
            await db.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateConcurrencyException) {
            return false;
        }
    }

    /// <summary>
    /// Loads a board and resolves the caller's access to its owning team. Returns
    /// <c>(null, null)</c> when the board does not exist.
    /// </summary>
    private async Task<(Entities.ScoutingBoard? Board, ScoutingAccess? Access)> ResolveBoardAsync(
        Guid boardId,
        ClaimsPrincipal principal,
        bool tracked,
        CancellationToken cancellationToken) {
        var query = db.ScoutingBoards.AsQueryable();
        if (!tracked) {
            query = query.AsNoTracking();
        }

        var board = await query.FirstOrDefaultAsync(b => b.Id == boardId, cancellationToken);
        if (board is null) {
            return (null, null);
        }

        var access = await accessService.ResolveAsync(board.ScoutingTeamId, principal, cancellationToken);
        return (board, access);
    }

    private static ScoutingMember ToMemberDto(Entities.ScoutingTeamMember member) => new() {
        UserId = member.ShuttleUserId,
        Username = member.User.Username,
        Role = member.Role,
        CreatedAt = member.CreatedAt,
    };

    private static ScoutingBoardSummary ToBoardSummary(Entities.ScoutingBoard board, int entryCount) => new() {
        Id = board.Id,
        Name = board.Name,
        DraftSeason = board.DraftSeason,
        EntryCount = entryCount,
        UpdatedAt = board.UpdatedAt,
    };

    private static ScoutingComment ToCommentDto(Entities.ScoutingComment comment) => new() {
        Id = comment.Id,
        BoardId = comment.ScoutingBoardId,
        EntryId = comment.ScoutingBoardEntryId,
        AuthorUserId = comment.AuthorUserId,
        AuthorUsername = comment.Author.Username,
        Body = comment.Body,
        CreatedAt = comment.CreatedAt,
        EditedAt = comment.EditedAt,
    };
}
