using System.Text.Json;
using System.Text.Json.Serialization;

namespace SHLAnalytics.Api.Models.Portal.V1;

[JsonConverter(typeof(PlayerStatusJsonConverter))]
public enum PlayerStatus {
    Pending,
    Denied,
    Active,
    Retired
}

public static class PlayerStatusExtensions {
    extension(PlayerStatus @this) {
        public string ToValueString() => @this switch {
            PlayerStatus.Pending => "pending",
            PlayerStatus.Denied => "denied",
            PlayerStatus.Active => "active",
            PlayerStatus.Retired => "retired",
            _ => throw new ArgumentOutOfRangeException(nameof(@this), @this, null)
        };
        
        public static PlayerStatus FromString(string status) => status.ToLower() switch {
            "pending" => PlayerStatus.Pending,
            "denied" => PlayerStatus.Denied,
            "active" => PlayerStatus.Active,
            "retired" => PlayerStatus.Retired,
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
        };
    }
}

public class PlayerStatusJsonConverter : JsonConverter<PlayerStatus> {

    public override PlayerStatus Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        var text = reader.GetString();
        if (text is null) {
            throw new JsonException("PlayerStatus value is null");
        }
        return PlayerStatus.FromString(text);
    }
    public override void Write(Utf8JsonWriter writer, PlayerStatus value, JsonSerializerOptions options) {
        writer.WriteStringValue(value.ToValueString());
    }
}