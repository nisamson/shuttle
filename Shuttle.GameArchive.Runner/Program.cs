using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shuttle.GameArchive;
using Shuttle.GameArchive.Runner;

var builder = Host.CreateApplicationBuilder(args);
builder.Logging.AddFilter("System.Net.Http.HttpClient", LogLevel.Warning);
using var startupLogs = LoggerFactory.Create(logging => logging.AddSimpleConsole());
var logger = startupLogs.CreateLogger("ArchiveRunner");

return await ArchiveRunner.RunOnceAsync(async token => {
    ArchiveRunner.ValidateConfiguration(builder.Configuration, builder.Environment);
    builder.Services.AddGameArchive(builder.Configuration, builder.Environment, runOnce: true);
    using var host = builder.Build();
    await host.StartAsync(token);
    try {
        using var shutdown = CancellationTokenSource.CreateLinkedTokenSource(
            token, host.Services.GetRequiredService<IHostApplicationLifetime>().ApplicationStopping);
        logger.LogInformation("Starting one-shot archive sync from {Source} to {Remote}",
            builder.Configuration["Archive:SourceUrl"] ?? new GameArchiveOptions().SourceUrl,
            builder.Configuration["Archive:RemoteUrl"]);
        using var scope = host.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<GameArchiveSynchronizer>().SynchronizeAsync(shutdown.Token);
    } finally {
        await host.StopAsync(CancellationToken.None);
    }
}, logger, CancellationToken.None);
