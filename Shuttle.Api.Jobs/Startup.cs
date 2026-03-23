using Quartz;
using Quartz.AspNetCore;
using Shuttle.EFCore;

namespace Shuttle.Api.Jobs;

public static class Startup {
    public const string OptionsAuthorizationPolicy = "QuartzDashboard";

    public static void AddQuartz(this WebApplicationBuilder builder) {
        builder.Services.Configure<QuartzOptions>(options => {
                options.Scheduling.IgnoreDuplicates = true;
                options.Scheduling.OverWriteExistingData = true;
                options.Scheduling.ScheduleTriggerRelativeToReplacedTrigger = true;
                options[ShuttleEfCoreConstants.QuartzTablePrefixKey] = ShuttleEfCoreConstants.QuartzTablePrefix;
                options["quartz.plugin.jobHistory.type"] = "Quartz.Plugin.History.LoggingJobHistoryPlugin, Quartz.Plugins";
                options["quartz.plugin.triggerHistory.type"] = "Quartz.Plugin.History.LoggingTriggerHistoryPlugin, Quartz.Plugins";
            }
        );
        var connStr = builder.GetConnectionString();
        builder.Services.AddQuartz(q => {
            q.InterruptJobsOnShutdownWithWait = true;
            q.SchedulerName = "Shuttle Quartz Scheduler";
            q.UsePersistentStore(pso => {
                pso.UseSystemTextJsonSerializer();
                pso.UseProperties = true;
                pso.UseSqlServer(connStr);
            });
        });
        builder.Services.AddQuartzServer(o => {
                o.AwaitApplicationStarted = true;
            }
        );
        builder.Services.AddAuthorization(options => {
                options.AddPolicy(
                    OptionsAuthorizationPolicy,
                    policy => {
                        policy.RequireAuthenticatedUser();
                        policy.RequireRole("Admin");
                    }
                );
            }
        );
        builder.Services.AddQuartzDashboard(options => {
                options.AuthorizationPolicy = OptionsAuthorizationPolicy;
            }
        );
    }
}
