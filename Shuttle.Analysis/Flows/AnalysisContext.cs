using Microsoft.Extensions.Logging;
using Microsoft.ML;

namespace Shuttle.Analysis.Flows;

/// <summary>
/// The execution context handed to an <see cref="IDataAnalysisFlow"/> when it runs.
/// </summary>
/// <remarks>
/// Carries everything a flow needs: the shared ML.NET <see cref="MLContext"/> (a single instance per
/// run so its random seed and component registrations are consistent), the <see cref="IngestedData"/>
/// read from the input file, the input file itself, an output directory for any artifacts the flow
/// produces (models, reports, predictions), and a <see cref="ILogger"/> for progress/diagnostics.
/// </remarks>
public sealed class AnalysisContext {

    public AnalysisContext(
        MLContext mlContext,
        IngestedData data,
        FileInfo input,
        DirectoryInfo output,
        ILogger logger,
        IReadOnlyDictionary<string, string>? arguments = null
    ) {
        MLContext = mlContext ?? throw new ArgumentNullException(nameof(mlContext));
        Data = data ?? throw new ArgumentNullException(nameof(data));
        Input = input ?? throw new ArgumentNullException(nameof(input));
        Output = output ?? throw new ArgumentNullException(nameof(output));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        Arguments = arguments ?? FlowArguments.Empty;
    }

    /// <summary>The shared ML.NET context for building data views, pipelines, and models.</summary>
    public MLContext MLContext { get; }

    /// <summary>The tabular data ingested from <see cref="Input"/>.</summary>
    public IngestedData Data { get; }

    /// <summary>The input data file that was ingested.</summary>
    public FileInfo Input { get; }

    /// <summary>A directory into which the flow may write artifacts (created before the flow runs).</summary>
    public DirectoryInfo Output { get; }

    /// <summary>A logger for flow progress and diagnostics.</summary>
    public ILogger Logger { get; }

    /// <summary>Flow-specific arguments (from <c>--arg key=value</c>), keyed case-insensitively.</summary>
    public IReadOnlyDictionary<string, string> Arguments { get; }

    /// <summary>Attempts to read the raw string value of an argument.</summary>
    public bool TryGetArgument(string key, out string? value) {
        if (Arguments.TryGetValue(key, out var raw)) {
            value = raw;
            return true;
        }

        value = null;
        return false;
    }

    /// <summary>Reads a required integer argument (see <see cref="FlowArguments.GetRequiredInt"/>).</summary>
    public int GetRequiredInt(string key) => FlowArguments.GetRequiredInt(Arguments, key);

    /// <summary>Reads an optional integer argument (see <see cref="FlowArguments.GetOptionalInt"/>).</summary>
    public int GetOptionalInt(string key, int defaultValue) =>
        FlowArguments.GetOptionalInt(Arguments, key, defaultValue);
}
