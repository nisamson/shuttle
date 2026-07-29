namespace Shuttle.Fhm.Vision.Layout;

/// <summary>How the text read from a region should be interpreted.</summary>
public enum FieldKind {
    /// <summary>Free text (name, birthplace, position label…).</summary>
    Text,

    /// <summary>A whole-number rating; all non-digit characters are stripped before parsing.</summary>
    Integer,

    /// <summary>
    /// A decimal number (e.g. a salary or a fractional rating). Digits, one decimal point and a
    /// leading minus are kept; currency symbols, thousands separators and units are stripped.
    /// </summary>
    Float,

    /// <summary>
    /// The fixed FHM10 player "bio" line that concatenates several values, e.g.
    /// <c>LD/RD | SACRAMENTO EXPRESS | SHOOTS: LEFT | AGE: 23 | 6'5" - 243 LBS | SALARY: $775,000 (1)</c>.
    /// A dedicated parser pulls the individual fields out; the region's <c>Key</c>/<c>Group</c> are
    /// ignored. See <see cref="Extraction.FhmBioLineParser"/>.
    /// </summary>
    Bio,
}

/// <summary>Which logical group a captured field belongs to.</summary>
public enum FieldGroup {
    /// <summary>Identity fields: name, jersey number, position, handedness.</summary>
    Identity,

    /// <summary>A raw player attribute rating.</summary>
    Attribute,

    /// <summary>A derived per-role rating.</summary>
    Role,

    /// <summary>Anything else worth keeping as text.</summary>
    Other,
}

/// <summary>
/// A rectangle expressed as fractions (0..1) of the captured window's width and height, so a
/// profile is independent of the exact resolution/DPI the screenshot was taken at.
/// </summary>
public readonly record struct RatioRect(double X, double Y, double Width, double Height) {
    /// <summary>Maps this ratio rectangle onto a concrete pixel rectangle for a given image size.</summary>
    public PixelRect ToPixels(int imageWidth, int imageHeight) {
        var left = (int)Math.Round(X * imageWidth);
        var top = (int)Math.Round(Y * imageHeight);
        var width = (int)Math.Round(Width * imageWidth);
        var height = (int)Math.Round(Height * imageHeight);

        // Clamp to the image bounds so a slightly over-sized region never reads out of range.
        left = Math.Clamp(left, 0, Math.Max(0, imageWidth - 1));
        top = Math.Clamp(top, 0, Math.Max(0, imageHeight - 1));
        width = Math.Clamp(width, 1, imageWidth - left);
        height = Math.Clamp(height, 1, imageHeight - top);

        return new PixelRect(left, top, width, height);
    }

    /// <summary>Builds a ratio rectangle from a pixel rectangle relative to an image size.</summary>
    public static RatioRect FromPixels(int x, int y, int width, int height, int imageWidth, int imageHeight) {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(imageWidth, 0);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(imageHeight, 0);
        return new RatioRect(
            (double)x / imageWidth,
            (double)y / imageHeight,
            (double)width / imageWidth,
            (double)height / imageHeight);
    }
}

/// <summary>A concrete pixel rectangle inside a captured image.</summary>
public readonly record struct PixelRect(int X, int Y, int Width, int Height);

/// <summary>One OCR field: where to read it, how to interpret it, and which group it belongs to.</summary>
public sealed record FieldRegion {
    public required string Key { get; init; }
    public required FieldGroup Group { get; init; }
    public required FieldKind Kind { get; init; }
    public required RatioRect Bounds { get; init; }
}

/// <summary>
/// A region that, when it contains <see cref="ExpectedText"/> (case-insensitive substring), confirms
/// the current window really is an FHM player-info screen. All anchors must match.
/// </summary>
public sealed record AnchorMarker {
    public required RatioRect Bounds { get; init; }
    public required string ExpectedText { get; init; }
}

/// <summary>
/// The full layout map for a known FHM player-info screen: the anchors used to recognise the screen
/// and the field regions to OCR. Coordinates are resolution-independent ratios.
/// </summary>
public sealed record LayoutProfile {
    public required string Name { get; init; }
    public IReadOnlyList<AnchorMarker> Anchors { get; init; } = [];
    public IReadOnlyList<FieldRegion> Regions { get; init; } = [];
}
