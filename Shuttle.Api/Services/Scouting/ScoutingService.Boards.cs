using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Shuttle.Models.Scouting;
using Entities = Shuttle.EFCore.Entities.Scouting;

namespace Shuttle.Api.Services.Scouting;

public sealed partial class ScoutingService {
    public async Task<ScoutingResult<ScoutingBoardDetail>> CreateBoardAsync(
        Guid teamId,
        CreateScoutingBoardRequest request,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default) {
        var access = await accessService.ResolveAsync(teamId, principal, cancellationToken);
        if (access is null) {
            return ScoutingResult<ScoutingBoardDetail>.NotFound("Team not found.");
        }

        if (!access.CanEditBoards) {
            return ScoutingResult<ScoutingBoardDetail>.Forbidden("You do not have permission to edit this team's boards.");
        }

        var name = request.Name.Trim();
        if (name.Length is 0 or > ScoutingLimits.BoardNameMaxLength) {
            return ScoutingResult<ScoutingBoardDetail>.Invalid("Board name is required.");
        }

        var now = Now;
        var board = new Entities.ScoutingBoard {
            Id = Guid.CreateVersion7(),
            ScoutingTeamId = teamId,
            Name = name,
            DraftSeason = request.DraftSeason,
            CreatedAt = now,
            UpdatedAt = now,
        };
        db.ScoutingBoards.Add(board);
        await TouchTeamAsync(teamId, cancellationToken);
        if (!await TrySaveChangesAsync(cancellationToken)) {
            return ScoutingResult<ScoutingBoardDetail>.Conflict(
                "The team was changed by someone else; please reload and try again.");
        }

        return ScoutingResult<ScoutingBoardDetail>.Ok(await LoadBoardDetailAsync(board.Id, cancellationToken));
    }

    public async Task<ScoutingResult<ScoutingBoardDetail>> GetBoardAsync(
        Guid boardId,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default) {
        var (board, access) = await ResolveBoardAsync(boardId, principal, tracked: false, cancellationToken);
        if (board is null || access is null) {
            return ScoutingResult<ScoutingBoardDetail>.NotFound("Board not found.");
        }

        if (!access.CanView) {
            return ScoutingResult<ScoutingBoardDetail>.Forbidden("You are not a member of this team.");
        }

        return ScoutingResult<ScoutingBoardDetail>.Ok(await LoadBoardDetailAsync(boardId, cancellationToken));
    }

    public async Task<ScoutingResult<ScoutingBoardDetail>> UpdateBoardAsync(
        Guid boardId,
        UpdateScoutingBoardRequest request,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default) {
        var (board, access) = await ResolveBoardAsync(boardId, principal, tracked: true, cancellationToken);
        if (board is null || access is null) {
            return ScoutingResult<ScoutingBoardDetail>.NotFound("Board not found.");
        }

        if (!access.CanEditBoards) {
            return ScoutingResult<ScoutingBoardDetail>.Forbidden("You do not have permission to edit this board.");
        }

        var name = request.Name.Trim();
        if (name.Length is 0 or > ScoutingLimits.BoardNameMaxLength) {
            return ScoutingResult<ScoutingBoardDetail>.Invalid("Board name is required.");
        }

        board.Name = name;
        board.DraftSeason = request.DraftSeason;
        board.UpdatedAt = Now;
        if (!await TrySaveChangesAsync(cancellationToken)) {
            return ScoutingResult<ScoutingBoardDetail>.Conflict(
                "The board was changed by someone else; please reload and try again.");
        }

        return ScoutingResult<ScoutingBoardDetail>.Ok(await LoadBoardDetailAsync(boardId, cancellationToken));
    }

    public async Task<ScoutingResult> DeleteBoardAsync(
        Guid boardId,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default) {
        var (board, access) = await ResolveBoardAsync(boardId, principal, tracked: true, cancellationToken);
        if (board is null || access is null) {
            return ScoutingResult.NotFound("Board not found.");
        }

        if (!access.CanEditBoards) {
            return ScoutingResult.Forbidden("You do not have permission to delete this board.");
        }

        db.ScoutingBoards.Remove(board);
        await TouchTeamAsync(board.ScoutingTeamId, cancellationToken);
        if (!await TrySaveChangesAsync(cancellationToken)) {
            return ScoutingResult.Conflict(
                "The board was changed by someone else; please reload and try again.");
        }

        return ScoutingResult.Ok();
    }

