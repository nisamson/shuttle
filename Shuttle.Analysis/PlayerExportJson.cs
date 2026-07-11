using System.Text.Json;

namespace Shuttle.Analysis;

/// <summary>
/// JSON serialization options used for analysis exports. Uses <see cref="JsonSerializerDefaults.Web"/>
/// so the per-type <c>[JsonConverter]</c> attributes on the SHL enums (e.g. spaced position/status
/// values) are honored rather than overridden by a global string-enum converter.
/// </summary>
public static class PlayerExportJson {
    public static JsonSerializerOptions CreateOptions(bool indented = true) =>
        new(JsonSerializerDefaults.Web) {
            WriteIndented = indented,
        };
}
