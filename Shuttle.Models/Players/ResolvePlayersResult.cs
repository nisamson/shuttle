using Shuttle.Shl.Api.Models.Common;
using Shuttle.Shl.Api.Models.Portal.V1;

namespace Shuttle.Models.Players;

/// <summary>
/// Result of a batch player-resolution request (<c>QUERY /players/resolve</c>). Resolved players are
/// de-duplicated preserving first-seen order (requested ids first, then name-resolved players).
/// </summary>
public sealed record ResolvePlayersResult {
    /// <summary>The players that were resolved from the requested ids and names.</summary>
    public required IReadOnlyList<ResolvedPlayer> Resolved { get; init; }

    /// <summary>The requested ids (as strings) and names that matched no player.</summary>
    public required IReadOnlyList<string> NotFound { get; init; }

    /// <summary>The requested names that matched more than one player and so could not be resolved.</summary>
    public required IReadOnlyList<string> Ambiguous { get; init; }
}

/// <summary>
/// A slim player summary returned by <c>QUERY /players/resolve</c> — enough to render a resolved
/// preview (name, position, draft season, total TPE) and to feed a follow-up bulk-add by id.
/// </summary>
public sealed record ResolvedPlayer {
    public required int PlayerId { get; init; }
    public required string Name { get; init; }
    public required string Username { get; init; }
    public required PlayerStatus Status { get; init; }
    public required PlayerPosition Position { get; init; }
    public int? DraftSeason { get; init; }
    public required int TotalTpe { get; init; }
}
