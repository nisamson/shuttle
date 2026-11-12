using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.PixelFormats;

namespace Shuttle.Fhm.Vision.Capture;

public static class ImageFileLoader
{
    public static async Task<Image<Rgba32>> LoadAsync(FileInfo file, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(file);

        await using var stream = file.OpenRead();
        var header = new byte[4];
        var bytesRead = 0;
        while (bytesRead < header.Length)
        {
            var read = await stream.ReadAsync(header.AsMemory(bytesRead), cancellationToken);
            if (read == 0)
            {
                break;
            }

            bytesRead += read;
        }

        stream.Position = 0;
        if (IsTiff(header))
        {
            throw new NotSupportedException($"TIFF images are not supported: {file.FullName}");
        }

        var options = new DecoderOptions { SkipMetadata = true };
        var image = await SixLabors.ImageSharp.Image.LoadAsync<Rgba32>(options, stream, cancellationToken);
        image.Metadata.IccProfile = null;
        return image;
    }

    private static bool IsTiff(byte[] header) =>
        header[0] == 0x49 && header[1] == 0x49 && (header[2] is 0x2A or 0x2B) && header[3] == 0x00
        || header[0] == 0x4D && header[1] == 0x4D && header[2] == 0x00 && (header[3] is 0x2A or 0x2B);
}
