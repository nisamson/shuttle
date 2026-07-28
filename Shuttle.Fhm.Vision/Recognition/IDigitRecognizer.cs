using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Shuttle.Fhm.Vision.Layout;

namespace Shuttle.Fhm.Vision.Recognition;

/// <summary>Outcome of reading a numeric cell with a <see cref="IDigitRecognizer"/>.</summary>
/// <param name="Text">The recognized characters, left-to-right (best-guess even when not confident).</param>
/// <param name="Recognized">True only when every segmented glyph matched a template within tolerance.</param>
/// <param name="WorstScore">The worst per-glyph normalized distance (0 = exact, 1 = fully different).</param>
public readonly record struct DigitReadResult(string Text, bool Recognized, double WorstScore) {
    public static DigitReadResult Empty => new(string.Empty, false, 1.0);
}

/// <summary>Recognizes the digits in a numeric cell of a captured image.</summary>
public interface IDigitRecognizer {
    DigitReadResult Read(Image<Rgba32> image, PixelRect region);
}

/// <summary>Nearest-template match for a single glyph.</summary>
/// <param name="Label">The label of the closest template.</param>
/// <param name="Score">Normalized distance to that template (0 = exact, 1 = fully different).</param>
/// <param name="Margin">
/// Normalized distance to the nearest template of a <em>different</em> label minus <paramref name="Score"/>.
/// Higher means the winning label stands out; a small margin signals an ambiguous glyph. 1.0 when no
/// other-labelled template exists to compare against.
/// </param>
/// <param name="Confident">
/// True when <paramref name="Score"/> is within the recognizer's distance tolerance <em>and</em>
/// <paramref name="Margin"/> clears its minimum gap.
/// </param>
public readonly record struct GlyphMatch(string Label, double Score, double Margin, bool Confident);
