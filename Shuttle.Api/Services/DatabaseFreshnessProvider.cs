using System.Globalization;
using Quartz;
using Shuttle.Api.Jobs;

namespace Shuttle.Api.Services;

/// <summary>
/// Exposes the "database freshness" signal: the UTC time at which the scheduled
/// <see cref="DbUpdateJob"/> last completed a successful update. Responses whose body only changes
/// when the backing database is refreshed use this timestamp as their cache/ETag version, so they can
/// revalidate cheaply (via <c>If-None-Match</c> / <c>304 Not Modified</c>) between updates.
/// </summary>
public interface IDatabaseFreshnessProvider {
    /// <summary>
    /// Returns the UTC time the database update last completed, read from the <see cref="DbUpdateJob"/>
    /// persisted <see cref="JobDataMap"/> (durable across restarts and trigger changes), or
    /// <c>null</c> when no completed update has been recorded yet.
    /// </summary>
    Task<DateTimeOffset?> GetLastUpdatedAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Default <see cref="IDatabaseFreshnessProvider"/> that reads the last-updated timestamp from the
/// <see cref="DbUpdateJob"/> Quartz job's persisted <see cref="JobDataMap"/>.
/// </summary>
public sealed class DatabaseFreshnessProvider : IDatabaseFreshnessProvider {
    private readonly ISchedulerFactory schedulerFactory;

    public DatabaseFreshnessProvider(ISchedulerFactory schedulerFactory) {
        this.schedulerFactory = schedulerFactory;
    }

    public async Task<DateTimeOffset?> GetLastUpdatedAsync(CancellationToken cancellationToken = default) {
        var scheduler = await schedulerFactory.GetScheduler(cancellationToken);
        var jobDetail = await scheduler.GetJobDetail(DbUpdateJob.JobKey, cancellationToken);
        var raw = jobDetail?.JobDataMap.GetString(DbUpdateJob.LastUpdatedKey);

        return DateTimeOffset.TryParse(
            raw,
            CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind,
            out var parsed)
            ? parsed
            : null;
    }
}
