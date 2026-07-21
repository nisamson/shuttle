using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Shuttle.Models.Scouting;
using Entities = Shuttle.EFCore.Entities.Scouting;

namespace Shuttle.Api.Services.Scouting;

public sealed partial class ScoutingService {
    public async Task<ScoutingResult<IReadOnlyList<ScoutingComment>>> GetBoardCommentsAsync(
        Guid boardId,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default) {
        var (board, access) = await ResolveBoardAsync(boardId, principal, tracked: false, cancellationToken);
        if (board is null || access is null) {
            return ScoutingResult<IReadOnlyList<ScoutingComment>>.NotFound("Board not found.");
        }

        if (!access.CanView) {
            return ScoutingResult<IReadOnlyList<ScoutingComment>>.Forbidden("You are not a member of this team.");
        }

        var comments = await LoadThreadAsync(boardId, entryId: null, cancellationToken);
        return ScoutingResult<IReadOnlyList<ScoutingComment>>.Ok(comments);
    }

    public async Task<ScoutingResult<IReadOnlyList<ScoutingComment>>> GetEntryCommentsAsync(
        Guid boardId,
        int playerId,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default) {
        var (board, access) = await ResolveBoardAsync(boardId, principal, tracked: false, cancellationToken);
        if (board is null || access is null) {
            return ScoutingResult<IReadOnlyList<ScoutingComment>>.NotFound("Board not found.");
        }

        if (!access.CanView) {
            return ScoutingResult<IReadOnlyList<ScoutingComment>>.Forbidden("You are not a member of this team.");
        }

        var entryId = await ResolveEntryIdAsync(boardId, playerId, cancellationToken);
        if (entryId is null) {
            return ScoutingResult<IReadOnlyList<ScoutingComment>>.NotFound("That player is not on this board.");
        }

        var comments = await LoadThreadAsync(boardId, entryId, cancellationToken);
        return ScoutingResult<IReadOnlyList<ScoutingComment>>.Ok(comments);
    }

    public async Task<ScoutingResult<ScoutingComment>> AddBoardCommentAsync(
        Guid boardId,
        CreateScoutingCommentRequest request,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default) {
        var (board, access) = await ResolveBoardAsync(boardId, principal, tracked: false, cancellationToken);
        if (board is null || access is null) {
            return ScoutingResult<ScoutingComment>.NotFound("Board not found.");
        }

        if (!access.CanComment) {
            return ScoutingResult<ScoutingComment>.Forbidden("You do not have permission to comment on this team's boards.");
        }

        return await AddCommentAsync(boardId, entryId: null, request.Body, access, cancellationToken);
    }

    public async Task<ScoutingResult<ScoutingComment>> AddEntryCommentAsync(
        Guid boardId,
        int playerId,
        CreateScoutingCommentRequest request,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default) {
        var (board, access) = await ResolveBoardAsync(boardId, principal, tracked: false, cancellationToken);
        if (board is null || access is null) {
            return ScoutingResult<ScoutingComment>.NotFound("Board not found.");
        }

        if (!access.CanComment) {
            return ScoutingResult<ScoutingComment>.Forbidden("You do not have permission to comment on this team's boards.");
        }

        var entryId = await ResolveEntryIdAsync(boardId, playerId, cancellationToken);
        if (entryId is null) {
            return ScoutingResult<ScoutingComment>.NotFound("That player is not on this board.");
        }

        return await AddCommentAsync(boardId, entryId, request.Body, access, cancellationToken);
    }

