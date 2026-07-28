using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Shuttle.Fhm.Vision.Layout;
using Shuttle.Fhm.Vision.Model;
using Shuttle.Fhm.Vision.Ocr;

namespace Shuttle.Fhm.Vision.Extraction;

/// <summary>
/// Turns a captured FHM window image into a <see cref="FhmPlayerCapture"/> by OCR'ing each region
/// defined in a <see cref="LayoutProfile"/> and parsing the result according to the region's kind
/// and group.
/// </summary>
public sealed class RegionExtractor {
    /// <summary>Reserved identity field keys recognised inside <see cref="FieldGroup.Identity"/>.</summary>
    public const string NameKey = "name";

    public const string NumberKey = "number";
    public const string PositionKey = "position";
    public const string HandednessKey = "handedness";

    /// <summary>Text-field key holding the raw height from a bio line (e.g. <c>6'5"</c>).</summary>
    public const string HeightKey = "height";

    /// <summary>Number-field key holding the height in whole inches parsed from a bio line.</summary>
    public const string HeightInchesKey = "heightInches";

    /// <summary>Number-field key holding the weight in pounds parsed from a bio line.</summary>
    public const string WeightKey = "weight";

    private readonly IOcrEngine ocr;
    private readonly ILogger logger;

    public RegionExtractor(IOcrEngine ocr, ILogger<RegionExtractor>? logger = null) {
        ArgumentNullException.ThrowIfNull(ocr);
        this.ocr = ocr;
        this.logger = logger ?? NullLogger<RegionExtractor>.Instance;
    }

    /// <summary>
    /// Returns true when every anchor's region contains its expected text (case-insensitive), i.e.
    /// the image looks like the player-info screen this profile describes. A profile with no anchors
    /// is always accepted.
    /// </summary>
    public async Task<bool> IsPlayerScreenAsync(
        Image<Rgba32> image,
        LayoutProfile profile,
        CancellationToken cancellationToken
    ) {
        ArgumentNullException.ThrowIfNull(image);
        ArgumentNullException.ThrowIfNull(profile);

        foreach (var anchor in profile.Anchors) {
            var text = await RecognizeAsync(image, anchor.Bounds, cancellationToken);
            if (text.IndexOf(anchor.ExpectedText, StringComparison.OrdinalIgnoreCase) < 0) {
                return false;
            }
        }

        return true;
    }

    /// <summary>OCRs and parses every field region into an immutable capture record.</summary>
    public async Task<FhmPlayerCapture> ExtractAsync(
        Image<Rgba32> image,
        LayoutProfile profile,
        DateTimeOffset capturedAtUtc,
        CancellationToken cancellationToken
    ) {
        ArgumentNullException.ThrowIfNull(image);
        ArgumentNullException.ThrowIfNull(profile);

        var name = string.Empty;
        int? number = null;
        string? position = null;
        string? handedness = null;
        var attributes = new Dictionary<string, int>(StringComparer.Ordinal);
        var roles = new Dictionary<string, int>(StringComparer.Ordinal);
        var numbers = new Dictionary<string, double>(StringComparer.Ordinal);
        var textFields = new Dictionary<string, string>(StringComparer.Ordinal);

        var sink = new FieldSink(attributes, roles, numbers, textFields);

        foreach (var region in profile.Regions) {
            var raw = await RecognizeAsync(image, region.Bounds, cancellationToken);

            if (region.Kind == FieldKind.Bio) {
                ApplyBioLine(raw, sink, ref position);
                continue;
            }

            AssignField(region.Key, region.Group, region.Kind, raw, sink,
                ref name, ref number, ref position, ref handedness);
        }

        return new FhmPlayerCapture {
            CapturedAtUtc = capturedAtUtc,
            Name = name,
            JerseyNumber = number,
            Position = position,
            Handedness = handedness,
            Attributes = attributes,
            RoleRatings = roles,
            Numbers = numbers,
            TextFields = textFields,
        };
    }

