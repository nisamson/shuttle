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
