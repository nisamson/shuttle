using System.Text.RegularExpressions;

namespace Shuttle.Fhm.Vision.Extraction;

/// <summary>
/// The parsed fields of an FHM10 player "bio" line. Only the fields the capture pipeline cares about
/// are surfaced; any that could not be read are left <c>null</c>.
/// </summary>
public readonly record struct FhmBioLine {
    /// <summary>The position codes as shown, e.g. <c>LD/RD</c>.</summary>
    public string? Position { get; init; }

    /// <summary>Height exactly as shown, e.g. <c>6'5"</c>.</summary>
    public string? Height { get; init; }

    /// <summary>Height converted to whole inches (feet * 12 + inches), or <c>null</c> if unreadable.</summary>
    public int? HeightInches { get; init; }

    /// <summary>Weight in pounds.</summary>
    public int? Weight { get; init; }
}

/// <summary>
/// Narrowly-scoped parser for the fixed FHM10 player bio line, e.g.
/// <c>LD/RD | SACRAMENTO EXPRESS | SHOOTS: LEFT | AGE: 23 | 6'5" - 243 LBS | SALARY: $775,000 (1)</c>.
/// The layout is stable within FHM10, so each field is pulled out by a targeted pattern rather than a
/// general splitter. Only position, height and weight are extracted (the values the model needs).
/// </summary>
public static partial class FhmBioLineParser {
    /// <summary>Parses the (possibly OCR-noisy) bio line into its fields.</summary>
    public static FhmBioLine Parse(string? raw) {
        var text = FieldTextParser.NormalizeText(raw);
        if (text.Length == 0) {
            return default;
        }

        return new FhmBioLine {
            Position = ParsePosition(text),
            Height = ParseHeight(text, out var inches),
            HeightInches = inches,
            Weight = ParseWeight(text),
        };
    }

    private static string? ParsePosition(string text) {
        // Position is everything before the first delimiter.
        var pipe = text.IndexOf('|');
        var head = (pipe >= 0 ? text[..pipe] : text).Trim();
        return head.Length == 0 ? null : head;
    }

    private static string? ParseHeight(string text, out int? inches) {
        inches = null;
        var match = HeightRegex().Match(text);
        if (!match.Success) {
            return null;
        }

        var feet = int.Parse(match.Groups["ft"].Value);
        var inch = int.Parse(match.Groups["in"].Value);
        inches = (feet * 12) + inch;
        return $"{feet}'{inch}\"";
    }

    private static int? ParseWeight(string text) {
        var match = WeightRegex().Match(text);
        return match.Success ? int.Parse(match.Groups["lbs"].Value) : null;
    }

    // Feet/inches such as 6'5" (the inch mark may be OCR'd as ", '' or dropped entirely).
    [GeneratedRegex(@"(?<ft>\d+)\s*'\s*(?<in>\d+)", RegexOptions.CultureInvariant)]
    private static partial Regex HeightRegex();

    // Weight such as "243 LBS".
    [GeneratedRegex(@"(?<lbs>\d+)\s*LBS", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex WeightRegex();
}
