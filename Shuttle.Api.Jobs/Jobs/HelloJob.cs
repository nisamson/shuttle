using Quartz;

namespace Shuttle.Api.Jobs.Jobs;

public class HelloJob : ISelfRegisteringJob {
    public static readonly JobKey Key = new(nameof(HelloJob), "debug");
    public static readonly TriggerKey TriggerKey = new(nameof(HelloJob) + "Trigger", "debug");
    
    private readonly ILogger<HelloJob> logger;
    
    public HelloJob(ILogger<HelloJob> logger) {
        this.logger = logger;
    }
    
    public Task Execute(IJobExecutionContext context) {
        logger.LogInformation("Hello from {JobKey} at {ExecutionTime}", Key, DateTimeOffset.UtcNow);
        return Task.CompletedTask;
    }

    public static IServiceCollectionQuartzConfigurator RegisterJob(IServiceCollectionQuartzConfigurator qc) {
        qc.ScheduleJob<HelloJob>(opts => {
            opts.WithIdentity(TriggerKey)
                .WithSimpleSchedule(x => x.WithIntervalInHours(1).RepeatForever())
                .WithDescription("Trigger for HelloJob, runs every hour.");
        },
        job => {
            job.WithIdentity(Key)
                .WithDescription("A simple job that logs a greeting message.")
                .StoreDurably();
        });
        return qc;
    }
}
