using System.Globalization;
using Microsoft.Extensions.Logging;
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Trainers;

namespace Shuttle.Analysis.Flows;

/// <summary>
/// Clusters players by their stat-attribute vectors using ML.NET k-means and reports, per cluster,
/// the actual centroid vector and the medoid (the real player nearest that centroid).
/// </summary>
/// <remarks>
/// <para>
/// Skaters and goaltenders have disjoint attribute sets, so they are clustered separately: the flow
/// partitions the ingested rows into a skater group (rows carrying <c>skaterAttributes.*</c>) and a
/// goaltender group (rows carrying <c>goaltenderAttributes.*</c>), and runs k-means over each present
/// group using that group's attribute columns as features.
/// </para>
/// <para>
/// Arguments (via <c>--arg key=value</c>):
/// <list type="bullet">
///   <item><c>k</c> (required, integer &gt;= 1): number of clusters per group. <c>k=1</c> yields a
///   single cluster over the whole group (centroid = mean vector, medoid = nearest player), computed
///   directly since ML.NET k-means requires at least two clusters.</item>
///   <item><c>seed</c> (optional, integer): seeds a dedicated <see cref="MLContext"/> for
///   reproducible clustering.</item>
/// </list>
/// </para>
/// <para>
/// For each processed group it writes two CSV files into the output directory:
/// <c>{group}-medoids.csv</c> (<c>clusterId, clusterSize, playerId, name, &lt;stats…&gt;</c>) and
/// <c>{group}-centroids.csv</c> (<c>clusterId, clusterSize, &lt;stats…&gt;</c>).
/// </para>
/// </remarks>
public sealed class KMeansCentroidFlow : IDataAnalysisFlow {

    private const string SkaterPrefix = "skaterAttributes.";
    private const string GoaltenderPrefix = "goaltenderAttributes.";
    private const string PlayerIdColumn = "playerId";
    private const string NameColumn = "name";

    public string Name => "kmeans-centroids";

    public string Description =>
        "K-means clustering of players by stat vector; reports each cluster's centroid and medoid player.";

    public async Task<AnalysisFlowResult> RunAsync(AnalysisContext context, CancellationToken cancellationToken) {
        ArgumentNullException.ThrowIfNull(context);

        int k;
        try {
            k = context.GetRequiredInt("k");
        } catch (ArgumentException ex) {
            return AnalysisFlowResult.Failure(ex.Message);
        }

        if (k < 1) {
            return AnalysisFlowResult.Failure($"Argument 'k' must be >= 1, but was {k}.");
        }

        var seed = context.TryGetArgument("seed", out _) ? context.GetOptionalInt("seed", 0) : (int?)null;
        var mlContext = seed is { } s ? new MLContext(s) : context.MLContext;

        var skaterFeatures = FeatureColumns(context.Data, SkaterPrefix);
        var goaltenderFeatures = FeatureColumns(context.Data, GoaltenderPrefix);

        var skaterRows = Partition(context.Data, skaterFeatures);
        var goaltenderRows = Partition(context.Data, goaltenderFeatures);

        var processed = new List<string>();

        var skaterFiles = await ProcessGroupAsync(
            context, mlContext, "skater", skaterFeatures, skaterRows, k, cancellationToken);
        if (skaterFiles) {
            processed.Add("skater");
        }

        var goaltenderFiles = await ProcessGroupAsync(
            context, mlContext, "goaltender", goaltenderFeatures, goaltenderRows, k, cancellationToken);
        if (goaltenderFiles) {
            processed.Add("goaltender");
        }

        if (processed.Count == 0) {
            return AnalysisFlowResult.Failure(
                $"No player group could be clustered with k={k}. Ensure the input contains at least {k} "
                + "skaters or goaltenders with stat attributes.");
        }

        return AnalysisFlowResult.Success(
            $"Clustered {string.Join(" and ", processed)} into k={k} clusters. "
            + $"Wrote medoid/centroid reports to {context.Output.FullName}.");
    }

    private static IReadOnlyList<string> FeatureColumns(IngestedData data, string prefix) =>
        [.. data.Columns.Where(c => c.StartsWith(prefix, StringComparison.Ordinal))];

