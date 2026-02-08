using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace SHLAnalytics.Api.Models;

public partial class ShlDateConverter : JsonConverter<DateOnly> {

    public const string DateFormat = "yyyy-MM-dd";
    
    [GeneratedRegex(@"(\d+)-(\d+)-(\d+)")]
    public static partial Regex DateRegex();

    private static DateOnly ParseDate(string dateString) {
        var match = DateRegex().Match(dateString);
        if (!match.Success) {
            throw new FormatException($"Invalid date format: {dateString}");
        }
        return new DateOnly(
            int.Parse(match.Groups[1].Value),
            int.Parse(match.Groups[2].Value),
            int.Parse(match.Groups[3].Value)
        );
    }

    public override DateOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException($"Expected a JSON string to parse {nameof(DateOnly)}.");

        var s = reader.GetString();
        if (string.IsNullOrWhiteSpace(s)) {
            throw new JsonException($"Cannot parse empty {nameof(DateOnly)} value.");
        }

        try {
            return ParseDate(s!);
        } catch (FormatException ex) {
            throw new JsonException($"Error parsing {nameof(DateOnly)} from string '{s}'.", ex);
        }
    }

    public override void Write(Utf8JsonWriter writer, DateOnly value, JsonSerializerOptions options) {
        writer.WriteStringValue(value.ToString(DateFormat, CultureInfo.InvariantCulture));
    }
}
