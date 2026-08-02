using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Shuttle.Shl.Api.Models.Common;

[JsonConverter(typeof(HeightJsonConverter))]
public partial record Height(int Feet, int Inches) {
    [GeneratedRegex("""(\d+)(ft\.?|'|’)\s*(\d+)(in\.?|"|”)""", RegexOptions.IgnoreCase)]
    private static partial Regex HeightRegex();
    
    public static Height Parse(string value) {
        if (!TryParse(value, out var result)) {
            throw new FormatException($"Invalid height format: '{value}'. Expected format: '6ft 0in'");
        }
        return result;
    }
    
    public static bool TryParse(string? value, [NotNullWhen(true)] out Height? result) {
        result = null;
        if (value is null) {
            return false;
        }
        var match = HeightRegex().Match(value);
        if (!match.Success) {
            return false;
        }
        result = new Height(int.Parse(match.Groups[1].Value), int.Parse(match.Groups[3].Value)).Normalized();
        return true;
    }
    
    public override string ToString() => $"{Feet}ft {Inches}in";
    
    [JsonIgnore]
    public int TotalInches => Feet * 12 + Inches;
    
    [JsonIgnore]
    public int TotalCentimeters => (int)Math.Round(TotalInches * 2.54);
    
    public Height Normalized() => new Height(Feet + Inches / 12, Inches % 12);
}

public class HeightJsonConverter : JsonConverter<Height> {
    public override Height Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        var text = reader.GetString();
        if (text is null) {
            throw new JsonException("Height value is null");
        }
        return Height.Parse(text);
    }
    
    public override void Write(Utf8JsonWriter writer, Height value, JsonSerializerOptions options) {
        writer.WriteStringValue(value.ToString());
    }
}