    public async Task<ScoutingResult<ScoutingBoardEntry>> AddEntryAsync(
        Guid boardId,
        AddScoutingBoardEntryRequest request,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default) {
        var (board, access) = await ResolveBoardAsync(boardId, principal, tracked: true, cancellationToken);
        if (board is null || access is null) {
            return ScoutingResult<ScoutingBoardEntry>.NotFound("Board not found.");
        }

        if (!access.CanEditBoards) {
            return ScoutingResult<ScoutingBoardEntry>.Forbidden("You do not have permission to edit this board.");
        }

        var exists = await db.ScoutingBoardEntries
            .AsNoTracking()
            .AnyAsync(e => e.ScoutingBoardId == boardId && e.PlayerId == request.PlayerId, cancellationToken);
        if (exists) {
            return ScoutingResult<ScoutingBoardEntry>.Conflict("That player is already on this board.");
        }

        var maxRank = await db.ScoutingBoardEntries
            .Where(e => e.ScoutingBoardId == boardId)
            .Select(e => (int?)e.Rank)
            .MaxAsync(cancellationToken) ?? 0;

        var now = Now;
        var entry = new Entities.ScoutingBoardEntry {
            Id = Guid.CreateVersion7(),
            ScoutingBoardId = boardId,
            PlayerId = request.PlayerId,
            Rank = maxRank + 1,
            CreatedAt = now,
            UpdatedAt = now,
        };
        db.ScoutingBoardEntries.Add(entry);
        board.UpdatedAt = now;
        if (!await TrySaveChangesAsync(cancellationToken)) {
            return ScoutingResult<ScoutingBoardEntry>.Conflict(
                "The board was changed by someone else; please reload and try again.");
        }

        return ScoutingResult<ScoutingBoardEntry>.Ok(new ScoutingBoardEntry {
            Id = entry.Id,
            PlayerId = entry.PlayerId,
            Rank = entry.Rank,
            CommentCount = 0,
        });
    }

    public async Task<ScoutingResult> RemoveEntryAsync(
        Guid boardId,
        int playerId,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default) {
        var (board, access) = await ResolveBoardAsync(boardId, principal, tracked: true, cancellationToken);
        if (board is null || access is null) {
            return ScoutingResult.NotFound("Board not found.");
        }

        if (!access.CanEditBoards) {
            return ScoutingResult.Forbidden("You do not have permission to edit this board.");
        }

        var entries = await db.ScoutingBoardEntries
            .Where(e => e.ScoutingBoardId == boardId)
            .OrderBy(e => e.Rank)
            .ToListAsync(cancellationToken);

        var removed = entries.FirstOrDefault(e => e.PlayerId == playerId);
        if (removed is null) {
            return ScoutingResult.NotFound("That player is not on this board.");
        }

        // Delete the entry's comment thread explicitly (client-cascade relationship).
        var entryComments = await db.ScoutingComments
            .Where(c => c.ScoutingBoardEntryId == removed.Id)
            .ToListAsync(cancellationToken);
        db.ScoutingComments.RemoveRange(entryComments);

        db.ScoutingBoardEntries.Remove(removed);
        foreach (var entry in entries.Where(e => e.Rank > removed.Rank)) {
            entry.Rank--;
        }

        board.UpdatedAt = Now;
        if (!await TrySaveChangesAsync(cancellationToken)) {
            return ScoutingResult.Conflict(
                "The board was changed by someone else; please reload and try again.");
        }

        return ScoutingResult.Ok();
    }

