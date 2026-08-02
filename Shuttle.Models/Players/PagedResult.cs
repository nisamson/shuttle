namespace Shuttle.Models.Players;

/// <summary>
/// A single page of results from a paginated endpoint, together with the paging metadata needed to
/// render page navigation on the client.
/// </summary>
/// <typeparam name="T">The element type.</typeparam>
public record PagedResult<T> {
    /// <summary>The items on the current page.</summary>
    public required IReadOnlyList<T> Items { get; init; }

    /// <summary>The 1-based page number this result represents.</summary>
    public required int Page { get; init; }

    /// <summary>The page size (maximum number of items per page) used for this query.</summary>
    public required int PageSize { get; init; }

    /// <summary>The total number of items across all pages matching the query.</summary>
    public required int TotalCount { get; init; }

    /// <summary>The total number of pages, derived from <see cref="TotalCount"/> and <see cref="PageSize"/>.</summary>
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}
