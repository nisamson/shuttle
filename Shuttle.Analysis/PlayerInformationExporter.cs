using LinqToDB.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shuttle.EFCore;
using Shuttle.Shl.Api.Models.Common;

namespace Shuttle.Analysis;

/// <summary>
/// Downloads the current <c>PlayerInformation</c> table into a local JSON file for offline analysis.
/// </summary>
public static class PlayerInformationExporter {

    /// <summary>
    /// Reads every player from the Shuttle database and writes them to <paramref name="output"/> in
    /// the requested <paramref name="format"/> (a flat JSON array or a flat CSV table). When
    /// <paramref name="norm"/> is not <see cref="StatNorm.None"/>, each player's stat attributes are
    /// replaced in place with their normalized form. When <paramref name="positions"/> is non-null,
    /// only players whose position is in the set are exported.
    /// </summary>
    /// <returns>A process exit code: 0 on success, 130 if cancelled, 1 on failure.</returns>
    public static async Task<int> RunAsync(
        FileInfo output,
        string? database,
        ExportFormat format,
        StatNorm norm,
        IReadOnlySet<PlayerPosition>? positions,
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

            var query = db.PlayerInformation
                .AsNoTracking()
                .IgnoreAutoIncludes()
                .AsQueryable();

            if (positions is { Count: > 0 }) {
                var positionValues = positions.ToArray();
                query = query.Where(p => positionValues.Contains(p.Position));
                logger.LogInformation(
                    "Filtering to positions: {Positions}",
                    string.Join(", ", positions.Select(p => p.ToShortString())));
            }

            logger.LogInformation("Reading the PlayerInformation table");
            var players = await query
                .OrderBy(p => p.PlayerId)
                .ToListAsync(cancellationToken);

            var records = players.Select(PlayerExportRecord.FromEntity).ToList();

            output.Directory?.Create();
            await using (var stream = output.Open(FileMode.Create, FileAccess.Write, FileShare.None)) {
                if (format == ExportFormat.Csv) {
                    await PlayerCsvExport.WriteAsync(stream, records, norm, cancellationToken);
                } else {
                    await PlayerJsonExport.WriteAsync(stream, records, pretty, norm, cancellationToken);
                }
            }

            logger.LogInformation(
                "Wrote {Count} players to {Path} ({Format}, norm: {Norm})",
                records.Count,
                output.FullName,
                format,
                norm);
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
