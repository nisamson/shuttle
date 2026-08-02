using System.Text.Json.Serialization;

namespace Shuttle.Shl.Api.Models.Portal.V1;

[JsonConverter(typeof(JsonStringEnumConverter<PlayerHandedness>))]
public enum PlayerHandedness {
    Left,
    Right
}

public static class PlayerHandednessExtensions {
    extension(PlayerHandedness @this) {
        public string ToValueString() => @this switch {
            PlayerHandedness.Left => "Left",
            PlayerHandedness.Right => "Right",
            _ => throw new ArgumentOutOfRangeException(nameof(@this), @this, null)
        };

        public static PlayerHandedness FromString(string handedness) => handedness.ToLower() switch {
            "left" => PlayerHandedness.Left,
            "right" => PlayerHandedness.Right,
            _ => throw new ArgumentOutOfRangeException(nameof(handedness), handedness, null)
        };
    }
}
