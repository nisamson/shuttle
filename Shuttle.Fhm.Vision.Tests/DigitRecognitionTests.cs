using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Shuttle.Fhm.Vision.Layout;
using Shuttle.Fhm.Vision.Recognition;

namespace Shuttle.Fhm.Vision.Tests;

public sealed class DigitRecognitionTests {
    private static readonly Rgba32 Ink = new(255, 255, 255);
    private static readonly Rgba32 Background = new(10, 20, 30);

    private static Image<Rgba32> Canvas(int width, int height) => new(width, height, Background);

    private static void FillBlock(Image<Rgba32> image, int x0, int y0, int x1, int y1) {
        for (var y = y0; y <= y1; y++) {
            for (var x = x0; x <= x1; x++) {
                image[x, y] = Ink;
            }
        }
    }

    [Fact]
    public void GlyphBitmap_bitstring_round_trips_and_measures_distance() {
        var pixels = new bool[] { true, false, false, true };
        var glyph = new GlyphBitmap(2, 2, pixels);

        var restored = GlyphBitmap.FromBitString(2, 2, glyph.ToBitString());

        Assert.Equal("1001", glyph.ToBitString());
        Assert.Equal(0, glyph.Distance(restored));
        Assert.Equal(2, glyph.InkCount);

        var other = new GlyphBitmap(2, 2, [true, true, false, true]);
        Assert.Equal(1, glyph.Distance(other));
    }

    [Fact]
    public async Task DigitTemplateStore_round_trips_via_json() {
        var set = new DigitTemplateSet { Width = 3, Height = 3 };
        set.Add("7", new GlyphBitmap(3, 3, [true, true, true, false, false, true, false, true, false]));

        var file = new FileInfo(Path.Combine(Path.GetTempPath(), $"tmpl-{Guid.NewGuid():N}.json"));
        try {
            await DigitTemplateStore.SaveAsync(file, set, TestContext.Current.CancellationToken);
            var loaded = await DigitTemplateStore.LoadAsync(file, TestContext.Current.CancellationToken);

            Assert.Equal(3, loaded.Width);
            var materialized = loaded.Materialize();
            Assert.Single(materialized);
            Assert.Equal("7", materialized[0].Label);
            Assert.Equal(set.Templates[0].Bits, loaded.Templates[0].Bits);
        } finally {
            file.Delete();
        }
    }

    [Fact]
    public void Segmenter_splits_two_separated_blocks_into_two_glyphs() {
        using var image = Canvas(16, 10);
        FillBlock(image, 2, 2, 5, 7);   // left block
        FillBlock(image, 10, 2, 13, 7); // right block, separated by empty columns 6-9

        var glyphs = DigitSegmenter.Segment(image, new PixelRect(0, 0, 16, 10), 4, 6);

        Assert.Equal(2, glyphs.Count);
        Assert.Equal(2, glyphs[0].Bounds.X);
        Assert.Equal(10, glyphs[1].Bounds.X);
    }

    [Fact]
    public void Recognizer_matches_a_trained_glyph_exactly() {
        using var image = Canvas(12, 12);
        FillBlock(image, 3, 2, 8, 9);
        var region = new PixelRect(0, 0, 12, 12);

        var set = new DigitTemplateSet { Width = 5, Height = 7 };
        var trained = DigitSegmenter.Segment(image, region, set.Width, set.Height);
        Assert.Single(trained);
        set.Add("5", trained[0].Glyph);

        var recognizer = new TemplateDigitRecognizer(set);
        var result = recognizer.Read(image, region);

        Assert.True(result.Recognized);
        Assert.Equal("5", result.Text);
        Assert.Equal(0.0, result.WorstScore);
    }

    [Fact]
    public void Recognizer_reports_low_confidence_for_unlike_glyph() {
        var region = new PixelRect(0, 0, 14, 14);
        var set = new DigitTemplateSet { Width = 5, Height = 7 };

        using (var trainImage = Canvas(14, 14)) {
            // Train on a hollow block: ink border around a large empty centre.
            FillBlock(trainImage, 2, 2, 11, 11);
            for (var y = 4; y <= 9; y++) {
                for (var x = 4; x <= 9; x++) {
                    trainImage[x, y] = Background;
                }
            }

            var trained = DigitSegmenter.Segment(trainImage, region, set.Width, set.Height);
            set.Add("8", trained[0].Glyph);
        }

        // Recognize a fully solid block — same footprint but a very different ink pattern.
        using var testImage = Canvas(14, 14);
        FillBlock(testImage, 2, 2, 11, 11);
        var recognizer = new TemplateDigitRecognizer(set, maxNormalizedDistance: 0.1);
        var result = recognizer.Read(testImage, region);

        Assert.False(result.Recognized);
        Assert.True(result.WorstScore > 0.1, $"expected worst score > 0.1 but was {result.WorstScore}");
    }

    [Fact]
    public void Recognizer_returns_empty_for_blank_region() {
        using var image = Canvas(12, 12);
        var set = new DigitTemplateSet { Width = 5, Height = 7 };
        set.Add("0", new GlyphBitmap(5, 7, new bool[5 * 7]));

        var recognizer = new TemplateDigitRecognizer(set);
        var result = recognizer.Read(image, new PixelRect(0, 0, 12, 12));

        Assert.False(result.Recognized);
        Assert.Equal(string.Empty, result.Text);
    }

    [Fact]
    public void Classify_returns_confident_match_for_trained_glyph() {
        using var image = Canvas(12, 12);
        FillBlock(image, 3, 2, 8, 9);
        var region = new PixelRect(0, 0, 12, 12);

        var set = new DigitTemplateSet { Width = 5, Height = 7 };
        var trained = DigitSegmenter.Segment(image, region, set.Width, set.Height);
        set.Add("9", trained[0].Glyph);

        var recognizer = new TemplateDigitRecognizer(set);
        var match = recognizer.Classify(trained[0].Glyph);

        Assert.Equal("9", match.Label);
        Assert.True(match.Confident);
        Assert.Equal(0.0, match.Score);
    }
}
