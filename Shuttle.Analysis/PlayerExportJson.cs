using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Shuttle.Shl.Api.Models.Common;

namespace Shuttle.Analysis;

/// <summary>
/// JSON serialization options used for analysis exports. Uses <see cref="JsonSerializerDefaults.Web"/>
/// so the per-type <c>[JsonConverter]</c> attributes on the SHL enums (e.g. spaced position/status
/// values) are honored rather than overridden by a global string-enum converter.
/// </summary>
/// <remarks>
/// A number of "mental" attributes are the constant value 15 for every player in the database, so they
/// carry no analytical signal and are dropped from the serialized output via a type-info modifier.
/// Verified empirically across all players in the PlayerInformation table.
/// </remarks>
public static class PlayerExportJson {

    // Skater mental attributes that are always 15 across every player and therefore omitted.
    // (Aggression and Bravery genuinely vary, so they are retained.)
    private static readonly HashSet<string> SkaterConstantMental = new(StringComparer.OrdinalIgnoreCase) {
        nameof(SkaterAttributes.Determination),
        nameof(SkaterAttributes.TeamPlayer),
        nameof(SkaterAttributes.Leadership),
        nameof(SkaterAttributes.Temperament),
        nameof(SkaterAttributes.Professionalism),
    };

    // Goaltender mental attributes that are always 15 across every player and therefore omitted.
    // (MentalToughness and Stamina vary, and Aggression is a constant 8 rather than 15, so they are retained.)
    private static readonly HashSet<string> GoaltenderConstantMental = new(StringComparer.OrdinalIgnoreCase) {
        nameof(GoaltenderAttributes.Determination),
        nameof(GoaltenderAttributes.TeamPlayer),
        nameof(GoaltenderAttributes.Leadership),
        nameof(GoaltenderAttributes.Professionalism),
    };

    public static JsonSerializerOptions CreateOptions(bool indented = true) =>
        new(JsonSerializerDefaults.Web) {
            WriteIndented = indented,
            TypeInfoResolver = new DefaultJsonTypeInfoResolver {
                Modifiers = { DropConstantMentalAttributes },
            },
        };

    private static void DropConstantMentalAttributes(JsonTypeInfo typeInfo) {
        var drop =
            typeInfo.Type == typeof(SkaterAttributes) ? SkaterConstantMental
            : typeInfo.Type == typeof(GoaltenderAttributes) ? GoaltenderConstantMental
            : null;
        if (drop is null) {
            return;
        }

        for (var i = typeInfo.Properties.Count - 1; i >= 0; i--) {
            if (drop.Contains(typeInfo.Properties[i].Name)) {
                typeInfo.Properties.RemoveAt(i);
            }
        }
    }
}