    public async Task<ScoutingResult> RemoveEntriesAsync(
        Guid boardId,
        RemoveScoutingBoardEntriesRequest request,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default) {
        var (board, access) = await ResolveBoardAsync(boardId, principal, tracked: true, cancellationToken);
        if (board is null || access is null) {
            return ScoutingResult.NotFound("Board not found.");
        }

        if (!access.CanEditBoards) {
            return ScoutingResult.Forbidden("You do not have permission to edit this board.");
        }

        var playerIds = request.PlayerIds.Distinct().ToHashSet();
        if (playerIds.Count == 0) {
            return ScoutingResult.Invalid("No players were selected for removal.");
        }

        var entries = await db.ScoutingBoardEntries
            .Where(e => e.ScoutingBoardId == boardId)
            .OrderBy(e => e.Rank)
            .ToListAsync(cancellationToken);

        var removed = entries.Where(e => playerIds.Contains(e.PlayerId)).ToList();
        if (removed.Count == 0) {
            return ScoutingResult.NotFound("None of the selected players are on this board.");
        }

        var removedIds = removed.Select(e => e.Id).ToHashSet();

        // Delete the removed entries' comment threads explicitly (client-cascade relationship).
        var comments = await db.ScoutingComments
            .Where(c => c.ScoutingBoardEntryId != null && removedIds.Contains(c.ScoutingBoardEntryId!.Value))
            .ToListAsync(cancellationToken);
        db.ScoutingComments.RemoveRange(comments);
        db.ScoutingBoardEntries.RemoveRange(removed);

        // Compact the surviving ranks to 1..N in their existing order.
        var rank = 1;
        foreach (var entry in entries.Where(e => !removedIds.Contains(e.Id))) {
            entry.Rank = rank++;
        }

        board.UpdatedAt = Now;
        if (!await TrySaveChangesAsync(cancellationToken)) {
            return ScoutingResult.Conflict(
                "The board was changed by someone else; please reload and try again.");
        }

        return ScoutingResult.Ok();
    }

    public async Task<ScoutingResult> MoveEntryAsync(
        Guid boardId,
        MoveScoutingBoardEntryRequest request,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default) {
        var (board, access) = await ResolveBoardAsync(boardId, principal, tracked: true, cancellationToken);
        if (board is null || access is null) {
            return ScoutingResult.NotFound("Board not found.");
        }

        if (!access.CanEditBoards) {
            return ScoutingResult.Forbidden("You do not have permission to edit this board.");
        }

        var entries = await db.ScoutingBoardEntries
            .Where(e => e.ScoutingBoardId == boardId)
            .OrderBy(e => e.Rank)
            .ToListAsync(cancellationToken);

        var moved = entries.FirstOrDefault(e => e.PlayerId == request.PlayerId);
        if (moved is null) {
            return ScoutingResult.NotFound("That player is not on this board.");
        }

        if (moved.Rank != request.FromRank) {
            return ScoutingResult.Conflict(
                "The player's position has changed since you loaded the board; refresh and try again.");
        }

        if (request.ToRank < 1 || request.ToRank > entries.Count) {
            return ScoutingResult.Invalid($"Target rank must be between 1 and {entries.Count}.");
        }

        var from = moved.Rank;
        var to = request.ToRank;
        if (from != to) {
            if (to < from) {
                foreach (var entry in entries.Where(e => e.Rank >= to && e.Rank < from)) {
                    entry.Rank++;
                }
            } else {
                foreach (var entry in entries.Where(e => e.Rank > from && e.Rank <= to)) {
                    entry.Rank--;
                }
            }

            moved.Rank = to;
            moved.UpdatedAt = Now;
            board.UpdatedAt = Now;
            if (!await TrySaveChangesAsync(cancellationToken)) {
                return ScoutingResult.Conflict(
                    "The board was changed by someone else; please reload and try again.");
            }
        }

        return ScoutingResult.Ok();
    }

    private async Task<ScoutingBoardDetail> LoadBoardDetailAsync(Guid boardId, CancellationToken cancellationToken) {
        var board = await db.ScoutingBoards
            .AsNoTracking()
            .Where(b => b.Id == boardId)
            .Select(b => new {
                b.Id,
                b.ScoutingTeamId,
                b.Name,
                b.DraftSeason,
                b.CreatedAt,
                b.UpdatedAt,
            })
            .FirstAsync(cancellationToken);

        var entries = await db.ScoutingBoardEntries
            .AsNoTracking()
            .Where(e => e.ScoutingBoardId == boardId)
            .OrderBy(e => e.Rank)
            .Select(e => new ScoutingBoardEntry {
                Id = e.Id,
                PlayerId = e.PlayerId,
                Rank = e.Rank,
                CommentCount = e.Comments.Count,
            })
            .ToListAsync(cancellationToken);

        return new ScoutingBoardDetail {
            Id = board.Id,
            ScoutingTeamId = board.ScoutingTeamId,
            Name = board.Name,
            DraftSeason = board.DraftSeason,
            CreatedAt = board.CreatedAt,
            UpdatedAt = board.UpdatedAt,
            Entries = entries,
        };
    }
}
