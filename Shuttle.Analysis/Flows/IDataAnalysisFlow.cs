namespace Shuttle.Analysis.Flows;

/// <summary>
/// A named, self-describing analysis scenario that consumes ingested data and produces a result.
/// </summary>
/// <remarks>
/// Prefer deriving from <see cref="AnalysisFlowBase"/> rather than implementing this interface directly:
/// it supplies the default <see cref="DataSource"/> and keeps the contract stable as it grows. Register
/// the implementation in <see cref="AnalysisFlowRegistry"/> so it becomes selectable from the
/// <c>analyze</c> CLI command.
/// <para>
/// A <see cref="FlowDataSource.Csv"/> flow receives the export file, pre-ingested by the framework
/// (<see cref="CsvDataIngestor"/>), through <see cref="AnalysisContext.Data"/> and only has to project
/// the columns it needs into its own ML.NET schema. A <see cref="FlowDataSource.Database"/> flow instead
/// pulls data itself during <see cref="RunAsync"/> via the scoped <see cref="AnalysisContext.Services"/>.
/// </para>
/// </remarks>
public interface IDataAnalysisFlow {

    /// <summary>The flow's unique, CLI-friendly name (kebab-case), used to select it via <c>--flow</c>.</summary>
    string Name { get; }

    /// <summary>A short human-readable description shown when listing available flows.</summary>
    string Description { get; }

    /// <summary>
    /// Where this flow gets its input data, which determines whether the runner ingests the
    /// <c>--input</c> CSV (<see cref="FlowDataSource.Csv"/>) or sets up database access
    /// (<see cref="FlowDataSource.Database"/>) before running the flow.
    /// </summary>
    FlowDataSource DataSource { get; }

    /// <summary>
    /// Runs the flow against the ingested data.
    /// </summary>
    /// <param name="context">The execution context (ML context, data, paths, logger).</param>
    /// <param name="cancellationToken">A token to cancel the run.</param>
    /// <returns>The flow result.</returns>
    Task<AnalysisFlowResult> RunAsync(AnalysisContext context, CancellationToken cancellationToken);
}
