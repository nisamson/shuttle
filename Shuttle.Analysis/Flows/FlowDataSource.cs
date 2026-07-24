namespace Shuttle.Analysis.Flows;

/// <summary>
/// Identifies where an <see cref="IDataAnalysisFlow"/> gets its input data, so the
/// <see cref="AnalysisFlowRunner"/> can set up only what the flow needs.
/// </summary>
/// <remarks>
/// A <see cref="Csv"/> flow consumes the pre-ingested export CSV (<c>--input</c>) surfaced as
/// <see cref="AnalysisContext.Data"/>; the runner never touches the database. A <see cref="Database"/>
/// flow instead pulls data itself during the analysis phase through the scoped
/// <see cref="AnalysisContext.Services"/> (for example, querying <c>ShlDbContext</c>), so the Azure SQL
/// connection and sign-in are only required for these flows.
/// </remarks>
public enum FlowDataSource {

    /// <summary>The flow consumes the pre-ingested export CSV (<c>--input</c>) via <see cref="AnalysisContext.Data"/>.</summary>
    Csv,

    /// <summary>The flow queries the database itself during analysis via the scoped <see cref="AnalysisContext.Services"/>.</summary>
    Database,
}
