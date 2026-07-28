using Shuttle.Fhm.Vision.Model;
using Shuttle.Fhm.Vision.Storage;

namespace Shuttle.Fhm.Vision.Tests;

public sealed class CaptureStoreTests : IDisposable {
    private readonly string _root = Path.Combine(Path.GetTempPath(), "fhm-vision-tests", Guid.NewGuid().ToString("N"));

    private static FhmPlayerCapture SampleCapture(string name = "Wayne Gretzky", int skating = 15) => new() {
        CapturedAtUtc = DateTimeOffset.UtcNow,
        Name = name,
        JerseyNumber = 99,
        Position = "C",
        Handedness = "L",
        Attributes = new Dictionary<string, int> { ["skating"] = skating },
        RoleRatings = new Dictionary<string, int> { ["playmaker"] = 19 },
    };

    [Fact]
    public async Task TryStoreAsync_stores_new_capture_and_writes_image() {
        var ct = TestContext.Current.CancellationToken;
        var store = await OpenStoreAsync(ct);
        var png = new byte[] { 1, 2, 3, 4 };

        var result = await store.TryStoreAsync(SampleCapture(), png, ct);

        Assert.Equal(CaptureStoreOutcome.Stored, result.Outcome);
        Assert.NotNull(result.ImageFileName);
        Assert.True(File.Exists(Path.Combine(store.ImagesDirectory, result.ImageFileName!)));
    }

    [Fact]
    public async Task TryStoreAsync_rejects_duplicate_content_hash() {
        var ct = TestContext.Current.CancellationToken;
        var store = await OpenStoreAsync(ct);

        var first = await store.TryStoreAsync(SampleCapture(), new byte[] { 1 }, ct);
        var second = await store.TryStoreAsync(SampleCapture(), new byte[] { 2 }, ct);

        Assert.Equal(CaptureStoreOutcome.Stored, first.Outcome);
        Assert.Equal(CaptureStoreOutcome.Duplicate, second.Outcome);
        Assert.Equal(first.RecordId, second.RecordId);
    }

    [Fact]
    public async Task TryStoreAsync_stores_distinct_captures_separately() {
        var ct = TestContext.Current.CancellationToken;
        var store = await OpenStoreAsync(ct);

        // Distinct captures differ by their numeric values (dedup ignores name/identity text).
        var a = await store.TryStoreAsync(SampleCapture(skating: 15), new byte[] { 1 }, ct);
        var b = await store.TryStoreAsync(SampleCapture(skating: 16), new byte[] { 2 }, ct);

        Assert.Equal(CaptureStoreOutcome.Stored, a.Outcome);
        Assert.Equal(CaptureStoreOutcome.Stored, b.Outcome);
        Assert.NotEqual(a.RecordId, b.RecordId);
    }

    [Fact]
    public async Task TryStoreAsync_treats_same_numbers_with_different_name_as_duplicate() {
        var ct = TestContext.Current.CancellationToken;
        var store = await OpenStoreAsync(ct);

        var first = await store.TryStoreAsync(SampleCapture("Player A"), new byte[] { 1 }, ct);
        var second = await store.TryStoreAsync(SampleCapture("Player B"), new byte[] { 2 }, ct);

        Assert.Equal(CaptureStoreOutcome.Stored, first.Outcome);
        Assert.Equal(CaptureStoreOutcome.Duplicate, second.Outcome);
        Assert.Equal(first.RecordId, second.RecordId);
    }

    private async Task<CaptureStore> OpenStoreAsync(CancellationToken cancellationToken) =>
        await CaptureStore.OpenAsync(Path.Combine(_root, "captures.db"), null, cancellationToken);

    public void Dispose() {
        if (Directory.Exists(_root)) {
            try {
                Directory.Delete(_root, recursive: true);
            } catch (IOException) {
                // Best-effort cleanup; the SQLite file may still be briefly locked.
            }
        }
    }
}