    public async Task<ScoutingResult<ScoutingComment>> EditCommentAsync(
        Guid commentId,
        UpdateScoutingCommentRequest request,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default) {
        var comment = await db.ScoutingComments
            .Include(c => c.Author)
            .FirstOrDefaultAsync(c => c.Id == commentId, cancellationToken);
        if (comment is null) {
            return ScoutingResult<ScoutingComment>.NotFound("Comment not found.");
        }

        var access = await ResolveAccessForBoardTeamAsync(comment.ScoutingBoardId, principal, cancellationToken);
        if (access is null) {
            return ScoutingResult<ScoutingComment>.NotFound("Comment not found.");
        }

        // Only the author may edit their own comment, and only while they still hold posting rights
        // (a demoted Viewer or a former member must not be able to edit old comments). Moderators can
        // delete but not edit others' comments.
        if (comment.AuthorUserId != access.User.Id) {
            return ScoutingResult<ScoutingComment>.Forbidden("You can only edit your own comments.");
        }

        if (!access.CanComment) {
            return ScoutingResult<ScoutingComment>.Forbidden(
                "You no longer have permission to post on this team's boards.");
        }

        var body = request.Body.Trim();
        if (body.Length is 0 or > ScoutingLimits.CommentBodyMaxLength) {
            return ScoutingResult<ScoutingComment>.Invalid("Comment body is required.");
        }

        comment.Body = body;
        comment.EditedAt = Now;
        await db.SaveChangesAsync(cancellationToken);
        return ScoutingResult<ScoutingComment>.Ok(ToCommentDto(comment));
    }

    public async Task<ScoutingResult> DeleteCommentAsync(
        Guid commentId,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default) {
        var comment = await db.ScoutingComments
            .FirstOrDefaultAsync(c => c.Id == commentId, cancellationToken);
        if (comment is null) {
            return ScoutingResult.NotFound("Comment not found.");
        }

        var access = await ResolveAccessForBoardTeamAsync(comment.ScoutingBoardId, principal, cancellationToken);
        if (access is null) {
            return ScoutingResult.NotFound("Comment not found.");
        }

        if (comment.AuthorUserId != access.User.Id && !access.CanModerateComments) {
            return ScoutingResult.Forbidden("You can only delete your own comments unless you are an owner or admin.");
        }

        db.ScoutingComments.Remove(comment);
        await db.SaveChangesAsync(cancellationToken);
        return ScoutingResult.Ok();
    }

    private async Task<ScoutingResult<ScoutingComment>> AddCommentAsync(
        Guid boardId,
        Guid? entryId,
        string body,
        ScoutingAccess access,
        CancellationToken cancellationToken) {
        var trimmed = body.Trim();
        if (trimmed.Length is 0 or > ScoutingLimits.CommentBodyMaxLength) {
            return ScoutingResult<ScoutingComment>.Invalid("Comment body is required.");
        }

        var comment = new Entities.ScoutingComment {
            Id = Guid.CreateVersion7(),
            ScoutingBoardId = boardId,
            ScoutingBoardEntryId = entryId,
            AuthorUserId = access.User.Id,
            Body = trimmed,
            CreatedAt = Now,
        };
        db.ScoutingComments.Add(comment);
        await db.SaveChangesAsync(cancellationToken);

        comment.Author = access.User;
        return ScoutingResult<ScoutingComment>.Ok(ToCommentDto(comment));
    }

    private async Task<IReadOnlyList<ScoutingComment>> LoadThreadAsync(
        Guid boardId,
        Guid? entryId,
        CancellationToken cancellationToken) {
        return await db.ScoutingComments
            .AsNoTracking()
            .Where(c => c.ScoutingBoardId == boardId && c.ScoutingBoardEntryId == entryId)
            .OrderBy(c => c.CreatedAt)
            .Select(c => new ScoutingComment {
                Id = c.Id,
                BoardId = c.ScoutingBoardId,
                EntryId = c.ScoutingBoardEntryId,
                AuthorUserId = c.AuthorUserId,
                AuthorUsername = c.Author.Username,
                Body = c.Body,
                CreatedAt = c.CreatedAt,
                EditedAt = c.EditedAt,
            })
            .ToListAsync(cancellationToken);
    }

    private async Task<Guid?> ResolveEntryIdAsync(Guid boardId, int playerId, CancellationToken cancellationToken) {
        return await db.ScoutingBoardEntries
            .AsNoTracking()
            .Where(e => e.ScoutingBoardId == boardId && e.PlayerId == playerId)
            .Select(e => (Guid?)e.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<ScoutingAccess?> ResolveAccessForBoardTeamAsync(
        Guid boardId,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken) {
        var teamId = await db.ScoutingBoards
            .AsNoTracking()
            .Where(b => b.Id == boardId)
            .Select(b => (Guid?)b.ScoutingTeamId)
            .FirstOrDefaultAsync(cancellationToken);
        if (teamId is null) {
            return null;
        }

        return await accessService.ResolveAsync(teamId.Value, principal, cancellationToken);
    }
}
