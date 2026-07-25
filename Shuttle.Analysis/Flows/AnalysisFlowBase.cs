namespace Shuttle.Analysis.Flows;

/// <summary>
/// Base class for analysis scenarios. Derive from this to add a new flow: supply a kebab-case
/// <see cref="Name"/>, a short <see cref="Description"/>, and the <see cref="RunAsync"/> body, and
/// override <see cref="DataSource"/> only when the flow needs to pull from the database instead of the
/// export CSV.
/// </summary>
/// <remarks>
/// <see cref="DataSource"/> defaults to <see cref="FlowDataSource.Csv"/>, so an existing CSV-based flow
/// works unchanged. Override it to return <see cref="FlowDataSource.Database"/> to have the runner set up
/// database access (see <see cref="AnalysisContext.Services"/>) and skip CSV ingestion.
/// </remarks>
public abstract class AnalysisFlowBase : IDataAnalysisFlow {

    /// <inheritdoc/>
    public abstract string Name { get; }

    /// <inheritdoc/>
    public abstract string Description { get; }

    /// <inheritdoc/>
    /// <remarks>Defaults to <see cref="FlowDataSource.Csv"/>; override for database-backed flows.</remarks>
    public virtual FlowDataSource DataSource => FlowDataSource.Csv;

    /// <inheritdoc/>
    public abstract Task<AnalysisFlowResult> RunAsync(AnalysisContext context, CancellationToken cancellationToken);
}
