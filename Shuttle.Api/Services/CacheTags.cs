namespace Shuttle.Api.Services;

/// <summary>
/// Well-known <see cref="Microsoft.Extensions.Caching.Hybrid.HybridCache"/> tags shared across the
/// API so related cache entries can be invalidated together.
/// </summary>
public static class CacheTags {
    /// <summary>
    /// Tags any cache entry derived from the periodically-refreshed database (for example the
    /// recruitment analysis). The <c>DbUpdateJob</c> purges everything under this tag after a
    /// successful update, so add it to any future DB-derived cache entries you want invalidated on
    /// each refresh.
    /// </summary>
    public const string DatabaseData = "db-data";
}
