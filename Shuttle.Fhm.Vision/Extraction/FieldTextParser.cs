using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Shuttle.Fhm.Vision.Extraction;

/// <summary>Helpers for turning raw OCR strings into normalized field values.</summary>
public static partial class FieldTextParser {
    /// <summary>Collapses runs of whitespace and trims — the canonical form for a text field.</summary>
    public static string NormalizeText(string? raw) {
        if (string.IsNullOrWhiteSpace(raw)) {
            return string.Empty;
        }

        var builder = new StringBuilder(raw.Length);
        var previousWasSpace = false;
        foreach (var ch in raw.Trim()) {
            if (char.IsWhiteSpace(ch)) {
                if (!previousWasSpace) {
                    builder.Append(' ');
                }

                previousWasSpace = true;
            } else {
                builder.Append(ch);
                previousWasSpace = false;
            }
        }

        return builder.ToString();
    }

    /// <summary>
    /// Extracts a whole number from an OCR string by keeping digits (and a leading minus). OCR of a
    /// clean numeric cell can include stray marks, so everything else is discarded. Returns
    /// <c>null</c> when no digits are present.
    /// </summary>
    public static int? ParseInteger(string? raw) {
        if (string.IsNullOrWhiteSpace(raw)) {
            return null;
        }

        var digits = new StringBuilder(raw.Length);
        var negative = false;
        var seenDigit = false;
        foreach (var ch in raw) {
            if (char.IsDigit(ch)) {
                digits.Append(ch);
                seenDigit = true;
            } else if (ch == '-' && !seenDigit && digits.Length == 0) {
                negative = true;
            }
        }

        if (!seenDigit) {
            return null;
        }

        if (!int.TryParse(digits.ToString(), NumberStyles.None, CultureInfo.InvariantCulture, out var value)) {
            return null;
        }

        return negative ? -value : value;
    }

    /// <summary>
    /// Extracts the first decimal number from an OCR string. Commas are treated as thousands
    /// separators and removed; a single <c>.</c> is the decimal point. Currency symbols, units and
    /// any trailing tokens are ignored. Returns <c>null</c> when no number is present.
    /// </summary>
    public static double? ParseDecimal(string? raw) {
        if (string.IsNullOrWhiteSpace(raw)) {
            return null;
        }

        var match = DecimalRegex().Match(raw);
        if (!match.Success) {
            return null;
        }

        var normalized = match.Value.Replace(",", string.Empty, StringComparison.Ordinal);
        if (!double.TryParse(normalized, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture, out var value)) {
            return null;
        }

        return value;
    }

    // First numeric token: optional sign, digit groups (optionally comma-separated), optional decimal part.
    [GeneratedRegex(@"-?\d[\d,]*(?:\.\d+)?", RegexOptions.CultureInvariant)]
    private static partial Regex DecimalRegex();
}
