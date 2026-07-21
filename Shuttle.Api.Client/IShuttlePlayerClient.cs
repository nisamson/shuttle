using Refit;
using Shuttle.Models.Players;

namespace Shuttle.Api.Client;

/// <summary>
/// Typed Refit client for the Shuttle backend API (<c>Shuttle.Api</c>) player endpoints. The base
/// address is supplied at registration time (see
/// <see cref="ShuttleApiClientExtensions.AddShuttleApiClient"/>).
/// </summary>
public interface IShuttlePlayerClient {
    /// <summary>
    /// Fetches the "at a glance" <see cref="PlayerCard"/> for every player, ordered by name.
    /// </summary>
    /// <param name="token">A cancellation token.</param>
    [Get("/players")]
    Task<IReadOnlyList<PlayerCard>> GetPlayers(CancellationToken token = default);

    /// <summary>
    /// Searches players with server-side filtering, sorting, and pagination. The multiselect
    /// filters on <paramref name="query"/> are serialized as repeated query keys.
    /// </summary>
    /// <param name="query">The filter, sort, and paging options.</param>
    /// <param name="token">A cancellation token.</param>
    [Get("/players/search")]
    Task<PagedResult<PlayerCard>> SearchPlayers([Query] PlayerSearchQuery query, CancellationToken token = default);

    /// <summary>
    /// Fetches the slim <see cref="PlayerSuggestion"/> directory for every player, ordered by name.
    /// Intended to be fetched once and cached client-side to power local name/username autocomplete.
    /// </summary>
    /// <param name="token">A cancellation token.</param>
    [Get("/players/suggestions")]
    Task<IReadOnlyList<PlayerSuggestion>> GetPlayerSuggestions(CancellationToken token = default);

    /// <summary>
    /// Fetches the "at a glance" <see cref="PlayerCard"/> for the given player id. Returns
    /// <see langword="null"/> when no player with that id exists (HTTP 404).
    /// </summary>
    /// <param name="playerId">The SHL player id.</param>
    /// <param name="token">A cancellation token.</param>
    [Get("/player/{playerId}")]
    Task<PlayerCard?> GetPlayer(int playerId, CancellationToken token = default);

    /// <summary>
    /// Fetches the player's TPE timeline (cumulative total TPE over time), ordered chronologically.
    /// Returns <see langword="null"/> when no player with that id exists (HTTP 404); an existing
    /// player with no recorded events yields an empty list.
    /// </summary>
    /// <param name="playerId">The SHL player id.</param>
    /// <param name="token">A cancellation token.</param>
    [Get("/player/{playerId}/tpe-timeline")]
    Task<IReadOnlyList<TpeTimelinePoint>?> GetPlayerTpeTimeline(int playerId, CancellationToken token = default);

    /// <summary>
    /// Looks up a batch of player ids and/or names, resolving them to concrete players (reporting
    /// unknown and ambiguous inputs) without mutating anything. Uses the HTTP <c>QUERY</c> verb. Backs
    /// the bulk-add preview.
    /// </summary>
    /// <param name="request">The ids and/or names to look up.</param>
    /// <param name="token">A cancellation token.</param>
    [HttpQuery("/players/lookup")]
    Task<PlayerLookupResult> LookupPlayers([Body] PlayerLookupRequest request, CancellationToken token = default);
}
