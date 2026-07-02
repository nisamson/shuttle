using System.Drawing;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Shuttle.Shl.Api.Models.Index.V1;

public record TeamColors(
    [property: JsonConverter(typeof(ColorJsonConverter))]
    Color Primary,
    [property: JsonConverter(typeof(ColorJsonConverter))]
    Color Secondary,
    [property: JsonConverter(typeof(ColorJsonConverter))]
    Color? Text
);

public class ColorJsonConverter : JsonConverter<Color> {

    public override Color Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        var text = reader.GetString();
        if (text is null) {
            throw new JsonException("Color value is null");
        }
        return ColorTranslator.FromHtml(text);
    }

    public override void Write(Utf8JsonWriter writer, Color value, JsonSerializerOptions options) {
        var colorString = ColorTranslator.ToHtml(value);
        writer.WriteStringValue(colorString);
    }
}
