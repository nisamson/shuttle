// Standalone runner for the DbUpdateJob logic. Pulls the latest data from the SHL
// Index and Portal APIs into the Shuttle database without starting Shuttle.Api.
//
// Requires the same environment configuration as the server:
//   SHUTTLESQLSERVER_DATABASE  - the Azure SQL database (catalog) name
//   SHUTTLESQLSERVER_HOST      - the Azure SQL server host
// These may be supplied via the environment or a local .env file (loaded below).
// Azure SQL uses ActiveDirectoryDefault auth, so a signed-in Azure identity
// (e.g. `az login`) that can access the database is also required.

using dotenv.net;
using LinqToDB.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shuttle.EFCore;
using Shuttle.EFCore.Procedures;
using Shuttle.Shl.Api.Client;

LoadEnvironment();

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddShlApiClients();
builder.AddShuttleDatabase();
builder.Services.AddScoped<IndexUpdater>();
builder.Services.AddScoped<PortalUpdater>();

var app = builder.Build();

LinqToDBForEFTools.Initialize();

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, eventArgs) => {
    eventArgs.Cancel = true;
    cts.Cancel();
};

var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("DbUpdate");

try {
    await app.EnsureShuttleDatabaseConnectivity(cts.Token);

    using var scope = app.Services.CreateScope();
    var indexUpdater = scope.ServiceProvider.GetRequiredService<IndexUpdater>();
    var portalUpdater = scope.ServiceProvider.GetRequiredService<PortalUpdater>();

    logger.LogInformation("Starting database update");
    await indexUpdater.UpdateIndex(cts.Token);
    logger.LogInformation("Finished index update");
    await portalUpdater.UpdatePortal(cts.Token);
    logger.LogInformation("Finished portal update");
    logger.LogInformation("Finished updating the database");
} catch (OperationCanceledException) {
    logger.LogWarning("Database update cancelled");
    return 130;
} catch (Exception ex) {
    logger.LogError(ex, "Database update failed");
    return 1;
}

return 0;

// Loads database configuration from the shared Shuttle.EFCore/.env file (the same
// file the EF Core design-time factory uses), regardless of the current working
// directory, then any real environment variables. Existing environment variables
// always win, so machine/user-level configuration still overrides the .env file.
static void LoadEnvironment() {
    // Walk up from the executable location and the current directory looking for the
    // solution root, then load the well-known .env alongside Shuttle.EFCore.
    foreach (var start in new[] { AppContext.BaseDirectory, Directory.GetCurrentDirectory() }) {
        var dir = new DirectoryInfo(start);
        while (dir is not null) {
            var envPath = Path.Combine(dir.FullName, "Shuttle.EFCore", ".env");
            if (File.Exists(envPath)) {
                DotEnv.Load(new DotEnvOptions(envFilePaths: [envPath], overwriteExistingVars: false));
                return;
            }

            dir = dir.Parent;
        }
    }

    // Fall back to the default behaviour (a .env in the current directory, if any).
    DotEnv.Load(new DotEnvOptions(overwriteExistingVars: false));
}
