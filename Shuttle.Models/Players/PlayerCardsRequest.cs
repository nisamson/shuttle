namespace Shuttle.Models.Players;

/// <summary>
/// Batch player-card request used by <c>QUERY /players/cards</c>. Callers supply a set of player ids
/// and receive the "at a glance" <see cref="PlayerCard"/> for each one that exists, resolving many
/// cards in a single round trip instead of one request per id. Unknown ids are simply omitted from
/// the response. Backs the scouting board and player comparison.
/// </summary>
public sealed record PlayerCardsRequest {
    /// <summary>The player ids to fetch cards for. Unknown ids are omitted from the result.</summary>
    public IReadOnlyList<int>? PlayerIds { get; init; }
}
