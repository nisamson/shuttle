using System.Runtime.Versioning;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.Storage.Streams;

namespace Shuttle.Fhm.Vision.Ocr;

/// <summary>
/// <see cref="IOcrEngine"/> backed by the built-in Windows OCR engine (<c>Windows.Media.Ocr</c>).
/// Offline, requires no external native binaries, and uses the installed OCR language packs.
/// </summary>
/// <remarks>
/// This is the default engine. Alternatives evaluated in the plan (Tesseract, an ONNX/ML.NET digit
/// model) can be dropped in behind <see cref="IOcrEngine"/> without touching the pipeline. The
/// ML.NET/ONNX route is the strongest fit for the clean, fixed-font numeric attribute grid.
/// </remarks>
[SupportedOSPlatform("windows10.0.19041.0")]
public sealed class WindowsMediaOcrEngine : IOcrEngine {
    private readonly OcrEngine engine;

    public WindowsMediaOcrEngine() {
        engine = OcrEngine.TryCreateFromUserProfileLanguages()
                  ?? throw new InvalidOperationException(
                      "No Windows OCR language pack is available. Install one via "
                      + "Settings > Time & language > Language & region.");
    }

    public async Task<string> RecognizeAsync(ReadOnlyMemory<byte> imagePng, CancellationToken cancellationToken) {
        using var stream = new InMemoryRandomAccessStream();

        var writer = new DataWriter(stream);
        writer.WriteBytes(imagePng.ToArray());
        await writer.StoreAsync().AsTask(cancellationToken);
        await writer.FlushAsync().AsTask(cancellationToken);
        writer.DetachStream();
        stream.Seek(0);

        var decoder = await BitmapDecoder.CreateAsync(stream).AsTask(cancellationToken);
        using var decoded = await decoder.GetSoftwareBitmapAsync().AsTask(cancellationToken);

        // Windows OCR requires Bgra8/Gray8; PNG decode may yield another format.
        using var bitmap = decoded.BitmapPixelFormat == BitmapPixelFormat.Bgra8
            ? SoftwareBitmap.Copy(decoded)
            : SoftwareBitmap.Convert(decoded, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);

        var result = await engine.RecognizeAsync(bitmap).AsTask(cancellationToken);
        return result.Text ?? string.Empty;
    }
}
