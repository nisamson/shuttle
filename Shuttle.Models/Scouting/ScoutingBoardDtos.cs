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

/// <summary>A single ranked player on a board.</summary>
public record ScoutingBoardEntry {
    public required Guid Id { get; init; }

    /// <summary>The upstream integer id of the ranked player.</summary>
    public required int PlayerId { get; init; }

    /// <summary>The player's 1-based rank on the board.</summary>
    public required int Rank { get; init; }

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

/// <summary>Payload for removing several players from a board in a single request.</summary>
public record RemoveScoutingBoardEntriesRequest {
    [Required]
    public required IReadOnlyList<int> PlayerIds { get; init; }
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
