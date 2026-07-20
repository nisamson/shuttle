namespace Shuttle.Shl.Api.Models.Portal.V1;

/// <summary>
/// A single point on a player's TPE timeline, as returned by the portal
/// <c>GET /tpeevents/timeline?pid={pid}</c> endpoint. Each entry records the player's cumulative
/// <see cref="TotalTpe"/> at the moment a TPE-earning (or -adjusting) task was applied.
/// </summary>
/// <param name="Name">The player's name at the time of the event.</param>
/// <param name="TaskDate">The UTC timestamp at which the task was applied.</param>
/// <param name="TotalTpe">The player's cumulative total TPE immediately after the task.</param>
public record TpeTimelineEntry(
    string Name,
    DateTime TaskDate,
    int TotalTpe
);
