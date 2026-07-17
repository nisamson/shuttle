using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using Quartz;
using Shuttle.Api.Jobs;
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
    private readonly ISchedulerFactory schedulerFactory;
    private readonly ILogger<DataController> logger;

    public DataController(ISchedulerFactory schedulerFactory, ILogger<DataController> logger) {
        this.schedulerFactory = schedulerFactory;
        this.logger = logger;
    }

    /// <summary>
    /// Returns metadata describing how fresh the database is. The last-updated time is read from the
    /// database update job's persisted <see cref="Quartz.JobDataMap"/> (durable across restarts and
    /// trigger changes) and reflects when the update actually completed; the next-update time comes
    /// from the job's trigger.
    /// </summary>
    [HttpGet("metainfo")]
    [ProducesResponseType<DataMetaInfo>(StatusCodes.Status200OK)]
    public async Task<ActionResult<DataMetaInfo>> GetMetaInfo(CancellationToken cancellationToken) {
        var scheduler = await schedulerFactory.GetScheduler(cancellationToken);

        var jobDetail = await scheduler.GetJobDetail(DbUpdateJob.JobKey, cancellationToken);
        var lastUpdated = ParseLastUpdated(jobDetail?.JobDataMap.GetString(DbUpdateJob.LastUpdatedKey));

        var trigger = await scheduler.GetTrigger(DbUpdateJob.TriggerKey, cancellationToken);
        var nextExpectedUpdate = trigger?.GetNextFireTimeUtc();

        var result = new DataMetaInfo(lastUpdated, nextExpectedUpdate);

        SetCacheHeaders();

        return Ok(result);
    }

    private static DateTimeOffset? ParseLastUpdated(string? value) =>
        DateTimeOffset.TryParse(
            value,
            CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind,
            out var parsed)
            ? parsed
            : null;

    private void SetCacheHeaders() {
        Response.GetTypedHeaders().CacheControl = new CacheControlHeaderValue {
            Public = true,
            MaxAge = TimeSpan.FromMinutes(5),
        };
    }
}
