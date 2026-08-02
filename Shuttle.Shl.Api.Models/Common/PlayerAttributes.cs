using System.Text.Json;
using System.Text.Json.Serialization;
using Shuttle.Shl.Api.Models.Common.Mixins;

namespace Shuttle.Shl.Api.Models.Common;

[JsonConverter(typeof(PlayerAttributesJsonConverter))]
public abstract record PlayerAttributes;

public record SkaterAttributes(
    int Checking,
    int Stickchecking,
    int Hitting,
    int Positioning,
    int ShotBlocking,
    int DefensiveRead,
    int Aggression,
    int Bravery,
    int Determination,
    int TeamPlayer,
    int Leadership,
    int Temperament,
    int Professionalism,
    int Screening,
    int GettingOpen,
    int Passing,
    int Puckhandling,
    int ShootingAccuracy,
    int ShootingRange,
    int OffensiveRead,
    int Acceleration,
    int Agility,
    int Balance,
    int Speed,
    int Stamina,
    int Strength,
    int Fighting,
    int Faceoffs
) : PlayerAttributes, ISkaterRatings;

public record GoaltenderAttributes(
    int Aggression,
    int MentalToughness,
    int Determination,
    int TeamPlayer,
    int Leadership,
    [property: JsonPropertyName("goaltenderStamina")]
    int Stamina,
    int Professionalism,
    int Positioning,
    int Passing,
    int PokeCheck,
    int Blocker,
    int Glove,
    int Rebound,
    int Recovery,
    int Puckhandling,
    int LowShots,
    int Skating,
    int Reflexes
) : PlayerAttributes, IGoaltenderRatings;

public class PlayerAttributesJsonConverter : JsonConverter<PlayerAttributes> {

    public override PlayerAttributes? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        using var jsonDoc = JsonDocument.ParseValue(ref reader);
        var root = jsonDoc.RootElement;
        // fail early if not an object
        if (root.ValueKind != JsonValueKind.Object) {
            throw new JsonException($"Expected JSON object but got {root.ValueKind}");
        }
        
        if (root.TryGetProperty("checking", out _) || root.TryGetProperty("Checking", out _)) {
            return root.Deserialize<SkaterAttributes>(options);
        }

        if (root.TryGetProperty("blocker", out _) || root.TryGetProperty("Blocker", out _)) {
            return root.Deserialize<GoaltenderAttributes>(options);
        }
        
        throw new JsonException("Unable to determine player attributes type");
    }
    public override void Write(Utf8JsonWriter writer, PlayerAttributes value, JsonSerializerOptions options) {
        switch (value) {
            case SkaterAttributes skaterAttributes:
                JsonSerializer.Serialize(writer, skaterAttributes, options);
                break;
            case GoaltenderAttributes goaltenderAttributes:
                JsonSerializer.Serialize(writer, goaltenderAttributes, options);
                break;
            default:
                throw new JsonException("Unknown player attributes type");
        }
    }
}   