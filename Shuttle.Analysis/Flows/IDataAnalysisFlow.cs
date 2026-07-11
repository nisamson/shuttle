namespace Shuttle.Analysis.Flows;

/// <summary>
/// A named, self-describing analysis scenario that consumes ingested data and produces a result.
/// </summary>
/// <remarks>
/// Implement this interface to add a new analysis scenario (for example, an ML.NET model that predicts
/// draft success or classifies player archetypes). Register the implementation in
/// <see cref="AnalysisFlowRegistry"/> so it becomes selectable from the <c>analyze</c> CLI command.
/// The ingestion of the input file is handled by the framework (<see cref="CsvDataIngestor"/>); a flow
/// receives the parsed data through the <see cref="AnalysisContext"/> and only has to project the
/// columns it needs into its own ML.NET schema.
/// </remarks>
public interface IDataAnalysisFlow {

    /// <summary>The flow's unique, CLI-friendly name (kebab-case), used to select it via <c>--flow</c>.</summary>
    string Name { get; }

    /// <summary>A short human-readable description shown when listing available flows.</summary>
    string Description { get; }

    /// <summary>
    /// Runs the flow against the ingested data.
    /// </summary>
    /// <param name="context">The execution context (ML context, data, paths, logger).</param>
    /// <param name="cancellationToken">A token to cancel the run.</param>
    /// <returns>The flow result.</returns>
    Task<AnalysisFlowResult> RunAsync(AnalysisContext context, CancellationToken cancellationToken);
}