    /// <summary>Mutable collectors for the non-scalar capture fields, passed by reference to assignment.</summary>
    private readonly record struct FieldSink(
        Dictionary<string, int> Attributes,
        Dictionary<string, int> Roles,
        Dictionary<string, double> Numbers,
        Dictionary<string, string> TextFields
    );

    /// <summary>
    /// Parses the fixed FHM10 bio line and stores the fields we care about: position (identity),
    /// height (raw text plus <c>heightInches</c> as a number) and weight.
    /// </summary>
    private void ApplyBioLine(string raw, FieldSink sink, ref string? position) {
        var bio = FhmBioLineParser.Parse(raw);

        if (!string.IsNullOrEmpty(bio.Position)) {
            position = bio.Position;
        }

        if (bio.Height is not null) {
            sink.TextFields[HeightKey] = bio.Height;
        }

        if (bio.HeightInches is { } inches) {
            sink.Numbers[HeightInchesKey] = inches;
        }

        if (bio.Weight is { } weight) {
            sink.Numbers[WeightKey] = weight;
        }

        if (bio is { Position: null, Height: null, HeightInches: null, Weight: null }) {
            logger.LogWarning("Could not read any bio-line fields from OCR text '{Raw}'", raw);
        }
    }

    private void AssignField(
        string key,
        FieldGroup group,
        FieldKind kind,
        string raw,
        FieldSink sink,
        ref string name,
        ref int? number,
        ref string? position,
        ref string? handedness
    ) {
        if (group == FieldGroup.Identity) {
            if (key.Equals(NameKey, StringComparison.OrdinalIgnoreCase)) {
                name = FieldTextParser.NormalizeText(raw);
                return;
            }

            if (key.Equals(NumberKey, StringComparison.OrdinalIgnoreCase)) {
                number = FieldTextParser.ParseInteger(raw);
                return;
            }

            if (key.Equals(PositionKey, StringComparison.OrdinalIgnoreCase)) {
                position = FieldTextParser.NormalizeText(raw);
                return;
            }

            if (key.Equals(HandednessKey, StringComparison.OrdinalIgnoreCase)) {
                handedness = FieldTextParser.NormalizeText(raw);
                return;
            }
        }

        switch (kind) {
            case FieldKind.Float:
                AddDecimal(key, group, raw, sink.Numbers);
                break;

            case FieldKind.Integer when group == FieldGroup.Attribute:
                AddNumeric(key, group, raw, sink.Attributes);
                break;

            case FieldKind.Integer when group == FieldGroup.Role:
                AddNumeric(key, group, raw, sink.Roles);
                break;

            case FieldKind.Integer:
                // Numeric metadata outside the attribute/role vectors (e.g. weight, age) is kept as
                // a number so it is usable as an ML feature.
                AddDecimal(key, group, raw, sink.Numbers);
                break;

            default:
                sink.TextFields[key] = FieldTextParser.NormalizeText(raw);
                break;
        }
    }

    private void AddNumeric(string key, FieldGroup group, string raw, Dictionary<string, int> target) {
        var value = FieldTextParser.ParseInteger(raw);
        if (value is null) {
            logger.LogWarning(
                "Could not parse an integer for {Group} field '{Key}' from OCR text '{Raw}'",
                group, key, raw);
            return;
        }

        target[key] = value.Value;
    }

    private void AddDecimal(string key, FieldGroup group, string raw, Dictionary<string, double> target) {
        var value = FieldTextParser.ParseDecimal(raw);
        if (value is null) {
            logger.LogWarning(
                "Could not parse a number for {Group} field '{Key}' from OCR text '{Raw}'",
                group, key, raw);
            return;
        }

        target[key] = value.Value;
    }

    private async Task<string> RecognizeAsync(Image<Rgba32> image, RatioRect bounds, CancellationToken cancellationToken) {
        var pixels = bounds.ToPixels(image.Width, image.Height);
        var png = await RegionImaging.CropForOcrAsync(image, pixels, cancellationToken);
        return await ocr.RecognizeAsync(png, cancellationToken);
    }
}
