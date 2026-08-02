using System.ComponentModel.DataAnnotations;

namespace Shuttle.Models.Scouting;

/// <summary>A board in a team's board list, without its full ranked entries.</summary>
public record ScoutingBoardSummary {
    public required Guid Id { get; init; }
    public required string Name { get; init; }

    /// <summary>Optional draft season the board targets.</summary>
    public int? DraftSeason { get; init; }

    /// <summary>Number of ranked players on the board.</summary>
    public required int EntryCount { get; init; }

    public required DateTimeOffset UpdatedAt { get; init; }
}

/// <summary>The full detail of a board: its metadata and its ranked entries in rank order.</summary>
public record ScoutingBoardDetail {
    public required Guid Id { get; init; }
    public required Guid ScoutingTeamId { get; init; }
    public required string Name { get; init; }
    public int? DraftSeason { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }

    public required IReadOnlyList<ScoutingBoardEntry> Entries { get; init; }
}

/// <summary>
/// The scouting lifecycle state of a prospect on a board. Values are mutually exclusive and advance
/// as a member scouts and then decides on the player.
/// </summary>
public enum ScoutingProspectStatus {
    /// <summary>Not yet scouted; the default state for a newly added prospect.</summary>
    Pending = 0,

    /// <summary>Has been scouted, but no accept/reject decision has been made yet.</summary>
    Scouted = 1,

    /// <summary>Scouted and approved for the team's draft plans.</summary>
    Approved = 2,

    /// <summary>Scouted and rejected. Rejected prospects are pulled out of the active rank sequence.</summary>
    Rejected = 3
}

/// <summary>A single player on a board, with its scouting status and (optional) assignee.</summary>
public record ScoutingBoardEntry {
    public required Guid Id { get; init; }

    /// <summary>The upstream integer id of the ranked player.</summary>
    public required int PlayerId { get; init; }

    /// <summary>
    /// The player's 1-based rank among the board's active (non-rejected) prospects, or <c>0</c> when
    /// the prospect is <see cref="ScoutingProspectStatus.Rejected"/> and therefore unranked.
    /// </summary>
    public required int Rank { get; init; }

    /// <summary>The prospect's scouting status.</summary>
    public required ScoutingProspectStatus Status { get; init; }

    /// <summary>The <c>ShuttleUser</c> id this prospect is assigned to scout, or <c>null</c> if unassigned.</summary>
    public Guid? AssignedToUserId { get; init; }

    /// <summary>The username of the assignee, or <c>null</c> if unassigned.</summary>
    public string? AssignedToUsername { get; init; }

    /// <summary>Number of comments in this entry's thread.</summary>
    public required int CommentCount { get; init; }
}

/// <summary>Payload for creating a board on a team.</summary>
public record CreateScoutingBoardRequest {
    [Required]
    [StringLength(ScoutingLimits.BoardNameMaxLength, MinimumLength = 1)]
    public required string Name { get; init; }

    public int? DraftSeason { get; init; }
}

/// <summary>Payload for updating a board's name and/or target draft season.</summary>
public record UpdateScoutingBoardRequest {
    [Required]
    [StringLength(ScoutingLimits.BoardNameMaxLength, MinimumLength = 1)]
    public required string Name { get; init; }

    public int? DraftSeason { get; init; }
}

/// <summary>Payload for adding a player to a board; the player is appended at the next rank.</summary>
public record AddScoutingBoardEntryRequest {
    public required int PlayerId { get; init; }
}

/// <summary>
/// Payload for adding several players to a board in a single request. Players may be identified by
/// upstream <see cref="PlayerIds"/> and/or by player <see cref="Names"/>; at least one of the two
/// must be non-empty. Only players that already exist in the database are added — unknown ids and
/// names are reported back as skipped rather than failing the request. A name that matches more than
/// one player is ambiguous and causes the whole request to be rejected.
/// </summary>
public record AddScoutingBoardEntriesRequest {
    /// <summary>Upstream integer ids of players to add. Ids not present in the database are skipped.</summary>
    public IReadOnlyList<int>? PlayerIds { get; init; }

    /// <summary>
    /// Player names to resolve and add. Matching is case-insensitive on the trimmed name; a name that
    /// resolves to more than one player is ambiguous and rejects the request.
    /// </summary>
    public IReadOnlyList<string>? Names { get; init; }
}

