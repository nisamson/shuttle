using System.ComponentModel.DataAnnotations;

namespace Shuttle.Models.Scouting;

/// <summary>
/// A comment in a scouting thread. When <see cref="EntryId"/> is <c>null</c> the comment belongs to
/// the board-level thread; otherwise it belongs to that player entry's thread.
/// </summary>
public record ScoutingComment {
    public required Guid Id { get; init; }
    public required Guid BoardId { get; init; }

    /// <summary>The entry this comment is attached to, or <c>null</c> for the board-level thread.</summary>
    public Guid? EntryId { get; init; }

    /// <summary>The author's <c>ShuttleUser</c> id.</summary>
    public required Guid AuthorUserId { get; init; }

    /// <summary>The author's username.</summary>
    public required string AuthorUsername { get; init; }

    public required string Body { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>When the comment was last edited, or <c>null</c> if never edited.</summary>
    public DateTimeOffset? EditedAt { get; init; }
}

/// <summary>Payload for posting a new comment to a thread.</summary>
public record CreateScoutingCommentRequest {
    [Required]
    [StringLength(ScoutingLimits.CommentBodyMaxLength, MinimumLength = 1)]
    public required string Body { get; init; }
}

/// <summary>Payload for editing an existing comment's body.</summary>
public record UpdateScoutingCommentRequest {
    [Required]
    [StringLength(ScoutingLimits.CommentBodyMaxLength, MinimumLength = 1)]
    public required string Body { get; init; }
}
