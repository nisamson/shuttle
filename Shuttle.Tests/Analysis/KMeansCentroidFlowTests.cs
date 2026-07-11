using System.Globalization;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.ML;
using Shuttle.Analysis.Flows;

namespace Shuttle.Tests.Analysis;

public class KMeansCentroidFlowTests {

    private static readonly string[] SkaterColumns = [
        "playerId", "name", "skaterAttributes.checking", "skaterAttributes.speed",
    ];

    private static readonly string[] GoaltenderColumns = [
        "playerId", "name", "goaltenderAttributes.glove", "goaltenderAttributes.reflexes",
    ];

    private static IReadOnlyDictionary<string, string?> SkaterRow(int id, string name, double checking, double speed) =>
        new Dictionary<string, string?> {
            ["playerId"] = id.ToString(CultureInfo.InvariantCulture),
            ["name"] = name,
            ["skaterAttributes.checking"] = checking.ToString(CultureInfo.InvariantCulture),
            ["skaterAttributes.speed"] = speed.ToString(CultureInfo.InvariantCulture),
        };

    private static IReadOnlyDictionary<string, string?> GoaltenderRow(int id, string name, double glove, double reflexes) =>
        new Dictionary<string, string?> {
            ["playerId"] = id.ToString(CultureInfo.InvariantCulture),
            ["name"] = name,
            ["goaltenderAttributes.glove"] = glove.ToString(CultureInfo.InvariantCulture),
            ["goaltenderAttributes.reflexes"] = reflexes.ToString(CultureInfo.InvariantCulture),
        };

    private static async Task<(int ExitLikeResult, DirectoryInfo Output)> RunAsync(
        IReadOnlyList<string> columns,
        IReadOnlyList<IReadOnlyDictionary<string, string?>> rows,
        params string[] args
    ) {
        var data = new IngestedData(columns, rows);
        var output = new DirectoryInfo(Path.Combine(Path.GetTempPath(), $"shuttle-kmeans-{Guid.NewGuid():N}"));
        output.Create();

        var context = new AnalysisContext(
            new MLContext(seed: 1),
            data,
            new FileInfo("in.csv"),
            output,
            NullLogger.Instance,
            FlowArguments.Parse(args));

        var flow = new KMeansCentroidFlow();
        var result = await flow.RunAsync(context, TestContext.Current.CancellationToken);
        return (result.Succeeded ? 0 : 1, output);
    }

    private static string[][] ReadCsv(string path) {
        using var reader = new StreamReader(path, Encoding.UTF8);
        var text = reader.ReadToEnd();
        return text
            .Split("\r\n", StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Split(','))
            .ToArray();
    }

    [Fact]
    public async Task Run_ClustersSkatersIntoKGroups_ReportsMedoidsAndCentroids() {
        // Two well-separated clusters: low (~1,1) and high (~20,20).
        var rows = new[] {
            SkaterRow(1, "Low A", 1, 1), SkaterRow(2, "Low B", 2, 1), SkaterRow(3, "Low C", 1, 2),
            SkaterRow(4, "High A", 20, 20), SkaterRow(5, "High B", 19, 20), SkaterRow(6, "High C", 20, 19),
        };

        var (result, output) = await RunAsync(SkaterColumns, rows, "k=2", "seed=1");
        try {
            Assert.Equal(0, result);

            var medoids = ReadCsv(Path.Combine(output.FullName, "skater-medoids.csv"));
            var centroids = ReadCsv(Path.Combine(output.FullName, "skater-centroids.csv"));

            // Header + 2 clusters.
            Assert.Equal(3, medoids.Length);
            Assert.Equal(3, centroids.Length);

            var header = medoids[0];
            Assert.Equal(["clusterId", "clusterSize", "playerId", "name", "skaterAttributes.checking", "skaterAttributes.speed"], header);

            // Medoids are real input players, one per cluster (distinct ids).
            var medoidIds = medoids.Skip(1).Select(r => r[2]).ToHashSet();
            Assert.Equal(2, medoidIds.Count);
            Assert.All(medoidIds, id => Assert.Contains(id, new[] { "1", "2", "3", "4", "5", "6" }));

            // One centroid sits near the low cluster, the other near the high cluster.
            var checkingValues = centroids.Skip(1)
                .Select(r => double.Parse(r[2], CultureInfo.InvariantCulture))
                .OrderBy(v => v)
                .ToArray();
            Assert.True(checkingValues[0] < 10, $"expected a low centroid, got {checkingValues[0]}");
            Assert.True(checkingValues[1] > 10, $"expected a high centroid, got {checkingValues[1]}");
        } finally {
            output.Delete(recursive: true);
        }
    }

