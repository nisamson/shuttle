using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Shuttle.Shl.Api.Models;

/// <summary>
/// Reads a <see cref="float"/> that the SHL API may send as either a JSON number
/// or a JSON string (e.g. "0.625"). Writes it back as a number.
/// </summary>
public class FlexibleFloatJsonConverter : JsonConverter<float> {

    public override float Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        switch (reader.TokenType) {
            case JsonTokenType.Number:
                return reader.GetSingle();
            case JsonTokenType.String:
                var stringValue = reader.GetString();
                if (float.TryParse(stringValue, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)) {
                    return parsed;
                }

                throw new JsonException($"Unable to parse '{stringValue}' as a float.");
            default:
                throw new JsonException($"Unexpected token {reader.TokenType} when parsing a float.");
        }
    }

    public override void Write(Utf8JsonWriter writer, float value, JsonSerializerOptions options) {
        writer.WriteNumberValue(value);
    }
}