/// <summary>
/// The outcome of a bulk add: the refreshed board plus a breakdown of what happened to each
/// requested player. On the ambiguous-name rejection path the board is unchanged and
/// <see cref="Ambiguous"/> lists the offending names.
/// </summary>
public record AddScoutingBoardEntriesResult {
    /// <summary>The board after the add, with its entries in rank order.</summary>
    public required ScoutingBoardDetail Board { get; init; }

    /// <summary>Player ids that were newly added to the board, in the order they were appended.</summary>
    public required IReadOnlyList<int> Added { get; init; }

    /// <summary>Requested players that were already on the board and therefore not re-added.</summary>
    public required IReadOnlyList<int> AlreadyOnBoard { get; init; }

    /// <summary>Requested ids and names that did not match any player in the database.</summary>
    public required IReadOnlyList<string> NotFound { get; init; }
}

/// <summary>Payload for removing several players from a board in a single request.</summary>
public record RemoveScoutingBoardEntriesRequest {
    [Required]
    public required IReadOnlyList<int> PlayerIds { get; init; }
}

/// <summary>
/// Payload for applying a status and/or assignee change to several prospects on a board in one
/// request. At least one of <see cref="Status"/> or <see cref="ChangeAssignee"/> must be set;
/// changing status recomputes the active rank sequence (rejected prospects are unranked and
/// newly-restored prospects are appended to the end).
/// </summary>
public record BulkUpdateScoutingBoardEntriesRequest {
    /// <summary>Upstream integer ids of the prospects to update.</summary>
    [Required]
    public required IReadOnlyList<int> PlayerIds { get; init; }

    /// <summary>The status to apply to every selected prospect, or <c>null</c> to leave statuses unchanged.</summary>
    public ScoutingProspectStatus? Status { get; init; }

    /// <summary>
    /// Whether to change the assignee of every selected prospect. When <c>true</c>,
    /// <see cref="AssignedToUserId"/> is applied (with <c>null</c> meaning "unassign"); when
    /// <c>false</c> assignees are left as-is. This flag disambiguates "clear the assignee" from
    /// "leave the assignee unchanged".
    /// </summary>
    public bool ChangeAssignee { get; init; }

    /// <summary>
    /// The <c>ShuttleUser</c> id to assign every selected prospect to (or <c>null</c> to unassign),
    /// applied only when <see cref="ChangeAssignee"/> is <c>true</c>. The assignee must be a team
    /// member with edit access (Editor or Owner).
    /// </summary>
    public Guid? AssignedToUserId { get; init; }
}

/// <summary>
/// Payload for moving a player to a new rank. <see cref="FromRank"/> is an optimistic-concurrency
/// guard: if it does not match the player's current rank the move is rejected with a conflict, so a
/// stale client cannot silently reorder based on outdated positions.
/// </summary>
public record MoveScoutingBoardEntryRequest {
    public required int PlayerId { get; init; }
    public required int FromRank { get; init; }
    public required int ToRank { get; init; }
}

/// <summary>
/// Payload for editing a single prospect's status, assignment, and (for active prospects) rank in
/// one atomic update. Setting <see cref="Status"/> to <see cref="ScoutingProspectStatus.Rejected"/>
/// unranks the prospect and compacts the remaining active ranks; leaving <c>Rejected</c> re-ranks it
/// at the end of the active list unless <see cref="Rank"/> requests a specific position.
/// </summary>
public record UpdateScoutingBoardEntryRequest {
    /// <summary>The prospect's new scouting status.</summary>
    public required ScoutingProspectStatus Status { get; init; }

    /// <summary>
    /// The <c>ShuttleUser</c> id to assign the prospect to, or <c>null</c> to leave it unassigned.
    /// The assignee must be a team member with edit access (Editor or Owner).
    /// </summary>
    public Guid? AssignedToUserId { get; init; }

    /// <summary>
    /// The desired 1-based rank among the board's active prospects. Ignored when <see cref="Status"/>
    /// is <see cref="ScoutingProspectStatus.Rejected"/> (rejected prospects are unranked). When
    /// <c>null</c> the prospect keeps its current position (or is appended when leaving rejection).
    /// </summary>
    public int? Rank { get; init; }
}
