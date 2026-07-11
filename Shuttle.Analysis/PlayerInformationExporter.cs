using System.Text.Json;
using LinqToDB.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shuttle.EFCore;

namespace Shuttle.Analysis;

/// <summary>
/// Downloads the current <c>PlayerInformation</c> table into a local JSON file for offline analysis.
/// </summary>
public static class PlayerInformationExporter {

    /// <summary>
    /// Reads every player from the Shuttle database and writes a flat JSON array to <paramref name="output"/>.
    /// </summary>
    /// <returns>A process exit code: 0 on success, 130 if cancelled, 1 on failure.</returns>
    public static async Task<int> RunAsync(
        FileInfo output,
        string? database,
        bool pretty,
        CancellationToken cancellationToken
    ) {
        ShuttleEnvironment.LoadDotEnv();

        var builder = Host.CreateApplicationBuilder();
        builder.AddShuttleDatabase(databaseName: database);

        var app = builder.Build();
        LinqToDBForEFTools.Initialize();

        var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Analysis");

        try {
            await app.EnsureShuttleDatabaseConnectivity(cancellationToken);

            using var scope = app.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ShlDbContext>();

            logger.LogInformation("Reading the PlayerInformation table");
            var players = await db.PlayerInformation
                .AsNoTracking()
                .IgnoreAutoIncludes()
                .OrderBy(p => p.PlayerId)
                .ToListAsync(cancellationToken);

            var records = players.Select(PlayerExportRecord.FromEntity).ToList();

            output.Directory?.Create();
            var options = PlayerExportJson.CreateOptions(pretty);
            await using (var stream = output.Open(FileMode.Create, FileAccess.Write, FileShare.None)) {
                await JsonSerializer.SerializeAsync(stream, records, options, cancellationToken);
            }

            logger.LogInformation("Wrote {Count} players to {Path}", records.Count, output.FullName);
            return 0;
        } catch (OperationCanceledException) {
            logger.LogWarning("Player information download cancelled");
            return 130;
        } catch (Exception ex) {
            logger.LogError(ex, "Player information download failed");
            return 1;
        }
    }
}
