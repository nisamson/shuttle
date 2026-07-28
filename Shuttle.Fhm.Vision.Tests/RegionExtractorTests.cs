using SixLabors.ImageSharp.PixelFormats;
using Shuttle.Fhm.Vision.Extraction;
using Shuttle.Fhm.Vision.Layout;

namespace Shuttle.Fhm.Vision.Tests;

public sealed class RegionExtractorTests {
    private static readonly Rgba32 AnchorColor = new(10, 10, 10);
    private static readonly Rgba32 NameColor = new(255, 0, 0);
    private static readonly Rgba32 NumberColor = new(0, 255, 0);
    private static readonly Rgba32 SkatingColor = new(0, 0, 255);
    private static readonly Rgba32 PlaymakerColor = new(255, 255, 0);
    private static readonly Rgba32 BioColor = new(0, 255, 255);
    private static readonly Rgba32 OverallColor = new(255, 0, 255);

    private static (LayoutProfile Profile, ColorMapOcrEngine Ocr) BuildFixture() {
        var profile = new LayoutProfile {
            Name = "test",
            Anchors = [
                new AnchorMarker { Bounds = new RatioRect(0.0, 0.0, 0.2, 0.1), ExpectedText = "Attributes" },
            ],
            Regions = [
                new FieldRegion { Key = "name", Group = FieldGroup.Identity, Kind = FieldKind.Text, Bounds = new RatioRect(0.0, 0.2, 0.2, 0.1) },
                new FieldRegion { Key = "number", Group = FieldGroup.Identity, Kind = FieldKind.Integer, Bounds = new RatioRect(0.3, 0.2, 0.1, 0.1) },
                new FieldRegion { Key = "skating", Group = FieldGroup.Attribute, Kind = FieldKind.Integer, Bounds = new RatioRect(0.0, 0.4, 0.1, 0.1) },
                new FieldRegion { Key = "playmaker", Group = FieldGroup.Role, Kind = FieldKind.Integer, Bounds = new RatioRect(0.0, 0.6, 0.1, 0.1) },
                new FieldRegion { Key = "overall", Group = FieldGroup.Attribute, Kind = FieldKind.Float, Bounds = new RatioRect(0.3, 0.4, 0.1, 0.1) },
                new FieldRegion { Key = "bio", Group = FieldGroup.Identity, Kind = FieldKind.Bio, Bounds = new RatioRect(0.0, 0.8, 0.9, 0.1) },
            ],
        };

        var map = new Dictionary<Rgba32, string> {
            [AnchorColor] = "Player Attributes",
            [NameColor] = "Wayne Gretzky",
            [NumberColor] = "99",
            [SkatingColor] = "15",
            [PlaymakerColor] = "19",
            [OverallColor] = "3.5",
            [BioColor] = "LD/RD | SACRAMENTO EXPRESS | SHOOTS: LEFT | AGE: 23 | 6'5\" - 243 LBS | SALARY: $775,000 (1)",
        };

        return (profile, new ColorMapOcrEngine(map));
    }

    private static SixLabors.ImageSharp.Image<Rgba32> BuildImage(LayoutProfile profile) {
        var colors = new Dictionary<string, Rgba32> {
            ["name"] = NameColor,
            ["number"] = NumberColor,
            ["skating"] = SkatingColor,
            ["playmaker"] = PlaymakerColor,
            ["overall"] = OverallColor,
            ["bio"] = BioColor,
        };

        var regions = profile.Regions.Select(r => (r.Bounds, colors[r.Key])).ToList();
        regions.Add((profile.Anchors[0].Bounds, AnchorColor));
        return TestImageFactory.WithRegions(400, 300, regions);
    }

    [Fact]
    public async Task ExtractAsync_maps_regions_to_identity_and_rating_vectors() {
        var (profile, ocr) = BuildFixture();
        using var image = BuildImage(profile);
        var extractor = new RegionExtractor(ocr);

        var capture = await extractor.ExtractAsync(image, profile, DateTimeOffset.UnixEpoch, TestContext.Current.CancellationToken);

        Assert.Equal("Wayne Gretzky", capture.Name);
        Assert.Equal(99, capture.JerseyNumber);
        Assert.Equal(15, capture.Attributes["skating"]);
        Assert.Equal(19, capture.RoleRatings["playmaker"]);
        Assert.Equal(3.5, capture.Numbers["overall"]);

        // Bio-kind region: position lands in the identity slot, height/weight in the number vector.
        Assert.Equal("LD/RD", capture.Position);
        Assert.Equal("6'5\"", capture.TextFields["height"]);
        Assert.Equal((6 * 12) + 5, capture.Numbers["heightInches"]);
        Assert.Equal(243, capture.Numbers["weight"]);
    }

    [Fact]
    public async Task IsPlayerScreenAsync_true_when_anchor_matches() {
        var (profile, ocr) = BuildFixture();
        using var image = BuildImage(profile);
        var extractor = new RegionExtractor(ocr);

        Assert.True(await extractor.IsPlayerScreenAsync(image, profile, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task IsPlayerScreenAsync_false_when_anchor_text_absent() {
        var (profile, _) = BuildFixture();
        using var image = BuildImage(profile);
        var ocr = new ColorMapOcrEngine(new Dictionary<Rgba32, string>()); // anchor reads empty
        var extractor = new RegionExtractor(ocr);

        Assert.False(await extractor.IsPlayerScreenAsync(image, profile, TestContext.Current.CancellationToken));
    }
}
