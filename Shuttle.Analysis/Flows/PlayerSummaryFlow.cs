using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shuttle.EFCore;
using Shuttle.Shl.Api.Models.Common;

namespace Shuttle.Analysis.Flows;

/// <summary>
/// A reference <see cref="FlowDataSource.Database"/> flow: it pulls the <c>PlayerInformation</c> table
/// straight from the database during the analysis phase and writes a per-position summary
/// (count and average total TPE) to <c>player-summary.csv</c>.
/// </summary>
/// <remarks>
/// This flow exists both as a small, useful report and as the documented template for database-backed
/// scenarios. Instead of consuming a pre-ingested CSV, it declares
/// <see cref="FlowDataSource.Database"/> and resolves <see cref="ShlDbContext"/> from the scoped
/// <see cref="AnalysisContext.Services"/> — the same access the exporter uses — so future flows can run
/// arbitrary EF/linq2db queries (income, TPE timelines, activity) rather than the flat export shape.
/// </remarks>
public sealed class PlayerSummaryFlow : AnalysisFlowBase {

    public override string Name => "player-summary";

    public override string Description =>
        "Reads PlayerInformation from the database and reports player count and average TPE per position.";

    public override FlowDataSource DataSource => FlowDataSource.Database;

    public override async Task<AnalysisFlowResult> RunAsync(AnalysisContext context, CancellationToken cancellationToken) {
        ArgumentNullException.ThrowIfNull(context);

        var db = context.GetRequiredService<ShlDbContext>();

        context.Logger.LogInformation("Reading the PlayerInformation table");
        var summaries = await db.PlayerInformation
            .AsNoTracking()
            .IgnoreAutoIncludes()
            .GroupBy(p => p.Position)
            .Select(g => new PositionSummary(g.Key, g.Count(), g.Average(p => (double)p.TotalTpe)))
            .ToListAsync(cancellationToken);

        if (summaries.Count == 0) {
            return AnalysisFlowResult.Failure("The PlayerInformation table returned no rows.");
        }

        var ordered = summaries.OrderBy(s => s.Position).ToList();

        var rows = ordered
            .Select(s => (IReadOnlyList<string?>)[
                s.Position.ToShortString(),
                s.Count.ToString(CultureInfo.InvariantCulture),
                s.AverageTotalTpe.ToString("F1", CultureInfo.InvariantCulture),
            ])
            .ToList();

        var outputFile = new FileInfo(Path.Combine(context.Output.FullName, "player-summary.csv"));
        await CsvResultWriter.WriteFileAsync(
            outputFile,
            ["position", "count", "avgTotalTpe"],
            rows,
            cancellationToken);

        var total = ordered.Sum(s => s.Count);
        return AnalysisFlowResult.Success(
            $"Summarized {total} players across {ordered.Count} positions. Wrote {outputFile.FullName}.");
    }

    private sealed record PositionSummary(PlayerPosition Position, int Count, double AverageTotalTpe);
}
