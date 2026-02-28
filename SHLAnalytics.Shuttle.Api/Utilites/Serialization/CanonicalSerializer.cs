using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace SHLAnalytics.Shuttle.Api.Utilites.Serialization;

public static class CanonicalSerializer {

    public static string Canonicalize<T>(T? obj) {

        if (obj is null) {
            return "null";
        }

        var options = new JsonSerializerOptions {
            WriteIndented = false,
        };
        var json = JsonSerializer.SerializeToDocument(options);
        return Canonicalize(json);
    }

    public static string Canonicalize(JsonDocument node) {
        var options = new JsonWriterOptions {
            Indented = false,
            SkipValidation = true
        };
        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream, options);
        Canonicalize(writer, node);
        writer.Flush();
        return Encoding.UTF8.GetString(stream.GetBuffer());
    }

    private static void Canonicalize(Utf8JsonWriter writer, JsonDocument node) {
        var root = node.RootElement;
        Canonicalize(writer, root);
    }
    
    private static void Canonicalize(Utf8JsonWriter writer, JsonElement elem) {
        switch (elem.ValueKind) {
            case JsonValueKind.Array:
                writer.WriteStartArray();
                foreach (var item in elem.EnumerateArray()) {
                    Canonicalize(writer, item);
                }

                writer.WriteEndArray();
                break;
            case JsonValueKind.Object:
                CanonicalizeObject(writer, elem);
                break;
            case JsonValueKind.Undefined:
                break;
            case JsonValueKind.String:
            case JsonValueKind.Number:
            case JsonValueKind.True:
            case JsonValueKind.False:
            case JsonValueKind.Null:
                JsonSerializer.Serialize(writer, elem);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private static void CanonicalizeObject(Utf8JsonWriter writer, JsonElement elem) {
        ArgumentOutOfRangeException.ThrowIfNotEqual(elem.ValueKind, JsonValueKind.Object);
        writer.WriteStartObject();
        var properties = elem.EnumerateObject().OrderBy(p => p.Name, StringComparer.Ordinal);
        foreach (var prop in properties) {
            writer.WritePropertyName(prop.Name);
            Canonicalize(writer, prop.Value);
        }

        writer.WriteEndObject();
    }

}
