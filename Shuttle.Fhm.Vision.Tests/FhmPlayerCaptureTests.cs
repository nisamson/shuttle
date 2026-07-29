using Shuttle.Fhm.Vision.Model;

namespace Shuttle.Fhm.Vision.Tests;

public sealed class FhmPlayerCaptureTests {
    private static FhmPlayerCapture Make(
        string name = "Wayne Gretzky",
        int? number = 99,
        IReadOnlyDictionary<string, int>? attributes = null,
        IReadOnlyDictionary<string, int>? roles = null
    ) => new() {
        CapturedAtUtc = DateTimeOffset.UtcNow,
        Name = name,
        JerseyNumber = number,
        Position = "C",
        Handedness = "L",
        Attributes = attributes ?? new Dictionary<string, int> { ["skating"] = 15, ["passing"] = 20 },
        RoleRatings = roles ?? new Dictionary<string, int> { ["playmaker"] = 19 },
    };

    [Fact]
    public void ContentHash_is_stable_across_instances() {
        Assert.Equal(Make().ContentHash, Make().ContentHash);
    }

    [Fact]
    public void ContentHash_ignores_attribute_ordering() {
        var a = Make(attributes: new Dictionary<string, int> { ["skating"] = 15, ["passing"] = 20 });
        var b = Make(attributes: new Dictionary<string, int> { ["passing"] = 20, ["skating"] = 15 });

        Assert.Equal(a.ContentHash, b.ContentHash);
    }

    [Fact]
    public void ContentHash_ignores_capture_timestamp() {
        var a = Make() with { CapturedAtUtc = DateTimeOffset.UnixEpoch };
        var b = Make() with { CapturedAtUtc = DateTimeOffset.UtcNow };

        Assert.Equal(a.ContentHash, b.ContentHash);
    }

    [Fact]
    public void ContentHash_differs_when_a_rating_changes() {
        var a = Make(attributes: new Dictionary<string, int> { ["skating"] = 15 });
        var b = Make(attributes: new Dictionary<string, int> { ["skating"] = 16 });

        Assert.NotEqual(a.ContentHash, b.ContentHash);
    }

    [Fact]
    public void ContentHash_ignores_all_identity_text() {
        // Dedup keys on numeric values only: same ratings but different name/number/position/
        // handedness => same hash.
        var a = Make(name: "Player A", number: 10) with { Position = "C", Handedness = "L" };
        var b = Make(name: "Player B", number: 99) with { Position = "LW", Handedness = "R" };

        Assert.Equal(a.ContentHash, b.ContentHash);
    }

    [Fact]
    public void ContentHash_differs_when_a_number_changes() {
        var a = Make() with { Numbers = new Dictionary<string, double> { ["weight"] = 200 } };
        var b = Make() with { Numbers = new Dictionary<string, double> { ["weight"] = 201 } };

        Assert.NotEqual(a.ContentHash, b.ContentHash);
    }
}
