using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Shuttle.Fhm.Vision.Layout;
using Shuttle.Fhm.Vision.Ocr;

namespace Shuttle.Fhm.Vision.Tests;

/// <summary>
/// A deterministic <see cref="IOcrEngine"/> for tests: it decodes the cropped region PNG, reads its
/// top-left pixel, and returns the string registered for that colour. This exercises the real region
/// cropping and ratio→pixel mapping while keeping OCR results predictable.
/// </summary>
internal sealed class ColorMapOcrEngine : IOcrEngine {
    private readonly IReadOnlyDictionary<Rgba32, string> _map;

    public ColorMapOcrEngine(IReadOnlyDictionary<Rgba32, string> map) {
        _map = map;
    }

    public async Task<string> RecognizeAsync(ReadOnlyMemory<byte> imagePng, CancellationToken cancellationToken) {
        using var image = Image.Load<Rgba32>(imagePng.ToArray());
        var color = image[0, 0];
        await Task.CompletedTask;
        return _map.TryGetValue(color, out var text) ? text : string.Empty;
    }
}

internal static class TestImageFactory {
    /// <summary>Creates an image and fills each region's pixel rectangle with the given colour.</summary>
    public static Image<Rgba32> WithRegions(
        int width,
        int height,
        IEnumerable<(RatioRect Bounds, Rgba32 Color)> regions
    ) {
        var image = new Image<Rgba32>(width, height, new Rgba32(0, 0, 0));
        foreach (var (bounds, color) in regions) {
            var rect = bounds.ToPixels(width, height);
            for (var y = rect.Y; y < rect.Y + rect.Height; y++) {
                for (var x = rect.X; x < rect.X + rect.Width; x++) {
                    image[x, y] = color;
                }
            }
        }

        return image;
    }
}
