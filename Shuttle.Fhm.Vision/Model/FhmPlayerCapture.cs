using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Shuttle.Fhm.Vision.Model;

/// <summary>
/// A single parsed FHM player-info screen: identity fields plus the attribute-rating and
/// role-rating vectors read from the screen. Instances are immutable; the <see cref="ContentHash"/>
/// is a stable fingerprint of the normalized data used to de-duplicate captures of the same player
/// state.
/// </summary>
public sealed record FhmPlayerCapture {
    public required DateTimeOffset CapturedAtUtc { get; init; }
    public required string Name { get; init; }
    public int? JerseyNumber { get; init; }
    public string? Position { get; init; }
    public string? Handedness { get; init; }

    /// <summary>Raw attribute ratings keyed by field key (e.g. <c>skating</c> =&gt; 15).</summary>
    public required IReadOnlyDictionary<string, int> Attributes { get; init; }

    /// <summary>Derived per-role ratings keyed by field key (e.g. <c>playmaker</c> =&gt; 12).</summary>
    public required IReadOnlyDictionary<string, int> RoleRatings { get; init; }

    /// <summary>
    /// Decimal-valued fields keyed by field key (e.g. a fractional rating, or numeric identity
    /// values such as <c>weight</c> =&gt; 243 or <c>salary</c> =&gt; 775000). Populated from
    /// <see cref="Layout.FieldKind.Float"/> fields and from non-attribute/role integer fields.
    /// </summary>
    public IReadOnlyDictionary<string, double> Numbers { get; init; } =
        new Dictionary<string, double>();

    /// <summary>Any additional free-text fields captured (birthplace, etc.).</summary>
    public IReadOnlyDictionary<string, string> TextFields { get; init; } =
        new Dictionary<string, string>();

    /// <summary>
    /// A SHA-256 fingerprint over the normalized numeric vectors only (attributes, role ratings and
    /// numeric fields such as weight). Identity text (name, jersey number, position, handedness) is
    /// deliberately excluded: two captures with the same numbers represent the same player state and
    /// are treated as duplicates.
    /// </summary>
    public string ContentHash => ComputeContentHash();

    private string ComputeContentHash() {
        var builder = new StringBuilder();
        AppendVector(builder, "attr", Attributes);
        AppendVector(builder, "role", RoleRatings);
        AppendNumbers(builder, "num", Numbers);

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString()));
        return Convert.ToHexStringLower(bytes);
    }

    private static void AppendVector(StringBuilder builder, string prefix, IReadOnlyDictionary<string, int> values) {
        foreach (var pair in values.OrderBy(p => p.Key, StringComparer.Ordinal)) {
            builder.Append(prefix).Append('.').Append(pair.Key).Append('=').Append(pair.Value).Append('\n');
        }
    }

    private static void AppendNumbers(StringBuilder builder, string prefix, IReadOnlyDictionary<string, double> values) {
        foreach (var pair in values.OrderBy(p => p.Key, StringComparer.Ordinal)) {
            builder.Append(prefix).Append('.').Append(pair.Key).Append('=')
                .Append(pair.Value.ToString("R", CultureInfo.InvariantCulture)).Append('\n');
        }
    }
}
