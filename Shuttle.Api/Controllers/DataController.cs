using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quartz;
using Shuttle.Api.Jobs;
using Shuttle.Api.Services;
using Shuttle.Models.Meta;

namespace Shuttle.Api.Controllers;

/// <summary>
/// Public, unauthenticated read access to metadata about the backing dataset, such as when the
/// database was last refreshed from the upstream SHL APIs.
/// </summary>
[AllowAnonymous]
[ApiController]
[Route("data")]
public class DataController : ControllerBase {
    private static readonly TimeSpan CacheMaxAge = TimeSpan.FromMinutes(5);

    private readonly ISchedulerFactory schedulerFactory;
    private readonly IDatabaseFreshnessProvider freshness;
    private readonly ILogger<DataController> logger;

    public DataController(
        ISchedulerFactory schedulerFactory,
        IDatabaseFreshnessProvider freshness,
        ILogger<DataController> logger) {
        this.schedulerFactory = schedulerFactory;
        this.freshness = freshness;
        this.logger = logger;
    }

    /// <summary>
    /// Returns metadata describing how fresh the database is. The last-updated time is read from the
    /// database update job's persisted <see cref="Quartz.JobDataMap"/> (durable across restarts and
    /// trigger changes) and reflects when the update actually completed; the next-update time comes
    /// from the job's trigger. The response carries an ETag/Last-Modified derived from the last-updated
    /// time, so callers can revalidate with <c>If-None-Match</c> and receive <c>304 Not Modified</c>
    /// until the next update completes.
    /// </summary>
    [HttpGet("metainfo")]
    [ProducesResponseType<DataMetaInfo>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status304NotModified)]
    public async Task<ActionResult<DataMetaInfo>> GetMetaInfo(CancellationToken cancellationToken) {
        var lastUpdated = await freshness.GetLastUpdatedAsync(cancellationToken);

        var scheduler = await schedulerFactory.GetScheduler(cancellationToken);
        var trigger = await scheduler.GetTrigger(DbUpdateJob.TriggerKey, cancellationToken);
        var nextExpectedUpdate = trigger?.GetNextFireTimeUtc();

        var result = new DataMetaInfo(lastUpdated, nextExpectedUpdate);

        return this.DbVersionedOk(result, lastUpdated, CacheMaxAge);
    }
}
