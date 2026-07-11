namespace Shuttle.Analysis.Flows;

/// <summary>
/// The outcome of running an <see cref="IDataAnalysisFlow"/>.
/// </summary>
/// <remarks>
/// Intentionally small for now (success plus an optional human-readable summary). It is a class so
/// future scenarios can extend the result with metrics, artifact paths, or structured output without
/// breaking the <see cref="IDataAnalysisFlow"/> contract.
/// </remarks>
public sealed class AnalysisFlowResult {

    private AnalysisFlowResult(bool succeeded, string? summary) {
        Succeeded = succeeded;
        Summary = summary;
    }

    /// <summary>Whether the flow completed successfully.</summary>
    public bool Succeeded { get; }

    /// <summary>An optional human-readable summary of what the flow did.</summary>
    public string? Summary { get; }

    /// <summary>Creates a successful result with an optional summary message.</summary>
    public static AnalysisFlowResult Success(string? summary = null) => new(true, summary);

    /// <summary>Creates a failed result with a message describing the failure.</summary>
    public static AnalysisFlowResult Failure(string summary) => new(false, summary);
}
