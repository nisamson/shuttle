using System.Reflection;
using System.Runtime.InteropServices;
using CrystalQuartz.Application;
using CrystalQuartz.Application.Startup;
using CrystalQuartz.AspNetCore;
using CrystalQuartz.Core.SchedulerProviders;
using CrystalQuartz.WebFramework;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using OpenTelemetry.Trace;
using Quartz;
using Quartz.AspNetCore;
using Quartz.Impl;
using Shuttle.Api.Jobs.Jobs;
using Shuttle.Api.Jobs.Quartz;
using Shuttle.EFCore;
using Shuttle.EFCore.Procedures;
using Shuttle.ServiceDefaults;

namespace Shuttle.Api.Jobs;

public static class Startup {
    public const string OptionsAuthorizationPolicy = "QuartzDashboard";
    public const string OptionsSectionName = "QuartzDashAuth";
    public const string OptionsAuthScheme = OpenIdConnectDefaults.AuthenticationScheme;

    public static void AddQuartz(this WebApplicationBuilder builder) {
        builder.Services.Configure<QuartzOptions>(options => {
                options.Scheduling.IgnoreDuplicates = true;
                options.Scheduling.OverWriteExistingData = false;
                options.Scheduling.ScheduleTriggerRelativeToReplacedTrigger = true;
                options["quartz.plugin.jobHistory.type"] = "Quartz.Plugin.History.LoggingJobHistoryPlugin, Quartz.Plugins";
                options["quartz.plugin.triggerHistory.type"] = "Quartz.Plugin.History.LoggingTriggerHistoryPlugin, Quartz.Plugins";
            }
        );
        builder.Services.AddScoped<IndexUpdater>();
        builder.Services.AddScoped<PortalUpdater>();
        var connStr = ShuttleEfCoreExtensions.GetConnectionString();
        builder.Services.AddQuartz(q => {
            q.InterruptJobsOnShutdownWithWait = true;
            q.SchedulerName = "Shuttle Quartz Scheduler";
            q.UsePersistentStore(pso => {
                pso.UseSystemTextJsonSerializer();
                pso.UseProperties = true;
                pso.UseSqlServer(
                    configure => {
                        configure.ConnectionString = connStr;
                        configure.TablePrefix = ShuttleEfCoreConstants.QuartzTablePrefix;
                    });
            });
            HelloJob.RegisterJob(q);
            DbUpdateJob.RegisterJob(q);
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
                        policy.RequireRole("Shuttle.Jobs.Admin");
                        policy.AuthenticationSchemes.Add(OptionsAuthScheme);
                    }
                );
            }
        );
        builder.Services.AddOpenTelemetry()
            .WithTracing(t => {
                    t.AddQuartzInstrumentation();
                }
            );
        builder.AddTelemetryService(ActivitySources.ShuttleJobs);
        builder.Services.AddSingleton(new CrystalQuartzOptions());
        builder.Services.AddSingleton(provider => {
                var scheduleFactory = provider.GetRequiredService<ISchedulerFactory>();
                var options = provider.GetRequiredService<CrystalQuartzOptions>();
                var app = new CrystalQuartzPanelApplication(
                    new FuncSchedulerProvider(() => scheduleFactory.GetScheduler().GetAwaiter().GetResult()),
                    options.ToRuntimeOptions(SchedulerEngineProviders.SchedulerEngineResolvers, RuntimeInformation.FrameworkDescription)
                );
                return app.Run();
            }
        );
    }

    public static void AddQuartz(this WebApplication app) {
        var options = app.Services.GetRequiredService<CrystalQuartzOptions>();
        var panel = app.Services.GetRequiredService<IRunningApplication>();
        app.MapGroup(options.Path ?? "/quartz")
            .MapFallback(httpContext => panel.Handle(new AspNetCoreRequest(httpContext.Request.Query, httpContext.Request.HasFormContentType ? httpContext.Request.Form : null), new AspNetCoreResponseRenderer(httpContext)))
            .RequireAuthorization(OptionsAuthorizationPolicy);
    }
}
