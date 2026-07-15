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
    /// Fetches the "at a glance" <see cref="PlayerCard"/> for the given player id. Returns
    /// <see langword="null"/> when no player with that id exists (HTTP 404).
    /// </summary>
    /// <param name="playerId">The SHL player id.</param>
    /// <param name="token">A cancellation token.</param>
    [Get("/player/{playerId}")]
    Task<PlayerCard?> GetPlayer(int playerId, CancellationToken token = default);
}