    private static IReadOnlyList<IReadOnlyDictionary<string, string?>> Partition(
        IngestedData data,
        IReadOnlyList<string> featureColumns
    ) {
        if (featureColumns.Count == 0) {
            return [];
        }

        return [.. data.Rows.Where(row => featureColumns.Any(c => row.TryGetValue(c, out var v) && v is not null))];
    }

    private async Task<bool> ProcessGroupAsync(
        AnalysisContext context,
        MLContext mlContext,
        string group,
        IReadOnlyList<string> featureColumns,
        IReadOnlyList<IReadOnlyDictionary<string, string?>> rows,
        int k,
        CancellationToken cancellationToken
    ) {
        if (featureColumns.Count == 0 || rows.Count == 0) {
            return false;
        }

        // Build feature vectors, skipping rows with any missing/non-numeric attribute cell.
        var members = new List<GroupMember>(rows.Count);
        foreach (var row in rows) {
            if (TryBuildVector(row, featureColumns, out var vector)) {
                members.Add(new GroupMember(row, vector));
            } else {
                context.Logger.LogWarning(
                    "Skipping {Group} player {Player} with missing or non-numeric attributes",
                    group,
                    row.TryGetValue(PlayerIdColumn, out var id) ? id : "(unknown)");
            }
        }

        if (members.Count < k) {
            context.Logger.LogWarning(
                "Skipping {Group} group: {Count} usable players is fewer than k={K}",
                group,
                members.Count,
                k);
            return false;
        }

        var clusters = k == 1
            ? ClusterSingle(members, featureColumns.Count)
            : ClusterKMeans(mlContext, members, featureColumns.Count, k);

        await WriteReportsAsync(context, group, featureColumns, clusters, cancellationToken);

        context.Logger.LogInformation(
            "{Group}: clustered {Members} players into {Clusters} clusters",
            group,
            members.Count,
            clusters.Count);
        return true;
    }

    private static bool TryBuildVector(
        IReadOnlyDictionary<string, string?> row,
        IReadOnlyList<string> featureColumns,
        out float[] vector
    ) {
        var result = new float[featureColumns.Count];
        for (var i = 0; i < featureColumns.Count; i++) {
            if (!row.TryGetValue(featureColumns[i], out var raw)
                || raw is null
                || !float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var value)) {
                vector = [];
                return false;
            }

            result[i] = value;
        }

