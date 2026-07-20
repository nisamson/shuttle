namespace Shuttle.Models.Players;

/// <summary>
/// A single point on a player's TPE timeline, returned by <c>GET /players/{playerId}/tpe-timeline</c>.
/// Each point records the player's cumulative <see cref="TotalTpe"/> at the moment a TPE-earning (or
/// -adjusting) task was applied. Points are returned in chronological order by <see cref="TaskDate"/>.
/// </summary>
public record TpeTimelinePoint {
    /// <summary>The UTC timestamp at which the task was applied.</summary>
    public required DateTime TaskDate { get; init; }

    /// <summary>The player's cumulative total TPE immediately after the task.</summary>
    public required int TotalTpe { get; init; }
}
