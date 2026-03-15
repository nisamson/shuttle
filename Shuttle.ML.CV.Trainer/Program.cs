

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Shuttle.ML.CV.Trainer;
using Shuttle.ML.CV.Trainer.Commands;

var parser = CommonCliOptions.CreateRootWithSubcommands();
var parseResult = parser.Parse(args);

var builder = Host.CreateApplicationBuilder();

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console()
    .CreateLogger();

Log.Logger.Debug("Bootstrap logger configured.");

var options = CommonCliOptions.FromResult(parseResult);

switch (options) {
    case DedupOptions dedupOptions:
        builder.Services.AddSingleton(dedupOptions);
        builder.Services.AddScoped<ICommand, DedupCommand>();
        break;
    default:
        throw new InvalidOperationException("Unsupported command options type.");
}

var loggingPath = options.LoggingPath;


builder.Services.AddSerilog(c => {
        c.WriteTo.Console()
            .Enrich.FromLogContext();
        if (!string.IsNullOrEmpty(loggingPath?.FullName))
        {
            c.WriteTo.File(loggingPath.FullName, buffered: true);
        }
    }
);

Log.Logger.Information("ML Trainer configuring at {StartTime}", DateTimeOffset.UtcNow);

var app = builder.Build();

await app.StartAsync();

var command = app.Services.GetRequiredService<ICommand>();

var logger = app.Services.GetRequiredService<ILogger<ICommand>>();

try {
    logger.LogInformation(
        "Executing command {CommandName} at {StartTime}",
        command.Name,
        DateTimeOffset.UtcNow
    );
    await command.Execute();
} catch (Exception ex) {
    logger.LogError(ex, "An error occurred while executing the command.");
}

await app.StopAsync(TimeSpan.FromSeconds(5));