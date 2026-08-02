using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.ML;
using Shuttle.Analysis.Flows;

namespace Shuttle.Tests.Analysis;

public class AnalysisFlowContextTests {

    private sealed class DefaultFlow : AnalysisFlowBase {
        public override string Name => "default";
        public override string Description => "default";

        public override Task<AnalysisFlowResult> RunAsync(AnalysisContext context, CancellationToken cancellationToken) =>
            Task.FromResult(AnalysisFlowResult.Success());
    }

    private sealed class DatabaseFlow : AnalysisFlowBase {
        public override string Name => "database";
        public override string Description => "database";
        public override FlowDataSource DataSource => FlowDataSource.Database;

        public override Task<AnalysisFlowResult> RunAsync(AnalysisContext context, CancellationToken cancellationToken) =>
            Task.FromResult(AnalysisFlowResult.Success());
    }

    private sealed class Marker;

    private static AnalysisContext CsvContext() =>
        new(
            new MLContext(seed: 1),
            new IngestedData(["a"], []),
            new FileInfo("in.csv"),
            new DirectoryInfo(Path.GetTempPath()),
            NullLogger.Instance);

    private static AnalysisContext DatabaseContext(IServiceProvider services) =>
        new(
            new MLContext(seed: 1),
            data: null,
            input: null,
            new DirectoryInfo(Path.GetTempPath()),
            NullLogger.Instance,
            arguments: null,
            services: services);

    [Fact]
    public void AnalysisFlowBase_DefaultsToCsvDataSource() {
        Assert.Equal(FlowDataSource.Csv, new DefaultFlow().DataSource);
    }

    [Fact]
    public void AnalysisFlowBase_CanOverrideToDatabase() {
        Assert.Equal(FlowDataSource.Database, new DatabaseFlow().DataSource);
    }

    [Fact]
    public void RequireData_ReturnsData_WhenPresent() {
        var context = CsvContext();

        Assert.Same(context.Data, context.RequireData());
    }

    [Fact]
    public void RequireData_Throws_WhenDataAbsent() {
        var services = new ServiceCollection().BuildServiceProvider();
        var context = DatabaseContext(services);

        Assert.Throws<InvalidOperationException>(() => context.RequireData());
    }

    [Fact]
    public void RequireServices_Throws_WhenServicesAbsent() {
        var context = CsvContext();

        Assert.Throws<InvalidOperationException>(() => context.RequireServices());
    }

    [Fact]
    public void GetRequiredService_ResolvesFromScopedProvider() {
        var marker = new Marker();
        var services = new ServiceCollection()
            .AddSingleton(marker)
            .BuildServiceProvider();
        var context = DatabaseContext(services);

        Assert.Same(marker, context.GetRequiredService<Marker>());
    }

    [Fact]
    public void GetRequiredService_Throws_WhenServicesAbsent() {
        var context = CsvContext();

        Assert.Throws<InvalidOperationException>(() => context.GetRequiredService<Marker>());
    }
}
