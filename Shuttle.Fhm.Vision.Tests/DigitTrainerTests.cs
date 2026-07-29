using System.Runtime.Versioning;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Shuttle.Fhm.Vision.Extraction;
using Shuttle.Fhm.Vision.Layout;
using Shuttle.Fhm.Vision.Ocr;
using Shuttle.Fhm.Vision.Recognition;
using Shuttle.Fhm.Vision.Training;

namespace Shuttle.Fhm.Vision.Tests;

[SupportedOSPlatform("windows")]
public sealed class DigitTrainerTests {
    private sealed class SilentOcrEngine : IOcrEngine {
        public Task<string> RecognizeAsync(ReadOnlyMemory<byte> imagePng, CancellationToken cancellationToken) =>
            Task.FromResult(string.Empty);
    }

    private static void FillBlock(Image<Rgba32> image, int x0, int y0, int x1, int y1) {
        var white = new Rgba32(255, 255, 255);
        for (var y = y0; y <= y1; y++) {
            for (var x = x0; x <= x1; x++) {
                image[x, y] = white;
            }
        }
    }

    [Fact]
    public async Task PendingGlyphBuilder_segments_numeric_regions_across_an_image() {
        // Two separated white blocks on a dark background inside a single Integer region -> two glyphs.
        var file = new FileInfo(Path.Combine(Path.GetTempPath(), $"trainer-{Guid.NewGuid():N}.png"));
        try {
            using (var image = new Image<Rgba32>(40, 20, new Rgba32(10, 20, 30))) {
                FillBlock(image, 4, 4, 8, 15);   // left glyph
                FillBlock(image, 20, 4, 24, 15); // right glyph (gap between them)
                await image.SaveAsPngAsync(file.FullName, TestContext.Current.CancellationToken);
            }

            var profile = new LayoutProfile {
                Name = "test",
                Anchors = [],
                Regions = [
                    new FieldRegion {
                        Key = "skating",
                        Group = FieldGroup.Attribute,
                        Kind = FieldKind.Integer,
                        Bounds = new RatioRect(0, 0, 1, 1),
                    },
                ],
            };

            var extractor = new RegionExtractor(new SilentOcrEngine());
            var glyphs = await PendingGlyphBuilder.BuildAsync(
                [file], [profile], DigitTemplateSet.DefaultWidth, DigitTemplateSet.DefaultHeight,
                extractor, TestContext.Current.CancellationToken);

            Assert.Equal(2, glyphs.Count);
            Assert.All(glyphs, g => Assert.Equal(file.Name, g.ImageName));
            Assert.All(glyphs, g => Assert.Equal("skating", g.RegionKey));
            Assert.All(glyphs, g => Assert.Equal(2, g.GlyphCount));
            Assert.Equal([0, 1], glyphs.Select(g => g.GlyphIndex));
            Assert.All(glyphs, g => Assert.Equal(DigitTemplateSet.DefaultWidth, g.Normalized.Width));
            Assert.All(glyphs, g => Assert.NotEmpty(g.OriginalCropPng));
        } finally {
            file.Delete();
        }
    }

    [Fact]
    public async Task PendingGlyphBuilder_skips_profiles_whose_anchors_do_not_match() {
        var file = new FileInfo(Path.Combine(Path.GetTempPath(), $"trainer-{Guid.NewGuid():N}.png"));
        try {
            using (var image = new Image<Rgba32>(40, 20, new Rgba32(10, 20, 30))) {
                FillBlock(image, 4, 4, 8, 15);
                await image.SaveAsPngAsync(file.FullName, TestContext.Current.CancellationToken);
            }

            // The silent OCR engine returns "", so an anchor expecting text never matches.
            var profile = new LayoutProfile {
                Name = "test",
                Anchors = [new AnchorMarker { Bounds = new RatioRect(0, 0, 1, 1), ExpectedText = "PLAYER" }],
                Regions = [
                    new FieldRegion {
                        Key = "skating", Group = FieldGroup.Attribute, Kind = FieldKind.Integer,
                        Bounds = new RatioRect(0, 0, 1, 1),
                    },
                ],
            };

            var extractor = new RegionExtractor(new SilentOcrEngine());
            var glyphs = await PendingGlyphBuilder.BuildAsync(
                [file], [profile], DigitTemplateSet.DefaultWidth, DigitTemplateSet.DefaultHeight,
                extractor, TestContext.Current.CancellationToken);

            Assert.Empty(glyphs);
        } finally {
            file.Delete();
        }
    }

    [Fact]
    public void GlyphImaging_renders_ink_black_and_background_white_scaled() {
        var glyph = new GlyphBitmap(2, 2, [true, false, false, true]);

        using var bitmap = GlyphImaging.Render(glyph, scale: 3);

        Assert.Equal(6, bitmap.Width);
        Assert.Equal(6, bitmap.Height);
        Assert.Equal(0, bitmap.GetPixel(0, 0).R);   // top-left cell is ink -> black
        Assert.Equal(255, bitmap.GetPixel(3, 0).R); // top-right cell is background -> white
        Assert.Equal(0, bitmap.GetPixel(3, 3).R);   // bottom-right cell is ink -> black
    }
}
