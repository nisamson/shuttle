using System.Globalization;
using Microsoft.Extensions.Logging;
using Shuttle.Analysis.Flows.Recruitment;
using Shuttle.EFCore.Recruitment;

namespace Shuttle.Analysis.Flows;

/// <summary>
/// A database-backed flow that analyzes player <em>recruitment</em>. Recruiters are classified as SHL
/// members ("player" recruiters), external/generic sources (e.g. Google, Reddit), self ("Myself"), or
/// none, and recruitment is consolidated by recruited member. For each recruiter it reports how many
/// members they recruited and those members' combined full-career TPE.
/// </summary>
/// <remarks>
/// The data query, classification, and aggregation live in <see cref="IRecruitmentAnalysisService"/>
/// (in <c>Shuttle.EFCore</c>) so the API server can reuse them; this flow only renders the result as
/// CSVs, GraphViz <c>.dot</c> files, and bar-graph images.
/// <para>
/// Arguments (via <c>--arg key=value</c>):
/// <list type="bullet">
///   <item><c>top</c> (optional, default 20): limit the bar graphs to the top N recruiters.</item>
///   <item><c>format</c> (optional, <c>png</c> | <c>svg</c>, default <c>png</c>): bar-graph image format.</item>
/// </list>
/// </para>
/// </remarks>
public sealed class RecruitmentFlow : AnalysisFlowBase {

    private const int DefaultTop = 20;

    public override string Name => "recruitment";

    public override string Description =>
        "Classifies player recruiters (SHL members, external, self, none) and reports, per recruiter, "
        + "the members recruited and their combined career TPE, as CSVs, GraphViz DOT, and bar graphs.";

    public override FlowDataSource DataSource => FlowDataSource.Database;

    public override async Task<AnalysisFlowResult> RunAsync(AnalysisContext context, CancellationToken cancellationToken) {
        ArgumentNullException.ThrowIfNull(context);

        var top = context.GetOptionalInt("top", DefaultTop);
        if (top < 1) {
            return AnalysisFlowResult.Failure($"Argument 'top' must be >= 1, but was {top}.");
        }

        string extension;
        if (context.TryGetArgument("format", out var format) && !string.IsNullOrWhiteSpace(format)) {
            extension = format!.Trim().ToLowerInvariant();
            if (extension is not ("png" or "svg")) {
                return AnalysisFlowResult.Failure($"Argument 'format' must be 'png' or 'svg', but was '{format}'.");
            }
        } else {
            extension = "png";
        }

        var service = context.GetRequiredService<IRecruitmentAnalysisService>();

        context.Logger.LogInformation("Analyzing recruitment from the database");
        var analysis = await service.GetRecruitmentAnalysisAsync(cancellationToken);

        if (analysis.Tallies.Count == 0) {
            return AnalysisFlowResult.Failure("No players were found to analyze recruitment.");
        }

        context.Output.Create();

        await WriteCsvsAsync(context, analysis, cancellationToken);
        await WriteDotAsync(context, analysis, cancellationToken);
        WriteCharts(context, analysis, top, extension);

        var totalUsers = analysis.CategorySummary.Sum(c => c.RecruitedUsers);
        return AnalysisFlowResult.Success(
            $"Analyzed {totalUsers} recruited members across {analysis.Tallies.Count} recruiters. "
            + $"Wrote CSVs, DOT graphs, and bar graphs to {context.Output.FullName}.");
    }

