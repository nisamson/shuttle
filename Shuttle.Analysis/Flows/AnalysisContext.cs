using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.ML;

namespace Shuttle.Analysis.Flows;

/// <summary>
/// The execution context handed to an <see cref="IDataAnalysisFlow"/> when it runs.
/// </summary>
/// <remarks>
/// Carries everything a flow needs. Always present: the shared ML.NET <see cref="MLContext"/> (a single
/// instance per run so its random seed and component registrations are consistent), an output directory
/// for any artifacts the flow produces (models, reports, predictions), a <see cref="ILogger"/> for
/// progress/diagnostics, and the flow <see cref="Arguments"/>.
/// <para>
/// The remaining members depend on the flow's <see cref="IDataAnalysisFlow.DataSource"/>. For a
/// <see cref="FlowDataSource.Csv"/> flow, <see cref="Data"/> and <see cref="Input"/> carry the ingested
/// export file and <see cref="Services"/> is <c>null</c>. For a <see cref="FlowDataSource.Database"/>
/// flow, <see cref="Services"/> is a scoped provider the flow uses to resolve <c>ShlDbContext</c> and run
/// its own queries, while <see cref="Data"/> and <see cref="Input"/> are <c>null</c>. Use
/// <see cref="RequireData"/> / <see cref="RequireServices"/> to fetch the appropriate one with a clear
/// error if a flow is wired to the wrong source.
/// </para>
/// </remarks>
public sealed class AnalysisContext {

    public AnalysisContext(
        MLContext mlContext,
        IngestedData? data,
        FileInfo? input,
        DirectoryInfo output,
        ILogger logger,
        IReadOnlyDictionary<string, string>? arguments = null,
        IServiceProvider? services = null
    ) {
        MLContext = mlContext ?? throw new ArgumentNullException(nameof(mlContext));
        Data = data;
        Input = input;
        Output = output ?? throw new ArgumentNullException(nameof(output));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        Arguments = arguments ?? FlowArguments.Empty;
        Services = services;
    }

    /// <summary>The shared ML.NET context for building data views, pipelines, and models.</summary>
    public MLContext MLContext { get; }

    /// <summary>
    /// The tabular data ingested from <see cref="Input"/>, or <c>null</c> for a
    /// <see cref="FlowDataSource.Database"/> flow. Prefer <see cref="RequireData"/> in CSV flows.
    /// </summary>
    public IngestedData? Data { get; }

    /// <summary>The input data file that was ingested, or <c>null</c> for a database-backed flow.</summary>
    public FileInfo? Input { get; }

    /// <summary>A directory into which the flow may write artifacts (created before the flow runs).</summary>
    public DirectoryInfo Output { get; }

    /// <summary>A logger for flow progress and diagnostics.</summary>
    public ILogger Logger { get; }

    /// <summary>Flow-specific arguments (from <c>--arg key=value</c>), keyed case-insensitively.</summary>
    public IReadOnlyDictionary<string, string> Arguments { get; }

    /// <summary>
    /// A scoped service provider for a <see cref="FlowDataSource.Database"/> flow to resolve services
    /// such as <c>ShlDbContext</c>, or <c>null</c> for a CSV flow. Prefer <see cref="RequireServices"/>
    /// or <see cref="GetRequiredService{T}"/> in database flows.
    /// </summary>
    public IServiceProvider? Services { get; }

    /// <summary>
    /// Returns the ingested <see cref="Data"/>, throwing if it is absent (i.e. the flow declared
    /// <see cref="FlowDataSource.Database"/> but tried to read CSV data).
    /// </summary>
    public IngestedData RequireData() =>
        Data ?? throw new InvalidOperationException(
            "No ingested CSV data is available. This flow must declare DataSource = FlowDataSource.Csv "
            + "and be run with --input to read AnalysisContext.Data.");

    /// <summary>
    /// Returns the scoped <see cref="Services"/>, throwing if they are absent (i.e. the flow declared
    /// <see cref="FlowDataSource.Csv"/> but tried to reach the database).
    /// </summary>
    public IServiceProvider RequireServices() =>
        Services ?? throw new InvalidOperationException(
            "No database services are available. This flow must declare DataSource = "
            + "FlowDataSource.Database to access AnalysisContext.Services.");

    /// <summary>Resolves a required service from the scoped <see cref="Services"/> (database flows).</summary>
    public T GetRequiredService<T>() where T : notnull =>
        RequireServices().GetRequiredService<T>();

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
