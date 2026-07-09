using Quartz;
using Shuttle.EFCore.Procedures;

namespace Shuttle.Api.Jobs;

public class DbUpdateJob : ISelfRegisteringJob {

    private readonly ILogger<DbUpdateJob> logger;
    private readonly IndexUpdater indexUpdater;
    private readonly PortalUpdater portalUpdater;
    private static readonly JobKey JobKey = JobKey.Create(nameof(DbUpdateJob), "data");
    private static readonly TriggerKey TriggerKey = new($"{nameof(DbUpdateJob)}Trigger", "data");
    
    public DbUpdateJob(ILogger<DbUpdateJob> logger, IndexUpdater indexUpdater, PortalUpdater portalUpdater) {
        this.logger = logger;
        this.indexUpdater = indexUpdater;
        this.portalUpdater = portalUpdater;
    }

    public async Task Execute(IJobExecutionContext context) {
        var token = context.CancellationToken;
        using var activity = ActivitySources.ShuttleApi.StartActivity();
        logger.LogInformation("Starting database update");
        await indexUpdater.UpdateIndex(token);
        logger.LogInformation("Finished index update");
        await portalUpdater.UpdatePortal(token);
        logger.LogInformation("Finished portal update");
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
