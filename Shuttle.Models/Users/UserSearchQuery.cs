namespace Shuttle.Models.Users;

/// <summary>
/// Filters, paging, and sort options accepted by <c>GET /users/search</c> and sent by the Refit
/// client as a flattened query string. Every filter is optional; unset / empty properties are
/// ignored by the server.
/// </summary>
public record UserSearchQuery {
    /// <summary>
    /// Free-text match against the username (case-insensitive contains). When
    /// <see cref="SearchDiscord"/> is set and the caller is authenticated, the Discord name is also
    /// matched.
    /// </summary>
    public string? Text { get; init; }

    /// <summary>
    /// When <see langword="true"/>, also match <see cref="Text"/> against the user's Discord name.
    /// Only honoured for authenticated callers; ignored for anonymous requests.
    /// </summary>
    public bool SearchDiscord { get; init; }

    /// <summary>The 1-based page number to return. Defaults to the first page.</summary>
    public int Page { get; init; } = 1;

    /// <summary>The page size. The server clamps this to the range 1..100.</summary>
    public int PageSize { get; init; } = 25;

    /// <summary>The field to sort by. Defaults to <see cref="UserSortField.Username"/>.</summary>
    public UserSortField SortBy { get; init; } = UserSortField.Username;

    /// <summary>Whether to sort in descending order. Defaults to ascending.</summary>
    public bool SortDescending { get; init; }
}
