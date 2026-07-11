using System.Text.Json;
using System.Text.Json.Serialization;

namespace Shuttle.Shl.Api.Models.Common;

[Flags]
[JsonConverter(typeof(PositionConverter))]
public enum PlayerPosition {
    Goalie = 0,
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

    extension(PlayerPosition playerPosition) {

        public string ToShortString() {
            return playerPosition switch {
                PlayerPosition.Goalie => "G",
                PlayerPosition.Center => "C",
                PlayerPosition.LeftWing => "LW",
                PlayerPosition.RightWing => "RW",
                PlayerPosition.LeftDefense => "LD",
                PlayerPosition.RightDefense => "RD",
                _ => throw new NotImplementedException($"Unknown position {playerPosition}"),
            };
        }

        public string ToLongString() {
            return playerPosition switch {
                PlayerPosition.Goalie => "Goalie",
                PlayerPosition.Center => "Center",
                PlayerPosition.LeftWing => "Left Wing",
                PlayerPosition.RightWing => "Right Wing",
                PlayerPosition.LeftDefense => "Left Defense",
                PlayerPosition.RightDefense => "Right Defense",
                _ => throw new NotImplementedException($"Unknown position {playerPosition}"),
            };
        }

        public static PlayerPosition FromString(string shortString) {
            return TryFromString(shortString, out var position)
                ? position
                : throw new ArgumentException("Invalid position string", nameof(shortString));
        }

        public static bool TryFromString(string? shortString, out PlayerPosition position) {
            switch (shortString?.Trim().ToLowerInvariant()) {
                case "g" or "goalie":
                    position = PlayerPosition.Goalie;
                    return true;
                case "c" or "center":
                    position = PlayerPosition.Center;
                    return true;
                case "lw" or "left wing":
                    position = PlayerPosition.LeftWing;
                    return true;
                case "rw" or "right wing":
                    position = PlayerPosition.RightWing;
                    return true;
                case "ld" or "left defense":
                    position = PlayerPosition.LeftDefense;
                    return true;
                case "rd" or "right defense":
                    position = PlayerPosition.RightDefense;
                    return true;
                default:
                    position = default;
                    return false;
            }
        }

        public bool IsForward => playerPosition.HasFlag(PlayerPosition.Forward);
        public bool IsDefense => !playerPosition.HasFlag(PlayerPosition.Forward);
        public bool IsLeft => playerPosition.HasFlag(PlayerPosition.Left);
        public bool IsRight => playerPosition.HasFlag(PlayerPosition.Right);
        public bool IsGoalie => playerPosition is { IsDefense: true, IsLeft: false, IsRight: false };
    }
}

public class PositionConverter : JsonConverter<PlayerPosition> {
    public override PlayerPosition Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        // get the string value
        var str = reader.GetString();
        if (str == null) {
            throw new JsonException("Position string is null");
        }
        return PlayerPosition.FromString(str);
    }
    public override void Write(Utf8JsonWriter writer, PlayerPosition value, JsonSerializerOptions options) {
        writer.WriteStringValue(value.ToShortString());
    }
}
