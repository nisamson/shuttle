using System.Globalization;
using Microsoft.Extensions.Caching.Hybrid;
using Shuttle.Api.Jobs;
using Shuttle.EFCore.Recruitment;

namespace Shuttle.Api.Services.Recruitment;

/// <summary>
/// A cached <see cref="RecruitmentAnalysis"/> together with the database freshness signal it was
/// computed against.
/// </summary>
/// <param name="Analysis">The recruitment analysis.</param>
/// <param name="LastUpdated">
/// When the backing database last completed an update (from the <see cref="DbUpdateJob"/> Quartz
/// job), or <c>null</c> when no completed update has been recorded. Doubles as the cache/ETag
/// version signal.
/// </param>
public sealed record RecruitmentAnalysisSnapshot(RecruitmentAnalysis Analysis, DateTimeOffset? LastUpdated);

/// <summary>
/// Provides the recruitment analysis, caching it so the (relatively expensive) recompute only runs
/// when the underlying data changes.
/// </summary>
public interface IRecruitmentAnalysisCache {
    /// <summary>
    /// Returns the current recruitment analysis, recomputing only when the database freshness signal
    /// has changed since the cached copy was produced.
    /// </summary>
    ValueTask<RecruitmentAnalysisSnapshot> GetAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Default <see cref="IRecruitmentAnalysisCache"/> backed by <see cref="HybridCache"/>. The cache
/// key is derived from the <see cref="DbUpdateJob"/> "last updated" timestamp (the same freshness
/// signal <c>DataController</c> exposes), so a completed database update naturally rotates the key
/// and forces a recompute; between updates the analysis is served from cache. <see cref="HybridCache"/>
/// coalesces concurrent factory calls for the same key, guarding against a recompute stampede.
/// </summary>
/// <remarks>
/// The in-process cache is per-instance (no distributed L2 is registered), but keying on the shared,
/// persisted Quartz timestamp keeps every instance independently consistent. Note that the standalone
/// <c>Shuttle.DbUpdate</c> CLI does not write that timestamp, so out-of-band updates it performs will
/// not invalidate this cache — in production Quartz owns the update job, so this is not a concern.
/// </remarks>
public sealed class RecruitmentAnalysisCache : IRecruitmentAnalysisCache {
    private const string CacheKeyPrefix = "recruitment:";

    private static readonly string[] DbDataCacheTags = [CacheTags.DatabaseData];

    // The key can't change while the signal is unknown, so fall back to a short expiration that
    // periodically retries the freshness lookup and recompute.
    private static readonly HybridCacheEntryOptions UnknownSignalOptions = new() {
        Expiration = TimeSpan.FromMinutes(1),
        LocalCacheExpiration = TimeSpan.FromMinutes(1),
    };

    // A known signal rotates the key on the next update, so the entry can live comfortably long.
    private static readonly HybridCacheEntryOptions KnownSignalOptions = new() {
        Expiration = TimeSpan.FromHours(12),
        LocalCacheExpiration = TimeSpan.FromHours(12),
    };

    private readonly HybridCache cache;
    private readonly IDatabaseFreshnessProvider freshness;
    private readonly IServiceScopeFactory scopeFactory;

    public RecruitmentAnalysisCache(
        HybridCache cache,
        IDatabaseFreshnessProvider freshness,
        IServiceScopeFactory scopeFactory) {
        this.cache = cache;
        this.freshness = freshness;
        this.scopeFactory = scopeFactory;
    }

    public async ValueTask<RecruitmentAnalysisSnapshot> GetAsync(CancellationToken cancellationToken = default) {
        var lastUpdated = await freshness.GetLastUpdatedAsync(cancellationToken);
        var key = CacheKeyPrefix + (lastUpdated?.ToString("o", CultureInfo.InvariantCulture) ?? "none");

        var analysis = await cache.GetOrCreateAsync(
            key,
            scopeFactory,
            static async (factory, ct) => {
                await using var scope = factory.CreateAsyncScope();
                var service = scope.ServiceProvider.GetRequiredService<IRecruitmentAnalysisService>();
                return await service.GetRecruitmentAnalysisAsync(ct);
            },
            lastUpdated is null ? UnknownSignalOptions : KnownSignalOptions,
            tags: DbDataCacheTags,
            cancellationToken);

        return new RecruitmentAnalysisSnapshot(analysis, lastUpdated);
    }
}

/// <summary>
/// DI registration for the recruitment analysis cache.
/// </summary>
public static class RecruitmentAnalysisCacheExtensions {
    /// <summary>
    /// Registers <see cref="HybridCache"/> and the singleton <see cref="IRecruitmentAnalysisCache"/>.
    /// </summary>
    public static IServiceCollection AddRecruitmentAnalysisCache(this IServiceCollection services) {
        services.AddHybridCache();
        services.AddSingleton<IRecruitmentAnalysisCache, RecruitmentAnalysisCache>();
        return services;
    }
}
