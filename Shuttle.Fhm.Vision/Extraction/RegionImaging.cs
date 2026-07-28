using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Shuttle.Fhm.Vision.Layout;

namespace Shuttle.Fhm.Vision.Extraction;

/// <summary>Cropping/encoding helpers shared by the extractor and calibration tooling.</summary>
public static class RegionImaging {
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
}
