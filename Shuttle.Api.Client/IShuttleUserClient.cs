using Refit;
using Shuttle.Models.Players;
using Shuttle.Models.Users;

namespace Shuttle.Api.Client;

/// <summary>
/// Typed Refit client for the Shuttle backend API (<c>Shuttle.Api</c>) user endpoints. The base
/// address is supplied at registration time (see
/// <see cref="ShuttleApiClientExtensions.AddShuttleUserClient"/>).
/// <para>
/// Discord names are only returned by the backend to authenticated callers, so this client should
/// be configured with an auth message handler that attaches the caller's access token when one is
/// available.
/// </para>
/// </summary>
public interface IShuttleUserClient {
    /// <summary>
    /// Fetches the <see cref="UserCard"/> for the given user, identified by either their numeric
    /// user id or their username. Returns <see langword="null"/> when no such user exists (HTTP 404).
    /// </summary>
    /// <param name="userIdOrName">The numeric user id or the username.</param>
    /// <param name="players">
    /// When <see langword="true"/>, include the player cards this user has created (empty when they
    /// have none); when <see langword="false"/>, <see cref="UserCard.Players"/> is left null.
    /// </param>
    /// <param name="token">A cancellation token.</param>
    [Get("/user/{userIdOrName}")]
    Task<UserCard?> GetUser(string userIdOrName, [Query] bool players = false, CancellationToken token = default);

    /// <summary>
    /// Searches users with server-side filtering, sorting, and pagination. Discord matching is only
    /// honoured for authenticated callers who opt in via <see cref="UserSearchQuery.SearchDiscord"/>.
    /// </summary>
    /// <param name="query">The filter, sort, and paging options.</param>
    /// <param name="token">A cancellation token.</param>
    [Get("/users/search")]
    Task<PagedResult<UserCard>> SearchUsers([Query] UserSearchQuery query, CancellationToken token = default);

    /// <summary>
    /// Fetches the slim <see cref="UserSuggestion"/> directory for every user, ordered by username.
    /// Intended to be fetched once and cached client-side to power local username autocomplete.
    /// </summary>
    /// <param name="token">A cancellation token.</param>
    [Get("/users/suggestions")]
    Task<IReadOnlyList<UserSuggestion>> GetUserSuggestions(CancellationToken token = default);
}
