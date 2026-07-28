using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Shuttle.Fhm.Vision.Layout;

namespace Shuttle.Fhm.Vision.Extraction;

/// <summary>Cropping/encoding helpers shared by the extractor and calibration tooling.</summary>
public static class RegionImaging {
    /// <summary>
    /// Windows OCR returns nothing on very small crops. When feeding a region to OCR we upscale it so
    /// its shorter side is at least this many pixels, which makes small rating boxes recognisable.
    /// </summary>
    public const int OcrMinDimension = 64;

    /// <summary>Windows OCR rejects images whose dimensions exceed this; upscaling is capped to stay under it.</summary>
    private const int MaxDimension = 10000;

    /// <summary>
    /// Minimum value each RGB channel must reach for a pixel to count as "white" text when isolating
    /// the (white) numeric text FHM renders on a coloured/dark background.
    /// </summary>
    public const byte WhiteTextThreshold = 170;

    /// <summary>Crops <paramref name="rect"/> out of <paramref name="image"/> and PNG-encodes it.</summary>
    public static async Task<byte[]> CropToPngAsync(
        Image<Rgba32> image,
        PixelRect rect,
        CancellationToken cancellationToken
    ) {
        ArgumentNullException.ThrowIfNull(image);
        using var cropped = image.Clone(ctx =>
            ctx.Crop(new SixLabors.ImageSharp.Rectangle(rect.X, rect.Y, rect.Width, rect.Height)));
        using var stream = new MemoryStream();
        await cropped.SaveAsPngAsync(stream, cancellationToken);
        return stream.ToArray();
    }

    /// <summary>
    /// Crops <paramref name="rect"/> and, if it is smaller than <see cref="OcrMinDimension"/> on its
    /// shorter side, upscales it by an integer factor before PNG-encoding, so Windows OCR can read
    /// small regions (e.g. a tight box around a two-digit rating). When
    /// <paramref name="isolateWhiteText"/> is set, the crop is first binarised to black text on a
    /// white background by keeping only near-white pixels, which greatly improves OCR of FHM's white
    /// numeric text over its coloured/dark background.
    /// </summary>
    public static async Task<byte[]> CropForOcrAsync(
        Image<Rgba32> image,
        PixelRect rect,
        CancellationToken cancellationToken,
        int minDimension = OcrMinDimension,
        bool isolateWhiteText = false
    ) {
        ArgumentNullException.ThrowIfNull(image);
        using var cropped = image.Clone(ctx =>
            ctx.Crop(new SixLabors.ImageSharp.Rectangle(rect.X, rect.Y, rect.Width, rect.Height)));

        if (isolateWhiteText) {
            BinarizeWhiteText(cropped, WhiteTextThreshold);
        }

        var scale = ComputeUpscale(cropped.Width, cropped.Height, minDimension);
        if (scale > 1) {
            cropped.Mutate(ctx => ctx.Resize(cropped.Width * scale, cropped.Height * scale, KnownResamplers.Bicubic));
        }

        using var stream = new MemoryStream();
        await cropped.SaveAsPngAsync(stream, cancellationToken);
        return stream.ToArray();
    }

    /// <summary>
    /// Rewrites <paramref name="image"/> to black-on-white: pixels whose R, G and B are all at least
    /// <paramref name="threshold"/> (i.e. white/light text) become black; everything else becomes white.
    /// </summary>
    private static void BinarizeWhiteText(Image<Rgba32> image, byte threshold) {
        image.ProcessPixelRows(accessor => {
            for (var y = 0; y < accessor.Height; y++) {
                var row = accessor.GetRowSpan(y);
                for (var x = 0; x < row.Length; x++) {
                    ref var pixel = ref row[x];
                    var isText = pixel.R >= threshold && pixel.G >= threshold && pixel.B >= threshold;
                    var value = isText ? (byte)0 : (byte)255;
                    pixel = new Rgba32(value, value, value, 255);
                }
            }
        });
    }

    /// <summary>Integer upscale factor that lifts the shorter side to at least <paramref name="minDimension"/>, capped by <see cref="MaxDimension"/>.</summary>
    private static int ComputeUpscale(int width, int height, int minDimension) {
        var shortSide = Math.Min(width, height);
        if (shortSide <= 0 || shortSide >= minDimension) {
            return 1;
        }

        var scale = (int)Math.Ceiling((double)minDimension / shortSide);
        var longSide = Math.Max(width, height);
        while (scale > 1 && longSide * scale > MaxDimension) {
            scale--;
        }

        return scale;
    }
}
