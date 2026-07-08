using CrystalQuartz.Application;
using CrystalQuartz.Application.Startup;
using CrystalQuartz.AspNetCore;
using CrystalQuartz.Core.SchedulerProviders;
using Microsoft.AspNetCore.Http.Features;

namespace Shuttle.Api.Jobs.Quartz;

public static class CrystalQuartzConfiguration
{
  public static void UseCrystalQuartz(
    this WebApplication app,
    Func<object> schedulerProvider,
    AspNetCoreOptions? aspNetCoreOptions = null)
  {
    app.UseCrystalQuartz(schedulerProvider, null, aspNetCoreOptions);
  }

  public static void UseCrystalQuartz(
    this WebApplication app,
    Func<object> schedulerProvider,
    CrystalQuartzOptions? options,
    AspNetCoreOptions? aspNetCoreOptions = null)
  {
    ISchedulerProvider schedulerProvider1 = new FuncSchedulerProvider(schedulerProvider);
    app.UseCrystalQuartz(schedulerProvider1, options, aspNetCoreOptions);
  }

  public static void UseCrystalQuartz(
    this WebApplication app,
    ISchedulerProvider schedulerProvider,
    AspNetCoreOptions? aspNetCoreOptions = null)
  {
    app.UseCrystalQuartz(schedulerProvider, null, aspNetCoreOptions);
  }

  public static void UseCrystalQuartz(
    this WebApplication app,
    ISchedulerProvider schedulerProvider,
    CrystalQuartzOptions? options,
    AspNetCoreOptions? aspNetCoreOptions = null)
  {
    CrystalQuartzOptions actualOptions = options ?? new CrystalQuartzOptions();
    string pathMatch = actualOptions.Path ?? "/quartz";
    var panelApp = new CrystalQuartzPanelApplication(
        schedulerProvider,
        actualOptions.ToRuntimeOptions(SchedulerEngineProviders.SchedulerEngineResolvers, ".NET Standard 2.1")
      )
      .Run();
    app.MapGroup(pathMatch)
      .MapFallback(panelApp.Handle);
    app.Map((PathString) pathMatch, privateApp => {
      privateApp.UseMiddleware<CrystalQuartzPanelMiddleware>(schedulerProvider, actualOptions.ToRuntimeOptions(SchedulerEngineProviders.SchedulerEngineResolvers, ".NET Standard 2.1"));
    });
  }
}

