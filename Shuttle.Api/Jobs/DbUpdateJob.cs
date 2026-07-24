using System.Diagnostics;
using Quartz;
using Shuttle.EFCore.Procedures;

namespace Shuttle.Api.Jobs;

public class DbUpdateJob : ISelfRegisteringJob {

    private readonly ILogger<DbUpdateJob> logger;
    private readonly IndexUpdater indexUpdater;
    private readonly PortalUpdater portalUpdater;
    public static readonly JobKey JobKey = JobKey.Create(nameof(DbUpdateJob), "data");
    public static readonly TriggerKey TriggerKey = new($"{nameof(DbUpdateJob)}Trigger", "data");

    /// <summary>
    /// Key under which the UTC completion time of the most recent successful update is stored in the
    /// job's persisted <see cref="JobDataMap"/> (round-trip "o" format). Persisted across restarts and
    /// trigger changes because the job is registered with <c>PersistJobDataAfterExecution</c>.
    /// </summary>
    public const string LastUpdatedKey = "LastUpdatedUtc";

    
    public DbUpdateJob(ILogger<DbUpdateJob> logger, IndexUpdater indexUpdater, PortalUpdater portalUpdater) {
        this.logger = logger;
        this.indexUpdater = indexUpdater;
        this.portalUpdater = portalUpdater;
    }

    public async Task Execute(IJobExecutionContext context) {
        var token = context.CancellationToken;
        // Server kind so Azure Monitor maps this to a request/operation that anchors the
        // background job's child spans (index/portal updates); background jobs have no
        // incoming HTTP request to otherwise anchor the trace. Explicit name keeps the
        // operation stable regardless of the method name.
        using var activity = ActivitySources.ShuttleApi.StartActivity("ExecuteDbUpdate", ActivityKind.Server);
        logger.LogInformation("Starting database update");
        await indexUpdater.UpdateIndex(token);
        logger.LogInformation("Finished index update");
        await portalUpdater.UpdatePortal(token);
        logger.LogInformation("Finished portal update");
        context.JobDetail.JobDataMap.Put(LastUpdatedKey, DateTimeOffset.UtcNow.ToString("o"));
        logger.LogInformation("Finished updating the database");
    }
    public static IServiceCollectionQuartzConfigurator RegisterJob(IServiceCollectionQuartzConfigurator qc) {
        qc.ScheduleJob<DbUpdateJob>(
            tc => {
                tc.WithIdentity(TriggerKey)
                    .ForJob(JobKey)
                    .WithSimpleSchedule(s => s.WithIntervalInHours(6).RepeatForever())
                    .WithDescription("Updates the database with the latest data from the SHL API");
            },
            jc => {
                jc.WithIdentity(JobKey)
                    .WithDescription("Updates the database with the latest data from the SHL API")
                    .StoreDurably()
                    .PersistJobDataAfterExecution()
                    .DisallowConcurrentExecution();
            });
        return qc;
    }
}
