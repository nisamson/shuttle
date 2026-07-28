namespace Shuttle.Fhm.Vision.Ocr;

/// <summary>
/// Recognises the text contained in a single cropped region image. Implementations wrap a concrete
/// OCR backend (Windows.Media.Ocr, Tesseract, an ONNX/ML.NET model, …) behind this seam so the
/// capture/extraction pipeline is engine-agnostic and unit-testable with a fake engine.
/// </summary>
public interface IOcrEngine {
    /// <summary>
    /// Recognises text from a PNG-encoded region image and returns the raw recognised string
    /// (whitespace-joined). Callers apply any field-specific parsing (e.g. digit extraction).
    /// </summary>
    Task<string> RecognizeAsync(ReadOnlyMemory<byte> imagePng, CancellationToken cancellationToken);
}
