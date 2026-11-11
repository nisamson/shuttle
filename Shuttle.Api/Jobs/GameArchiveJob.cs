using Quartz;
using Shuttle.GameArchive;
using Microsoft.Extensions.Options;

namespace Shuttle.Api.Jobs;

public sealed class GameArchiveJob(
    IServiceProvider services,
    IOptions<GameArchiveOptions> options,
    ILogger<GameArchiveJob> logger) : ISelfRegisteringJob {
    public static readonly JobKey JobKey = new(nameof(GameArchiveJob), "archive");
    public static readonly TriggerKey TriggerKey = new(nameof(GameArchiveJob) + "Trigger", "archive");

    public async Task Execute(IJobExecutionContext context) {
        if (!options.Value.Enabled) {
            logger.LogInformation("Skipping disabled SHL game archive sync");
            return;
        }
        logger.LogInformation("Starting SHL game archive sync");
        try {
            using var scope = services.CreateScope();
            var synchronizer = scope.ServiceProvider.GetRequiredService<GameArchiveSynchronizer>();
            await synchronizer.SynchronizeAsync(context.CancellationToken);
            logger.LogInformation("Finished SHL game archive sync");
        } catch (Exception ex) when (ex is not OperationCanceledException) {
            logger.LogError(ex, "SHL game archive sync failed");
            throw new JobExecutionException(ex, refireImmediately: false);
        }
    }

    public static IServiceCollectionQuartzConfigurator RegisterJob(IServiceCollectionQuartzConfigurator qc) {
        qc.ScheduleJob<GameArchiveJob>(
            trigger => trigger.WithIdentity(TriggerKey)
                .ForJob(JobKey)
                .WithSimpleSchedule(schedule => schedule.WithIntervalInHours(6).RepeatForever())
                .WithDescription("Mirror public SHL game files to the archive repository"),
            job => job.WithIdentity(JobKey).StoreDurably().DisallowConcurrentExecution());
        return qc;
    }
}