    [Fact]
    public async Task Run_K1_ReportsSingleCentroidAndNearestMedoid() {
        var rows = new[] {
            SkaterRow(1, "A", 2, 2), SkaterRow(2, "B", 4, 4), SkaterRow(3, "C", 6, 6),
        };

        var (result, output) = await RunAsync(SkaterColumns, rows, "k=1");
        try {
            Assert.Equal(0, result);

            var medoids = ReadCsv(Path.Combine(output.FullName, "skater-medoids.csv"));
            var centroids = ReadCsv(Path.Combine(output.FullName, "skater-centroids.csv"));

            // One cluster row each.
            Assert.Equal(2, medoids.Length);
            Assert.Equal(2, centroids.Length);

            // Centroid is the mean (4,4); the nearest player is B (id 2).
            Assert.Equal("1", centroids[1][0]); // clusterId
            Assert.Equal("3", centroids[1][1]); // clusterSize
            Assert.Equal(4, double.Parse(centroids[1][2], CultureInfo.InvariantCulture), 3);
            Assert.Equal(4, double.Parse(centroids[1][3], CultureInfo.InvariantCulture), 3);
            Assert.Equal("2", medoids[1][2]); // medoid playerId
        } finally {
            output.Delete(recursive: true);
        }
    }

    [Fact]
    public async Task Run_MixedFile_ClustersSkatersAndGoaltendersSeparately() {
        var columns = new[] {
            "playerId", "name",
            "skaterAttributes.checking", "skaterAttributes.speed",
            "goaltenderAttributes.glove", "goaltenderAttributes.reflexes",
        };

        var rows = new List<IReadOnlyDictionary<string, string?>> {
            SkaterRow(1, "Low A", 1, 1), SkaterRow(2, "Low B", 2, 1),
            SkaterRow(3, "High A", 20, 20), SkaterRow(4, "High B", 19, 20),
            GoaltenderRow(5, "G Low A", 1, 1), GoaltenderRow(6, "G Low B", 2, 1),
            GoaltenderRow(7, "G High A", 20, 20), GoaltenderRow(8, "G High B", 19, 20),
        };

        var (result, output) = await RunAsync(columns, rows, "k=2", "seed=1");
        try {
            Assert.Equal(0, result);
            Assert.True(File.Exists(Path.Combine(output.FullName, "skater-medoids.csv")));
            Assert.True(File.Exists(Path.Combine(output.FullName, "skater-centroids.csv")));
            Assert.True(File.Exists(Path.Combine(output.FullName, "goaltender-medoids.csv")));
            Assert.True(File.Exists(Path.Combine(output.FullName, "goaltender-centroids.csv")));

            // Goaltender report uses goaltender attribute columns, not skater ones.
            var goalieHeader = ReadCsv(Path.Combine(output.FullName, "goaltender-centroids.csv"))[0];
            Assert.Contains("goaltenderAttributes.glove", goalieHeader);
            Assert.DoesNotContain("skaterAttributes.checking", goalieHeader);
        } finally {
            output.Delete(recursive: true);
        }
    }

    [Fact]
    public async Task Run_SkatersOnly_DoesNotWriteGoaltenderReports() {
        var rows = new[] {
            SkaterRow(1, "A", 1, 1), SkaterRow(2, "B", 20, 20),
        };

        var (result, output) = await RunAsync(SkaterColumns, rows, "k=2", "seed=1");
        try {
            Assert.Equal(0, result);
            Assert.True(File.Exists(Path.Combine(output.FullName, "skater-medoids.csv")));
            Assert.False(File.Exists(Path.Combine(output.FullName, "goaltender-medoids.csv")));
        } finally {
            output.Delete(recursive: true);
        }
    }

    [Fact]
    public async Task Run_GroupSmallerThanK_IsSkipped_AndFailsWhenNothingClustered() {
        var rows = new[] { SkaterRow(1, "A", 1, 1) };

        var (result, output) = await RunAsync(SkaterColumns, rows, "k=2");
        try {
            Assert.Equal(1, result);
            Assert.False(File.Exists(Path.Combine(output.FullName, "skater-medoids.csv")));
        } finally {
            output.Delete(recursive: true);
        }
    }

    [Fact]
    public async Task Run_MissingK_Fails() {
        var rows = new[] { SkaterRow(1, "A", 1, 1), SkaterRow(2, "B", 20, 20) };

        var (result, _) = await RunAsync(SkaterColumns, rows);
        Assert.Equal(1, result);
    }

    [Fact]
    public async Task Run_NonIntegerK_Fails() {
        var rows = new[] { SkaterRow(1, "A", 1, 1), SkaterRow(2, "B", 20, 20) };

        var (result, _) = await RunAsync(SkaterColumns, rows, "k=abc");
        Assert.Equal(1, result);
    }
}
