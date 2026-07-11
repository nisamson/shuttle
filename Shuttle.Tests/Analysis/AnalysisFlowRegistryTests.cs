using Shuttle.Analysis.Flows;

namespace Shuttle.Tests.Analysis;

public class AnalysisFlowRegistryTests {

    private sealed class StubFlow(string name, string description = "stub") : IDataAnalysisFlow {
        public string Name { get; } = name;
        public string Description { get; } = description;

        public Task<AnalysisFlowResult> RunAsync(AnalysisContext context, CancellationToken cancellationToken) =>
            Task.FromResult(AnalysisFlowResult.Success());
    }

    [Fact]
    public void CreateDefault_RegistersKMeansCentroidsFlow() {
        var registry = AnalysisFlowRegistry.CreateDefault();

        Assert.True(registry.TryGet("kmeans-centroids", out var flow));
        Assert.NotNull(flow);
    }

    [Fact]
    public void TryGet_ResolvesRegisteredFlowCaseInsensitively() {
        var flow = new StubFlow("draft-success");
        var registry = new AnalysisFlowRegistry([flow]);

        Assert.True(registry.TryGet("DRAFT-SUCCESS", out var resolved));
        Assert.Same(flow, resolved);
    }

    [Fact]
    public void TryGet_ReturnsFalseForUnknownFlow() {
        var registry = new AnalysisFlowRegistry([new StubFlow("known")]);

        Assert.False(registry.TryGet("unknown", out var resolved));
        Assert.Null(resolved);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void TryGet_ReturnsFalseForBlankName(string? name) {
        var registry = new AnalysisFlowRegistry([new StubFlow("known")]);

        Assert.False(registry.TryGet(name!, out var resolved));
        Assert.Null(resolved);
    }

    [Fact]
    public void Flows_AreOrderedByName() {
        var registry = new AnalysisFlowRegistry([new StubFlow("zebra"), new StubFlow("alpha")]);

        Assert.Equal(["alpha", "zebra"], registry.Flows.Select(f => f.Name));
    }

    [Fact]
    public void Constructor_ThrowsOnDuplicateNames() {
        Assert.Throws<ArgumentException>(
            () => new AnalysisFlowRegistry([new StubFlow("dup"), new StubFlow("DUP")]));
    }
}