        vector = result;
        return true;
    }

    // k == 1: a single cluster over the whole group. The centroid is the component-wise mean and the
    // medoid is the member nearest that mean. Handled directly since ML.NET k-means requires k >= 2.
    private static List<Cluster> ClusterSingle(IReadOnlyList<GroupMember> members, int featureCount) {
        var centroid = new float[featureCount];
        foreach (var member in members) {
            for (var i = 0; i < featureCount; i++) {
                centroid[i] += member.Vector[i];
            }
        }

        for (var i = 0; i < featureCount; i++) {
            centroid[i] /= members.Count;
        }

        var medoid = members.MinBy(m => SquaredDistance(m.Vector, centroid))!;
        return [new Cluster(ClusterId: 1, Size: members.Count, Centroid: centroid, Medoid: medoid)];
    }

    private static List<Cluster> ClusterKMeans(
        MLContext mlContext,
        IReadOnlyList<GroupMember> members,
        int featureCount,
        int k
    ) {
        var schema = SchemaDefinition.Create(typeof(FeatureVector));
        schema[nameof(FeatureVector.Features)].ColumnType =
            new VectorDataViewType(NumberDataViewType.Single, featureCount);

        var dataView = mlContext.Data.LoadFromEnumerable(
            members.Select(m => new FeatureVector { Features = m.Vector }),
            schema);

        var trainer = mlContext.Clustering.Trainers.KMeans(
            new KMeansTrainer.Options {
                FeatureColumnName = nameof(FeatureVector.Features),
                NumberOfClusters = k,
            });

        var model = trainer.Fit(dataView);
        var transformed = model.Transform(dataView);
        var predictions = mlContext.Data
            .CreateEnumerable<ClusterPrediction>(transformed, reuseRowObject: false)
            .ToList();

        VBuffer<float>[] centroidBuffers = default!;
        model.Model.GetClusterCentroids(ref centroidBuffers, out var actualK);

        var clusters = new List<Cluster>(actualK);
        for (var clusterId = 1; clusterId <= actualK; clusterId++) {
            var id = clusterId;
            var memberIndices = predictions
                .Select((p, index) => (p.ClusterId, index))
                .Where(x => x.ClusterId == id)
                .Select(x => x.index)
                .ToList();

            if (memberIndices.Count == 0) {
                // An empty cluster still has a centroid but no medoid; report it with size 0.
                clusters.Add(new Cluster(id, Size: 0, Centroid: centroidBuffers[id - 1].DenseValues().ToArray(), Medoid: null));
                continue;
            }

            var medoidIndex = memberIndices.MinBy(index => predictions[index].Distances[id - 1]);
            clusters.Add(new Cluster(
                id,
                Size: memberIndices.Count,
                Centroid: centroidBuffers[id - 1].DenseValues().ToArray(),
                Medoid: members[medoidIndex]));
        }

        return clusters;
    }

    private static async Task WriteReportsAsync(
        AnalysisContext context,
        string group,
        IReadOnlyList<string> featureColumns,
        IReadOnlyList<Cluster> clusters,
        CancellationToken cancellationToken
    ) {
        var medoidColumns = new List<string> { "clusterId", "clusterSize", "playerId", "name" };
        medoidColumns.AddRange(featureColumns);

        var medoidRows = new List<IReadOnlyList<string?>>();
        foreach (var cluster in clusters.Where(c => c.Medoid is not null)) {
            var row = new List<string?> {
                cluster.ClusterId.ToString(CultureInfo.InvariantCulture),
                cluster.Size.ToString(CultureInfo.InvariantCulture),
                cluster.Medoid!.Row.TryGetValue(PlayerIdColumn, out var id) ? id : null,
                cluster.Medoid.Row.TryGetValue(NameColumn, out var name) ? name : null,
            };
            row.AddRange(featureColumns.Select(c => cluster.Medoid.Row.TryGetValue(c, out var v) ? v : null));
            medoidRows.Add(row);
        }

        var centroidColumns = new List<string> { "clusterId", "clusterSize" };
        centroidColumns.AddRange(featureColumns);

        var centroidRows = new List<IReadOnlyList<string?>>();
        foreach (var cluster in clusters) {
            var row = new List<string?> {
                cluster.ClusterId.ToString(CultureInfo.InvariantCulture),
                cluster.Size.ToString(CultureInfo.InvariantCulture),
            };
            row.AddRange(cluster.Centroid.Select(v => v.ToString("R", CultureInfo.InvariantCulture)));
            centroidRows.Add(row);
        }

        await CsvResultWriter.WriteFileAsync(
            new FileInfo(Path.Combine(context.Output.FullName, $"{group}-medoids.csv")),
            medoidColumns,
            medoidRows,
            cancellationToken);

        await CsvResultWriter.WriteFileAsync(
            new FileInfo(Path.Combine(context.Output.FullName, $"{group}-centroids.csv")),
            centroidColumns,
            centroidRows,
            cancellationToken);
    }

    private static double SquaredDistance(IReadOnlyList<float> a, IReadOnlyList<float> b) {
        double sum = 0;
        for (var i = 0; i < a.Count; i++) {
            var diff = a[i] - b[i];
            sum += diff * diff;
        }

        return sum;
    }

    private sealed record GroupMember(IReadOnlyDictionary<string, string?> Row, float[] Vector);

    private sealed record Cluster(int ClusterId, int Size, float[] Centroid, GroupMember? Medoid);

    private sealed class FeatureVector {
        public float[] Features { get; set; } = [];
    }

    private sealed class ClusterPrediction {
        [ColumnName("PredictedLabel")]
        public uint ClusterId { get; set; }

        [ColumnName("Score")]
        public float[] Distances { get; set; } = [];
    }
}
