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
            Status = entry.Status,
            AssignedToUserId = null,
            AssignedToUsername = null,
            CommentCount = 0,
        });
    }

    // Projection used to resolve player names to ids without pulling whole entities.
    private sealed record NameMatch(int PlayerId, string LoweredName);

    public async Task<ScoutingResult<AddScoutingBoardEntriesResult>> AddEntriesAsync(
        Guid boardId,
        AddScoutingBoardEntriesRequest request,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default) {
        var (board, access) = await ResolveBoardAsync(boardId, principal, tracked: true, cancellationToken);
        if (board is null || access is null) {
            return ScoutingResult<AddScoutingBoardEntriesResult>.NotFound("Board not found.");
        }

        if (!access.CanEditBoards) {
            return ScoutingResult<AddScoutingBoardEntriesResult>.Forbidden(
                "You do not have permission to edit this board.");
        }

        var requestedIds = (request.PlayerIds ?? []).ToList();
        var requestedNames = (request.Names ?? [])
            .Select(n => n?.Trim() ?? string.Empty)
            .Where(n => n.Length > 0)
            .ToList();

        if (requestedIds.Count == 0 && requestedNames.Count == 0) {
            return ScoutingResult<AddScoutingBoardEntriesResult>.Invalid(
                "Provide at least one player id or name to add.");
        }

        var notFound = new List<string>();

        // Resolve names -> player ids. Matching is case-insensitive on the trimmed name; any name that
        // matches more than one player is ambiguous and rejects the whole request.
        var loweredNames = requestedNames.Select(n => n.ToLowerInvariant()).Distinct().ToList();
        var nameMatches = new List<NameMatch>();
        if (loweredNames.Count > 0) {
            nameMatches = await db.PlayerInformation
                .Where(p => loweredNames.Contains(p.Name.ToLower()))
                .Select(p => new NameMatch(p.PlayerId, p.Name.ToLower()))
                .ToListAsync(cancellationToken);
        }

        var byLoweredName = nameMatches
            .GroupBy(m => m.LoweredName)
            .ToDictionary(g => g.Key, g => g.Select(m => m.PlayerId).Distinct().ToList());

        var ambiguous = new List<string>();
        var resolvedFromNames = new List<int>();
        var seenLoweredNames = new HashSet<string>();
        foreach (var name in requestedNames) {
            var lowered = name.ToLowerInvariant();
            if (!seenLoweredNames.Add(lowered)) {
                continue; // duplicate name within the request
            }

            if (!byLoweredName.TryGetValue(lowered, out var ids) || ids.Count == 0) {
                notFound.Add(name);
            } else if (ids.Count > 1) {
                ambiguous.Add(name);
            } else {
                resolvedFromNames.Add(ids[0]);
            }
        }

        if (ambiguous.Count > 0) {
            return ScoutingResult<AddScoutingBoardEntriesResult>.Invalid(
                $"These names match more than one player; add them by id instead: {string.Join(", ", ambiguous)}.");
        }

        // Keep only ids that actually exist in the database; report the rest as not found.
        var idSet = requestedIds.Distinct().ToList();
        var existingIds = idSet.Count == 0
            ? new HashSet<int>()
            : (await db.PlayerInformation
                .Where(p => idSet.Contains(p.PlayerId))
                .Select(p => p.PlayerId)
                .ToListAsync(cancellationToken))
            .ToHashSet();

        // Build the ordered, de-duplicated candidate list: requested ids first (in request order),
        // then name-resolved ids, preserving first-seen order.
        var ordered = new List<int>();
        var seenIds = new HashSet<int>();
        foreach (var id in requestedIds) {
            if (!seenIds.Add(id)) {
                continue;
            }

            if (existingIds.Contains(id)) {
                ordered.Add(id);
            } else {
                notFound.Add(id.ToString());
            }
        }

        foreach (var id in resolvedFromNames) {
            if (seenIds.Add(id)) {
                ordered.Add(id);
            }
        }

        var onBoard = (await db.ScoutingBoardEntries
                .Where(e => e.ScoutingBoardId == boardId)
                .Select(e => e.PlayerId)
                .ToListAsync(cancellationToken))
            .ToHashSet();

        var alreadyOnBoard = new List<int>();
        var toAdd = new List<int>();
        foreach (var id in ordered) {
            if (onBoard.Contains(id)) {
                alreadyOnBoard.Add(id);
            } else {
                toAdd.Add(id);
            }
        }

        if (toAdd.Count > 0) {
            var maxRank = await db.ScoutingBoardEntries
                .Where(e => e.ScoutingBoardId == boardId)
                .Select(e => (int?)e.Rank)
                .MaxAsync(cancellationToken) ?? 0;

            var now = Now;
            var rank = maxRank;
            foreach (var id in toAdd) {
                rank++;
                db.ScoutingBoardEntries.Add(new Entities.ScoutingBoardEntry {
                    Id = Guid.CreateVersion7(),
                    ScoutingBoardId = boardId,
                    PlayerId = id,
                    Rank = rank,
                    CreatedAt = now,
                    UpdatedAt = now,
                });
            }

            board.UpdatedAt = now;
            if (!await TrySaveChangesAsync(cancellationToken)) {
                return ScoutingResult<AddScoutingBoardEntriesResult>.Conflict(
                    "The board was changed by someone else; please reload and try again.");
            }
        }

        var detail = await LoadBoardDetailAsync(boardId, cancellationToken);
        return ScoutingResult<AddScoutingBoardEntriesResult>.Ok(new AddScoutingBoardEntriesResult {
            Board = detail,
            Added = toAdd,
            AlreadyOnBoard = alreadyOnBoard,
            NotFound = notFound,
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
        if (removed.Status != ScoutingProspectStatus.Rejected) {
            foreach (var entry in entries.Where(e =>
                         e.Status != ScoutingProspectStatus.Rejected && e.Rank > removed.Rank)) {
                entry.Rank--;
            }
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

        // Compact the surviving active ranks to 1..N in their existing order; rejected survivors stay unranked.
        var rank = 1;
        foreach (var entry in entries.Where(e =>
                     !removedIds.Contains(e.Id) && e.Status != ScoutingProspectStatus.Rejected)) {
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

        if (moved.Status == ScoutingProspectStatus.Rejected) {
            return ScoutingResult.Invalid("Rejected prospects are unranked; restore the prospect before moving it.");
        }

        if (moved.Rank != request.FromRank) {
            return ScoutingResult.Conflict(
                "The player's position has changed since you loaded the board; refresh and try again.");
        }

        // Only active (non-rejected) prospects participate in the contiguous rank sequence.
        var active = entries.Where(e => e.Status != ScoutingProspectStatus.Rejected).ToList();
        if (request.ToRank < 1 || request.ToRank > active.Count) {
            return ScoutingResult.Invalid($"Target rank must be between 1 and {active.Count}.");
        }

        var from = moved.Rank;
        var to = request.ToRank;
        if (from != to) {
            if (to < from) {
                foreach (var entry in active.Where(e => e.Rank >= to && e.Rank < from)) {
                    entry.Rank++;
                }
            } else {
                foreach (var entry in active.Where(e => e.Rank > from && e.Rank <= to)) {
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

    public async Task<ScoutingResult<ScoutingBoardEntry>> UpdateEntryAsync(
        Guid boardId,
        int playerId,
        UpdateScoutingBoardEntryRequest request,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default) {
        var (board, access) = await ResolveBoardAsync(boardId, principal, tracked: true, cancellationToken);
        if (board is null || access is null) {
            return ScoutingResult<ScoutingBoardEntry>.NotFound("Board not found.");
        }

        if (!access.CanEditBoards) {
            return ScoutingResult<ScoutingBoardEntry>.Forbidden("You do not have permission to edit this board.");
        }

        if (!Enum.IsDefined(request.Status)) {
            return ScoutingResult<ScoutingBoardEntry>.Invalid("Unknown prospect status.");
        }

        var entries = await db.ScoutingBoardEntries
            .Where(e => e.ScoutingBoardId == boardId)
            .OrderBy(e => e.Rank)
            .ToListAsync(cancellationToken);

        var entry = entries.FirstOrDefault(e => e.PlayerId == playerId);
        if (entry is null) {
            return ScoutingResult<ScoutingBoardEntry>.NotFound("That player is not on this board.");
        }

        // An assignee must be a team member with edit access (Editor or Owner).
        if (request.AssignedToUserId is { } assigneeId) {
            var assigneeRole = await db.ScoutingTeamMembers
                .AsNoTracking()
                .Where(m => m.ScoutingTeamId == board.ScoutingTeamId && m.ShuttleUserId == assigneeId)
                .Select(m => (ScoutingTeamRole?)m.Role)
                .FirstOrDefaultAsync(cancellationToken);
            if (assigneeRole is null) {
                return ScoutingResult<ScoutingBoardEntry>.Invalid("The assignee is not a member of this team.");
            }

            if (!assigneeRole.Value.CanEditBoards()) {
                return ScoutingResult<ScoutingBoardEntry>.Invalid(
                    "The assignee must have edit access (Editor or Owner).");
            }
        }

        var wasRejected = entry.Status == ScoutingProspectStatus.Rejected;
        var isRejected = request.Status == ScoutingProspectStatus.Rejected;
        var active = entries.Where(e => e.Status != ScoutingProspectStatus.Rejected).ToList();

        if (!wasRejected && isRejected) {
            // Pull the prospect out of the active rank sequence and close the gap it leaves behind.
            var vacatedRank = entry.Rank;
            entry.Rank = 0;
            foreach (var other in active.Where(e => e.Id != entry.Id && e.Rank > vacatedRank)) {
                other.Rank--;
            }
        } else if (wasRejected && !isRejected) {
            // Restore the prospect to the active sequence: append at the end, then honour any requested rank.
            var maxActiveRank = active.Count == 0 ? 0 : active.Max(e => e.Rank);
            entry.Rank = maxActiveRank + 1;
            active.Add(entry);
            if (request.Rank is { } target) {
                MoveWithinActive(active, entry, target);
            }
        } else if (!isRejected && request.Rank is { } target) {
            MoveWithinActive(active, entry, target);
        }

        entry.Status = request.Status;

        var now = Now;
        if (entry.AssignedToUserId != request.AssignedToUserId) {
            entry.AssignedToUserId = request.AssignedToUserId;
            entry.AssignedAt = request.AssignedToUserId is null ? null : now;
        }

        entry.UpdatedAt = now;
        board.UpdatedAt = now;
        if (!await TrySaveChangesAsync(cancellationToken)) {
            return ScoutingResult<ScoutingBoardEntry>.Conflict(
                "The board was changed by someone else; please reload and try again.");
        }

        var dto = await db.ScoutingBoardEntries
            .AsNoTracking()
            .Where(e => e.Id == entry.Id)
            .Select(e => new ScoutingBoardEntry {
                Id = e.Id,
                PlayerId = e.PlayerId,
                Rank = e.Rank,
                Status = e.Status,
                AssignedToUserId = e.AssignedToUserId,
                AssignedToUsername = e.AssignedTo != null ? e.AssignedTo.Username : null,
                CommentCount = e.Comments.Count,
            })
            .FirstAsync(cancellationToken);

        return ScoutingResult<ScoutingBoardEntry>.Ok(dto);
    }

    public async Task<ScoutingResult<ScoutingBoardDetail>> UpdateEntriesAsync(
        Guid boardId,
        BulkUpdateScoutingBoardEntriesRequest request,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default) {
        var (board, access) = await ResolveBoardAsync(boardId, principal, tracked: true, cancellationToken);
        if (board is null || access is null) {
            return ScoutingResult<ScoutingBoardDetail>.NotFound("Board not found.");
        }

        if (!access.CanEditBoards) {
            return ScoutingResult<ScoutingBoardDetail>.Forbidden("You do not have permission to edit this board.");
        }

        if (request.Status is null && !request.ChangeAssignee) {
            return ScoutingResult<ScoutingBoardDetail>.Invalid("Specify a status and/or an assignee to apply.");
        }

        if (request.Status is { } requested && !Enum.IsDefined(requested)) {
            return ScoutingResult<ScoutingBoardDetail>.Invalid("Unknown prospect status.");
        }

        var playerIds = request.PlayerIds.Distinct().ToHashSet();
        if (playerIds.Count == 0) {
            return ScoutingResult<ScoutingBoardDetail>.Invalid("No players were selected.");
        }

        // Validate the assignee once; it is shared across every selected prospect.
        if (request.ChangeAssignee && request.AssignedToUserId is { } assigneeId) {
            var assigneeRole = await db.ScoutingTeamMembers
                .AsNoTracking()
                .Where(m => m.ScoutingTeamId == board.ScoutingTeamId && m.ShuttleUserId == assigneeId)
                .Select(m => (ScoutingTeamRole?)m.Role)
                .FirstOrDefaultAsync(cancellationToken);
            if (assigneeRole is null) {
                return ScoutingResult<ScoutingBoardDetail>.Invalid("The assignee is not a member of this team.");
            }

            if (!assigneeRole.Value.CanEditBoards()) {
                return ScoutingResult<ScoutingBoardDetail>.Invalid(
                    "The assignee must have edit access (Editor or Owner).");
            }
        }

        var entries = await db.ScoutingBoardEntries
            .Where(e => e.ScoutingBoardId == boardId)
            .OrderBy(e => e.Rank)
            .ToListAsync(cancellationToken);

        var selected = entries.Where(e => playerIds.Contains(e.PlayerId)).ToList();
        if (selected.Count == 0) {
            return ScoutingResult<ScoutingBoardDetail>.NotFound("None of the selected players are on this board.");
        }

        var now = Now;

        if (request.Status is { } newStatus) {
            foreach (var entry in selected) {
                entry.Status = newStatus;
                entry.UpdatedAt = now;
            }

            // Recompute the active rank sequence: keep the existing order of prospects that were
            // already ranked, append newly-restored ones (Rank 0) at the end, and unrank the rejected.
            var active = entries
                .Where(e => e.Status != ScoutingProspectStatus.Rejected)
                .OrderBy(e => e.Rank > 0 ? 0 : 1)
                .ThenBy(e => e.Rank)
                .ThenBy(e => e.PlayerId)
                .ToList();
            var rank = 1;
            foreach (var entry in active) {
                entry.Rank = rank++;
            }

            foreach (var entry in entries.Where(e => e.Status == ScoutingProspectStatus.Rejected)) {
                entry.Rank = 0;
            }
        }

        if (request.ChangeAssignee) {
            foreach (var entry in selected.Where(e => e.AssignedToUserId != request.AssignedToUserId)) {
                entry.AssignedToUserId = request.AssignedToUserId;
                entry.AssignedAt = request.AssignedToUserId is null ? null : now;
                entry.UpdatedAt = now;
            }
        }

        board.UpdatedAt = now;
        if (!await TrySaveChangesAsync(cancellationToken)) {
            return ScoutingResult<ScoutingBoardDetail>.Conflict(
                "The board was changed by someone else; please reload and try again.");
        }

        return ScoutingResult<ScoutingBoardDetail>.Ok(await LoadBoardDetailAsync(boardId, cancellationToken));
    }

    // Reorders a prospect within the active (non-rejected) sequence, clamping the target into range
    // and shifting the entries between the old and new positions so ranks stay contiguous.
    private static void MoveWithinActive(
        List<Entities.ScoutingBoardEntry> active,
        Entities.ScoutingBoardEntry moved,
        int target) {
        target = Math.Clamp(target, 1, active.Count);
        var from = moved.Rank;
        if (from == target) {
            return;
        }

        if (target < from) {
            foreach (var entry in active.Where(e => e.Id != moved.Id && e.Rank >= target && e.Rank < from)) {
                entry.Rank++;
            }
        } else {
            foreach (var entry in active.Where(e => e.Id != moved.Id && e.Rank > from && e.Rank <= target)) {
                entry.Rank--;
            }
        }

        moved.Rank = target;
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
                Status = e.Status,
                AssignedToUserId = e.AssignedToUserId,
                AssignedToUsername = e.AssignedTo != null ? e.AssignedTo.Username : null,
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
