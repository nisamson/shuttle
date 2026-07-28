using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Shuttle.Fhm.Vision.Layout;

namespace Shuttle.Fhm.Vision.Recognition;

/// <summary>A glyph found in an image: its bounding box (absolute pixels) and normalized bitmap.</summary>
public readonly record struct SegmentedGlyph(PixelRect Bounds, GlyphBitmap Glyph);

/// <summary>
/// Splits a numeric cell into individual glyph bitmaps. FHM renders ratings as white text on a
/// coloured/dark background, so "ink" is a near-white pixel. Glyphs are separated by vertical
/// projection (a column with no ink ends the current glyph) and normalized to a fixed size.
/// </summary>
public static class DigitSegmenter {
    /// <summary>
    /// Segments the given <paramref name="region"/> of <paramref name="image"/> into normalized glyphs,
    /// ordered left-to-right.
    /// </summary>
    /// <param name="whiteThreshold">Min value each RGB channel must reach for a pixel to be ink.</param>
    /// <param name="minInkPixels">Column runs with fewer ink pixels than this are discarded as noise.</param>
    public static IReadOnlyList<SegmentedGlyph> Segment(
        Image<Rgba32> image,
        PixelRect region,
        int normWidth,
        int normHeight,
        byte whiteThreshold = 170,
        int minInkPixels = 3
    ) {
        ArgumentNullException.ThrowIfNull(image);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(normWidth, 0);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(normHeight, 0);

        var w = region.Width;
        var h = region.Height;
        var ink = new bool[w * h];
        var columnHasInk = new bool[w];

        for (var y = 0; y < h; y++) {
            for (var x = 0; x < w; x++) {
                var pixel = image[region.X + x, region.Y + y];
                var isInk = pixel.R >= whiteThreshold && pixel.G >= whiteThreshold && pixel.B >= whiteThreshold;
                if (isInk) {
                    ink[(y * w) + x] = true;
                    columnHasInk[x] = true;
                }
            }
        }

        var glyphs = new List<SegmentedGlyph>();
        var runStart = -1;
        for (var x = 0; x <= w; x++) {
            var inRun = x < w && columnHasInk[x];
            if (inRun && runStart < 0) {
                runStart = x;
            } else if (!inRun && runStart >= 0) {
                var glyph = BuildGlyph(ink, w, h, runStart, x - 1, region, normWidth, normHeight, minInkPixels);
                if (glyph is { } value) {
                    glyphs.Add(value);
                }

                runStart = -1;
            }
        }

        return glyphs;
    }

    private static SegmentedGlyph? BuildGlyph(
        bool[] ink, int w, int h, int x0, int x1, PixelRect region, int normWidth, int normHeight, int minInkPixels) {
        var top = -1;
        var bottom = -1;
        var count = 0;
        for (var y = 0; y < h; y++) {
            for (var x = x0; x <= x1; x++) {
                if (!ink[(y * w) + x]) {
                    continue;
                }

                count++;
                if (top < 0) {
                    top = y;
                }

                bottom = y;
            }
        }

        if (count < minInkPixels || top < 0) {
            return null;
        }

        var glyphWidth = x1 - x0 + 1;
        var glyphHeight = bottom - top + 1;
        var pixels = new bool[normWidth * normHeight];
        for (var ty = 0; ty < normHeight; ty++) {
            var sy = top + (int)(((ty + 0.5) * glyphHeight) / normHeight);
            sy = Math.Clamp(sy, top, bottom);
            for (var tx = 0; tx < normWidth; tx++) {
                var sx = x0 + (int)(((tx + 0.5) * glyphWidth) / normWidth);
                sx = Math.Clamp(sx, x0, x1);
                pixels[(ty * normWidth) + tx] = ink[(sy * w) + sx];
            }
        }

        var bounds = new PixelRect(region.X + x0, region.Y + top, glyphWidth, glyphHeight);
        return new SegmentedGlyph(bounds, new GlyphBitmap(normWidth, normHeight, pixels));
    }
}
