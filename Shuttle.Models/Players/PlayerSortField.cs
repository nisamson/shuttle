namespace Shuttle.Models.Players;

/// <summary>
/// The fields a <see cref="PlayerSearchQuery"/> may be sorted by. Kept intentionally small and
/// mapped to indexed / cheap-to-sort columns on the server.
/// </summary>
public enum PlayerSortField {
    Name,
    Username,
    TotalTpe,
    DraftSeason,
    Position,
    Status,
    League,
    Created,
}
