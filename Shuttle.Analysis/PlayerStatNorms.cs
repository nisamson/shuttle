using System.Text.Json;
using System.Text.Json.Nodes;

namespace Shuttle.Analysis;

/// <summary>
/// Normalizes a player's stat vector in place on a serialized player record.
/// </summary>
/// <remarks>
/// The stat vector for a skater is its skater attributes; for a goaltender, its goaltender attributes
/// (each player only carries one set). The constant "mental" attributes dropped from the export (see
/// <see cref="PlayerExportJson"/>) are absent from the vector, so normalization operates only over the
/// exported, meaningful attributes. When a norm is applied the raw integer attribute values are
/// replaced in place, keeping the exact original property names: the L1 form divides each component by
/// the vector's L1 norm (so nonnegative components sum to 1), and the L2 form divides by the L2 norm
/// (producing a unit vector).
/// </remarks>
public static class PlayerStatNorms {

    private static readonly string[] AttributeSets = ["skaterAttributes", "goaltenderAttributes"];

    /// <summary>
    /// Replaces the numeric attribute values of each attribute set present on <paramref name="record"/>
    /// with their <paramref name="norm"/>-normalized form. A <see cref="StatNorm.None"/> norm is a no-op.
    /// </summary>
    public static void Apply(JsonObject record, StatNorm norm) {
        ArgumentNullException.ThrowIfNull(record);

        if (norm == StatNorm.None) {
            return;
        }

        foreach (var key in AttributeSets) {
            if (record[key] is JsonObject attributes) {
                Normalize(attributes, norm);
            }
        }
    }

    private static void Normalize(JsonObject attributes, StatNorm norm) {
        // Snapshot components first: a JsonObject cannot be mutated while enumerating it.
        var components = new List<(string Key, double Value)>();
        foreach (var (key, value) in attributes) {
            if (value is JsonValue jsonValue && jsonValue.GetValueKind() == JsonValueKind.Number) {
                components.Add((key, jsonValue.GetValue<double>()));
            }
        }

        var denominator = norm == StatNorm.L1
            ? components.Sum(c => Math.Abs(c.Value))
            : Math.Sqrt(components.Sum(c => c.Value * c.Value));

        foreach (var (key, value) in components) {
            attributes[key] = denominator == 0 ? 0d : value / denominator;
        }
    }
}
