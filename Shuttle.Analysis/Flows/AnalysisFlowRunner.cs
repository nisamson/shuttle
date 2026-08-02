using LinqToDB.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.ML;
using Shuttle.EFCore;

namespace Shuttle.Analysis.Flows;

/// <summary>
/// Drives an analysis flow end to end: prepare its data source, build the <see cref="AnalysisContext"/>,
/// resolve the named flow from the registry, run it, and map the outcome to a process exit code.
/// </summary>
/// <remarks>
/// The flow's <see cref="IDataAnalysisFlow.DataSource"/> decides the setup: a
/// <see cref="FlowDataSource.Csv"/> flow ingests the <paramref name="input"/> CSV (required) and never
/// touches the database, while a <see cref="FlowDataSource.Database"/> flow sets up an Azure SQL scope
/// (requiring the same environment/sign-in as the exporter) and hands the flow a scoped provider instead.
/// Exit codes mirror <c>PlayerInformationExporter.RunAsync</c>: <c>0</c> success, <c>130</c> cancelled,
/// <c>1</c> failure (including an unknown flow name, a missing required input, or a malformed input file).
/// </remarks>
public static class AnalysisFlowRunner {

    /// <summary>
    /// Runs the flow named <paramref name="flowName"/>, sourcing its data per the flow's
    /// <see cref="IDataAnalysisFlow.DataSource"/>.
    /// </summary>
    /// <param name="flowName">The name of the flow to run (see <see cref="AnalysisFlowRegistry"/>).</param>
    /// <param name="input">The CSV data file to ingest; required for CSV flows, ignored for database flows.</param>
    /// <param name="output">The directory for flow artifacts; created if it does not exist.</param>
    /// <param name="registry">The registry of available flows.</param>
    /// <param name="arguments">Flow-specific arguments (from <c>--arg key=value</c>).</param>
    /// <param name="database">Overrides the target Azure SQL database (catalog) name for database flows.</param>
    /// <param name="cancellationToken">A token to cancel the run.</param>
    /// <returns>A process exit code: 0 success, 130 cancelled, 1 failure.</returns>
    public static async Task<int> RunAsync(
        string flowName,
        FileInfo? input,
        DirectoryInfo output,
        AnalysisFlowRegistry registry,
        IReadOnlyDictionary<string, string> arguments,
        string? database,
        CancellationToken cancellationToken
    ) {
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(arguments);

        var loggerBuilder = Host.CreateApplicationBuilder();
        var loggerApp = loggerBuilder.Build();
        var logger = loggerApp.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Analysis");

        if (!registry.TryGet(flowName, out var flow) || flow is null) {
            var available = registry.Flows.Count == 0
                ? "(none registered)"
                : string.Join(", ", registry.Flows.Select(f => f.Name));
            logger.LogError("Unknown analysis flow '{Flow}'. Available flows: {Available}", flowName, available);
            return 1;
        }

        if (flow.DataSource == FlowDataSource.Csv && input is null) {
            logger.LogError("Analysis flow '{Flow}' requires an input CSV. Pass --input <file>.", flow.Name);
            return 1;
        }

        return flow.DataSource switch {
            FlowDataSource.Database => await RunDatabaseFlowAsync(
                flow, output, arguments, database, logger, cancellationToken),
            _ => await RunCsvFlowAsync(flow, input!, output, arguments, logger, cancellationToken),
        };
    }

    private static async Task<int> RunCsvFlowAsync(
        IDataAnalysisFlow flow,
        FileInfo input,
        DirectoryInfo output,
        IReadOnlyDictionary<string, string> arguments,
        ILogger logger,
        CancellationToken cancellationToken
    ) {
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

            return await RunFlowAsync(flow, context, logger, cancellationToken);
        } catch (OperationCanceledException) {
            logger.LogWarning("Analysis flow '{Flow}' cancelled", flow.Name);
            return 130;
        } catch (Exception ex) {
            logger.LogError(ex, "Analysis flow '{Flow}' failed", flow.Name);
            return 1;
        }
    }

    private static async Task<int> RunDatabaseFlowAsync(
        IDataAnalysisFlow flow,
        DirectoryInfo output,
        IReadOnlyDictionary<string, string> arguments,
        string? database,
        ILogger logger,
        CancellationToken cancellationToken
    ) {
        ShuttleEnvironment.LoadDotEnv();

        var builder = Host.CreateApplicationBuilder();
        builder.AddShuttleDatabase(databaseName: database);

        var app = builder.Build();
        LinqToDBForEFTools.Initialize();

        try {
            await app.EnsureShuttleDatabaseConnectivity(cancellationToken);

            output.Create();

            using var scope = app.Services.CreateScope();
            var mlContext = new MLContext();
            var context = new AnalysisContext(
                mlContext, data: null, input: null, output, logger, arguments, scope.ServiceProvider);

            return await RunFlowAsync(flow, context, logger, cancellationToken);
        } catch (OperationCanceledException) {
            logger.LogWarning("Analysis flow '{Flow}' cancelled", flow.Name);
            return 130;
        } catch (Exception ex) {
            logger.LogError(ex, "Analysis flow '{Flow}' failed", flow.Name);
            return 1;
        }
    }

    private static async Task<int> RunFlowAsync(
        IDataAnalysisFlow flow,
        AnalysisContext context,
        ILogger logger,
        CancellationToken cancellationToken
    ) {
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
    }
}
