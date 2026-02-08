using System.Text.Json;
using System.Text.Json.Serialization;

namespace SHLAnalytics.Api.Models.Common;

[Flags]
[JsonConverter(typeof(PositionConverter))]
public enum SkaterPosition {
    Left = 1,
    Right = 1 << 1,
    Forward = 1 << 2,
    LeftWing = Left | Forward,
    RightWing = Right | Forward,
    Center = Forward,
    RightDefense = Right,
    LeftDefense = Left,
}

public static class PositionExtensions {

    extension(SkaterPosition skaterPosition) {

        public string ToShortString() {
            return skaterPosition switch {
                SkaterPosition.Center => "C",
                SkaterPosition.LeftWing => "LW",
                SkaterPosition.RightWing => "RW",
                SkaterPosition.LeftDefense => "LD",
                SkaterPosition.RightDefense => "RD",
                _ => throw new NotImplementedException($"Unknown position {skaterPosition}"),
            };
        }

        public string ToLongString() {
            return skaterPosition switch {
                SkaterPosition.Center => "Center",
                SkaterPosition.LeftWing => "Left Wing",
                SkaterPosition.RightWing => "Right Wing",
                SkaterPosition.LeftDefense => "Left Defense",
                SkaterPosition.RightDefense => "Right Defense",
                _ => throw new NotImplementedException($"Unknown position {skaterPosition}"),
            };
        }

        public static SkaterPosition FromString(string shortString) {
            return shortString.ToUpper() switch {
                "C" or "Center" => SkaterPosition.Center,
                "LW" or "Left Wing" => SkaterPosition.LeftWing,
                "RW" or "Right Wing" => SkaterPosition.RightWing,
                "LD" or "Left Defense" => SkaterPosition.LeftDefense,
                "RD" or "Right Defense" => SkaterPosition.RightDefense,
                _ => throw new ArgumentException("Invalid position string", nameof(shortString)),
            };
        }

        public bool IsForward => skaterPosition.HasFlag(SkaterPosition.Forward);
        public bool IsDefense => !skaterPosition.HasFlag(SkaterPosition.Forward);
        public bool IsLeft => skaterPosition.HasFlag(SkaterPosition.Left);
        public bool IsRight => skaterPosition.HasFlag(SkaterPosition.Right);
    }
}

public class PositionConverter : JsonConverter<SkaterPosition> {
    public override SkaterPosition Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        // get the string value
        var str = reader.GetString();
        if (str == null) {
            throw new JsonException("Position string is null");
        }
        return SkaterPosition.FromString(str);
    }
    public override void Write(Utf8JsonWriter writer, SkaterPosition value, JsonSerializerOptions options) {
        writer.WriteStringValue(value.ToShortString());
    }
}
