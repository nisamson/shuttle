using Shuttle.Models.Players;

namespace Shuttle.Api.Client;

/// <summary>
/// Convenience helpers over <see cref="IShuttlePlayerClient"/> that batch multiple-item fetches into
/// the server's bulk endpoints (rather than fanning out one request per item), transparently
/// splitting oversized requests to respect the server's per-request id cap.
/// </summary>
public static class ShuttlePlayerClientBatchExtensions {
    /// <summary>
    /// Maximum number of ids sent in a single <c>QUERY /players/cards</c> request. Kept at or below
    /// the server's <c>PlayerController.MaxCardIds</c> cap; larger id sets are split into successive
    /// requests and their results concatenated.
    /// </summary>
    public const int CardBatchSize = 500;

    /// <summary>
    /// Fetches the <see cref="PlayerCard"/>s for <paramref name="playerIds"/> using the bulk
    /// <c>QUERY /players/cards</c> endpoint, splitting the request into batches of at most
    /// <see cref="CardBatchSize"/> ids so any number of ids can be resolved. Duplicate ids are
    /// collapsed; unknown ids are omitted. The returned list is not guaranteed to preserve the input
    /// order — callers that need a specific order should index the result by
    /// <see cref="PlayerCard.PlayerId"/>.
    /// </summary>
    /// <param name="client">The player client.</param>
    /// <param name="playerIds">The ids to resolve.</param>
    /// <param name="token">A cancellation token.</param>
    public static async Task<IReadOnlyList<PlayerCard>> GetPlayerCardsBatched(
        this IShuttlePlayerClient client,
        IEnumerable<int> playerIds,
        CancellationToken token = default) {
        var ids = playerIds.Distinct().ToList();
        if (ids.Count == 0) {
            return [];
        }

        var cards = new List<PlayerCard>(ids.Count);
        for (var offset = 0; offset < ids.Count; offset += CardBatchSize) {
            var batch = ids.GetRange(offset, Math.Min(CardBatchSize, ids.Count - offset));
            var page = await client.GetPlayerCards(new PlayerCardsRequest { PlayerIds = batch }, token);
            cards.AddRange(page);
        }

        return cards;
    }
}
