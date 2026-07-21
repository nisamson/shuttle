namespace Shuttle.Models.Players;

/// <summary>
/// Batch player-resolution request used by <c>QUERY /players/resolve</c>. Callers supply any mix of
/// player ids and/or player names; the endpoint resolves them to concrete players (reporting unknown
/// and ambiguous inputs) without mutating anything. Backs the WebClient's bulk-add preview.
/// </summary>
public sealed record ResolvePlayersRequest {
    /// <summary>Player ids to resolve. Unknown ids are reported in <c>NotFound</c>.</summary>
    public IReadOnlyList<int>? PlayerIds { get; init; }

    /// <summary>
    /// Player names to resolve, matched case-insensitively. A name matching more than one player is
    /// reported in <c>Ambiguous</c>; a name matching none is reported in <c>NotFound</c>.
    /// </summary>
    public IReadOnlyList<string>? Names { get; init; }
}
