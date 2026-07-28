using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Shuttle.Fhm.Vision.Extraction;
using Shuttle.Fhm.Vision.Layout;

namespace Shuttle.Fhm.Vision.Recognition;

/// <summary>
/// Classifies the fixed FHM rating font by nearest-neighbour matching each segmented glyph against a
/// trained <see cref="DigitTemplateSet"/>. Deterministic and offline; far more reliable than general
/// OCR on the short, isolated numeric cells this tool captures.
/// </summary>
public sealed class TemplateDigitRecognizer : IDigitRecognizer {
    private readonly int width;
    private readonly int height;
    private readonly double maxNormalizedDistance;
    private readonly byte whiteThreshold;
    private readonly IReadOnlyList<(string Label, GlyphBitmap Glyph)> templates;

    /// <param name="maxNormalizedDistance">
    /// Per-glyph acceptance threshold as a fraction of total pixels (0..1). A glyph whose nearest
    /// template exceeds this is treated as unrecognized, so the caller can fall back to OCR.
    /// </param>
    public TemplateDigitRecognizer(
        DigitTemplateSet set,
        double maxNormalizedDistance = 0.18,
        byte whiteThreshold = RegionImaging.WhiteTextThreshold
    ) {
        ArgumentNullException.ThrowIfNull(set);
        templates = set.Materialize();
        if (templates.Count == 0) {
            throw new ArgumentException("The digit template set is empty; train it with 'train-digits' first.", nameof(set));
        }

        width = set.Width;
        height = set.Height;
        maxNormalizedDistance = Math.Clamp(maxNormalizedDistance, 0.0, 1.0);
        this.maxNormalizedDistance = maxNormalizedDistance;
        this.whiteThreshold = whiteThreshold;
    }

    public DigitReadResult Read(Image<Rgba32> image, PixelRect region) {
        ArgumentNullException.ThrowIfNull(image);

        var glyphs = DigitSegmenter.Segment(image, region, width, height, whiteThreshold);
        if (glyphs.Count == 0) {
            return DigitReadResult.Empty;
        }

        var total = width * height;
        var builder = new System.Text.StringBuilder(glyphs.Count);
        var recognized = true;
        var worst = 0.0;

        foreach (var glyph in glyphs) {
            var match = Classify(glyph.Glyph);
            worst = Math.Max(worst, match.Score);
            builder.Append(match.Label);
            if (!match.Confident) {
                recognized = false;
            }
        }

        return new DigitReadResult(builder.ToString(), recognized, worst);
    }

    /// <summary>Normalized glyph dimensions this recognizer expects (matches its template set).</summary>
    public int GlyphWidth => width;

    public int GlyphHeight => height;

    /// <summary>
    /// Classifies a single normalized glyph against the templates. <paramref name="glyph"/> must match
    /// <see cref="GlyphWidth"/>x<see cref="GlyphHeight"/> (i.e. produced with the same normalization).
    /// </summary>
    public GlyphMatch Classify(GlyphBitmap glyph) {
        ArgumentNullException.ThrowIfNull(glyph);
        var (label, distance) = NearestLabel(glyph);
        var normalized = (double)distance / (width * height);
        return new GlyphMatch(label, normalized, normalized <= maxNormalizedDistance);
    }

    private (string Label, int Distance) NearestLabel(GlyphBitmap candidate) {
        var bestLabel = templates[0].Label;
        var bestDistance = int.MaxValue;
        foreach (var (label, glyph) in templates) {
            var distance = candidate.Distance(glyph);
            if (distance < bestDistance) {
                bestDistance = distance;
                bestLabel = label;
            }
        }

        return (bestLabel, bestDistance);
    }
}
