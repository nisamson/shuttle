using System.Text.Json;
using System.Text.Json.Serialization;

namespace SHLAnalytics.Api.Models;

public class IntBoolJsonConverter : JsonConverter<bool> {

    public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        var intValue = reader.GetInt32();
        return intValue != 0;
    }
    public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options) {
        writer.WriteNumberValue(value ? 1 : 0);
    }
}
