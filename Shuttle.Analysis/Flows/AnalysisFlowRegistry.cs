namespace Shuttle.Analysis.Flows;

/// <summary>
/// Holds the set of available <see cref="IDataAnalysisFlow"/> implementations, keyed by name.
/// </summary>
/// <remarks>
/// This is the single, obvious place to register new analysis scenarios: add the flow to
/// <see cref="CreateDefault"/> and it immediately becomes selectable via <c>analyze --flow &lt;name&gt;</c>
/// and visible via <c>analyze --list</c>. It intentionally starts empty (the framework ships without a
/// concrete scenario). Lookups are case-insensitive.
/// </remarks>
public sealed class AnalysisFlowRegistry {

    private readonly IReadOnlyDictionary<string, IDataAnalysisFlow> flowsByName;

    /// <summary>
    /// Creates a registry from the given flows.
    /// </summary>
    /// <exception cref="ArgumentException">Two flows share the same (case-insensitive) name.</exception>
    public AnalysisFlowRegistry(IEnumerable<IDataAnalysisFlow> flows) {
        ArgumentNullException.ThrowIfNull(flows);

        var map = new Dictionary<string, IDataAnalysisFlow>(StringComparer.OrdinalIgnoreCase);
        foreach (var flow in flows) {
            ArgumentNullException.ThrowIfNull(flow);
            if (!map.TryAdd(flow.Name, flow)) {
                throw new ArgumentException($"Duplicate analysis flow name: '{flow.Name}'.", nameof(flows));
            }
        }

        flowsByName = map;
    }

    /// <summary>All registered flows, ordered by name.</summary>
    public IReadOnlyList<IDataAnalysisFlow> Flows =>
        [.. flowsByName.Values.OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase)];

    /// <summary>Attempts to resolve a flow by its (case-insensitive) name.</summary>
    public bool TryGet(string name, out IDataAnalysisFlow? flow) {
        if (string.IsNullOrWhiteSpace(name)) {
            flow = null;
            return false;
        }

        return flowsByName.TryGetValue(name.Trim(), out flow);
    }

    /// <summary>
    /// Creates the default registry containing every built-in flow. Register new flows here.
    /// </summary>
    public static AnalysisFlowRegistry CreateDefault() =>
        new([
            new KMeansCentroidFlow(),
        ]);
}
