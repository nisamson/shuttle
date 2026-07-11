using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.ML;

namespace Shuttle.Analysis.Flows;

/// <summary>
/// Drives an analysis flow end to end: ingest the input CSV, build the <see cref="AnalysisContext"/>,
/// resolve the named flow from the registry, run it, and map the outcome to a process exit code.
/// </summary>
/// <remarks>
/// Exit codes mirror <c>PlayerInformationExporter.RunAsync</c>: <c>0</c> success, <c>130</c> cancelled,
/// <c>1</c> failure (including an unknown flow name or a malformed input file).
/// </remarks>
public static class AnalysisFlowRunner {

    /// <summary>
    /// Runs the flow named <paramref name="flowName"/> against <paramref name="input"/>.
    /// </summary>
    /// <param name="flowName">The name of the flow to run (see <see cref="AnalysisFlowRegistry"/>).</param>
    /// <param name="input">The CSV data file to ingest.</param>
    /// <param name="output">The directory for flow artifacts; created if it does not exist.</param>
    /// <param name="registry">The registry of available flows.</param>
    /// <param name="arguments">Flow-specific arguments (from <c>--arg key=value</c>).</param>
    /// <param name="cancellationToken">A token to cancel the run.</param>
    /// <returns>A process exit code: 0 success, 130 cancelled, 1 failure.</returns>
    public static async Task<int> RunAsync(
        string flowName,
        FileInfo input,
        DirectoryInfo output,
        AnalysisFlowRegistry registry,
        IReadOnlyDictionary<string, string> arguments,
        CancellationToken cancellationToken
    ) {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(arguments);

        var builder = Host.CreateApplicationBuilder();
        var app = builder.Build();
        var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Analysis");

        if (!registry.TryGet(flowName, out var flow) || flow is null) {
            var available = registry.Flows.Count == 0
                ? "(none registered)"
                : string.Join(", ", registry.Flows.Select(f => f.Name));
            logger.LogError("Unknown analysis flow '{Flow}'. Available flows: {Available}", flowName, available);
            return 1;
        }

        try {
            logger.LogInformation("Ingesting {Path}", input.FullName);
            var data = await CsvDataIngestor.IngestAsync(input, cancellationToken);
            logger.LogInformation(
                "Ingested {Rows} rows across {Columns} columns",
                data.RowCount,
                data.Columns.Count);

            output.Create();

            var mlContext = new MLContext();
            var context = new AnalysisContext(mlContext, data, input, output, logger, arguments);

            logger.LogInformation("Running analysis flow '{Flow}'", flow.Name);
            var result = await flow.RunAsync(context, cancellationToken);

            if (result.Succeeded) {
                logger.LogInformation(
                    "Analysis flow '{Flow}' completed. {Summary}",
                    flow.Name,
                    result.Summary ?? string.Empty);
                return 0;
            }

            logger.LogError("Analysis flow '{Flow}' failed. {Summary}", flow.Name, result.Summary);
            return 1;
        } catch (OperationCanceledException) {
            logger.LogWarning("Analysis flow '{Flow}' cancelled", flow.Name);
            return 130;
        } catch (Exception ex) {
            logger.LogError(ex, "Analysis flow '{Flow}' failed", flow.Name);
            return 1;
        }
    }
}