    private static async Task WriteCsvsAsync(
        AnalysisContext context,
        RecruitmentAnalysis analysis,
        CancellationToken cancellationToken
    ) {
        var countRows = analysis.Tallies
            .Select(t => (IReadOnlyList<string?>)[
                t.Recruiter,
                t.Category.ToString(),
                t.RecruitedUsers.ToString(CultureInfo.InvariantCulture),
                t.TotalCareerTpe.ToString(CultureInfo.InvariantCulture),
                t.LineageUsers.ToString(CultureInfo.InvariantCulture),
                t.LineageCareerTpe.ToString(CultureInfo.InvariantCulture),
            ])
            .ToList();
        await CsvResultWriter.WriteFileAsync(
            new FileInfo(Path.Combine(context.Output.FullName, "recruiter-counts.csv")),
            ["recruiter", "category", "recruitedUsers", "totalCareerTpe", "lineageUsers", "lineageCareerTpe"],
            countRows,
            cancellationToken);

        var edgeRows = analysis.Edges
            .Select(e => (IReadOnlyList<string?>)[
                e.Recruiter,
                e.Category.ToString(),
                e.UserId.ToString(CultureInfo.InvariantCulture),
                e.Username,
                e.CareerTpe.ToString(CultureInfo.InvariantCulture),
            ])
            .ToList();
        await CsvResultWriter.WriteFileAsync(
            new FileInfo(Path.Combine(context.Output.FullName, "recruitment-edges.csv")),
            ["recruiter", "category", "userId", "username", "careerTpe"],
            edgeRows,
            cancellationToken);

        var summaryRows = analysis.CategorySummary
            .Select(c => (IReadOnlyList<string?>)[
                c.Category.ToString(),
                c.DistinctRecruiters.ToString(CultureInfo.InvariantCulture),
                c.RecruitedUsers.ToString(CultureInfo.InvariantCulture),
                c.TotalCareerTpe.ToString(CultureInfo.InvariantCulture),
            ])
            .ToList();
        await CsvResultWriter.WriteFileAsync(
            new FileInfo(Path.Combine(context.Output.FullName, "recruiter-category-summary.csv")),
            ["category", "distinctRecruiters", "recruitedUsers", "totalCareerTpe"],
            summaryRows,
            cancellationToken);
    }

    private static async Task WriteDotAsync(
        AnalysisContext context,
        RecruitmentAnalysis analysis,
        CancellationToken cancellationToken
    ) {
        await File.WriteAllTextAsync(
            Path.Combine(context.Output.FullName, "recruitment-full.dot"),
            RecruitmentDotWriter.BuildFullGraph(analysis),
            cancellationToken);

        await File.WriteAllTextAsync(
            Path.Combine(context.Output.FullName, "player-recruiter-network.dot"),
            RecruitmentDotWriter.BuildPlayerRecruiterNetwork(analysis),
            cancellationToken);
    }

    private static void WriteCharts(
        AnalysisContext context,
        RecruitmentAnalysis analysis,
        int top,
        string extension
    ) {
        var topByUsers = analysis.Tallies
            .Take(top)
            .Select(t => new RecruitmentChartWriter.BarItem(RecruiterLabel(t.Recruiter, t.Category), t.RecruitedUsers))
            .ToList();
        RecruitmentChartWriter.RenderHorizontalBars(
            new FileInfo(Path.Combine(context.Output.FullName, $"top-recruiters.{extension}")),
            $"Top {topByUsers.Count} recruiters by members recruited",
            "Recruited members",
            topByUsers,
            ScottPlot.Colors.SteelBlue);

        var topByTpe = analysis.Tallies
            .OrderByDescending(t => t.TotalCareerTpe)
            .ThenBy(t => t.Recruiter, StringComparer.OrdinalIgnoreCase)
            .Take(top)
            .Select(t => new RecruitmentChartWriter.BarItem(RecruiterLabel(t.Recruiter, t.Category), t.TotalCareerTpe))
            .ToList();
        RecruitmentChartWriter.RenderHorizontalBars(
            new FileInfo(Path.Combine(context.Output.FullName, $"top-recruiters-tpe.{extension}")),
            $"Top {topByTpe.Count} recruiters by recruited members' career TPE",
            "Combined career TPE",
            topByTpe,
            ScottPlot.Colors.MediumPurple);

        var topByLineageTpe = analysis.Tallies
            .OrderByDescending(t => t.LineageCareerTpe)
            .ThenBy(t => t.Recruiter, StringComparer.OrdinalIgnoreCase)
            .Take(top)
            .Select(t => new RecruitmentChartWriter.BarItem(RecruiterLabel(t.Recruiter, t.Category), t.LineageCareerTpe))
            .ToList();
        RecruitmentChartWriter.RenderHorizontalBars(
            new FileInfo(Path.Combine(context.Output.FullName, $"top-recruiters-lineage-tpe.{extension}")),
            $"Top {topByLineageTpe.Count} recruiters by full lineage career TPE",
            "Combined lineage career TPE",
            topByLineageTpe,
            ScottPlot.Colors.SeaGreen);

        RecruitmentChartWriter.RenderCategoryBreakdown(
            new FileInfo(Path.Combine(context.Output.FullName, $"recruiter-category-breakdown.{extension}")),
            analysis.CategorySummary);
    }

    private static string RecruiterLabel(string recruiter, RecruiterCategory category) => category switch {
        RecruiterCategory.Self => "(self)",
        RecruiterCategory.None => "(none)",
        _ => recruiter,
    };
}
