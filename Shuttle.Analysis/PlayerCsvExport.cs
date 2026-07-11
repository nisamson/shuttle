using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Shuttle.Analysis;

/// <summary>
/// Writes <see cref="PlayerExportRecord"/> data as CSV.
/// </summary>
/// <remarks>
/// Rows are flattened from the same JSON projection used for the JSON export (via
/// <see cref="PlayerExportJson"/>), so column names, enum formatting, and the omission of the
/// constant "mental" attributes stay consistent between the two formats. Nested attribute objects
/// are flattened into dotted column names (e.g. <c>skaterAttributes.checking</c>), and the column
/// set is the union across all rows because skaters and goaltenders expose different attributes.
/// Null values are written as empty cells.
/// </remarks>
public static class PlayerCsvExport {

    private static readonly char[] SpecialChars = ['"', ',', '\n', '\r'];

    public static async Task WriteAsync(
        Stream stream,
        IReadOnlyList<PlayerExportRecord> records,
        StatNorm norm,
        CancellationToken cancellationToken
    ) {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(records);

        // Reuse the JSON projection (enum converters + dropped mental attributes) as the source of
        // truth, and omit nulls so a skater row carries no goaltender columns and vice versa.
        var options = new JsonSerializerOptions(PlayerExportJson.CreateOptions(indented: false)) {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

        var columns = new List<string>();
        var columnSet = new HashSet<string>(StringComparer.Ordinal);
        var rows = new List<Dictionary<string, string?>>(records.Count);

        foreach (var record in records) {
            cancellationToken.ThrowIfCancellationRequested();
            var node = JsonSerializer.SerializeToNode(record, options)!.AsObject();
            PlayerStatNorms.Apply(node, norm);
            var row = new Dictionary<string, string?>(StringComparer.Ordinal);
            Flatten(node, prefix: null, row, columns, columnSet);
            rows.Add(row);
        }

        await using var writer = new StreamWriter(
            stream,
            // Emit a UTF-8 BOM so consumers (e.g. Excel) unambiguously detect UTF-8 and render
            // non-ASCII glyphs (accented names, CJK, etc.) correctly instead of mojibake.
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: true),
            leaveOpen: true) {
            NewLine = "\r\n",
        };

        await writer.WriteLineAsync(string.Join(',', columns.Select(EscapeField)));
        foreach (var row in rows) {
            cancellationToken.ThrowIfCancellationRequested();
            var line = string.Join(
                ',',
                columns.Select(c => EscapeField(row.TryGetValue(c, out var value) ? value : null)));
            await writer.WriteLineAsync(line);
        }

        await writer.FlushAsync(cancellationToken);
    }

    private static void Flatten(
        JsonObject obj,
        string? prefix,
        Dictionary<string, string?> row,
        List<string> columns,
        HashSet<string> columnSet
    ) {
        foreach (var (key, value) in obj) {
            var name = prefix is null ? key : $"{prefix}.{key}";
            switch (value) {
                case JsonObject nested:
                    Flatten(nested, name, row, columns, columnSet);
                    break;
                case JsonArray array:
                    AddColumn(name, columns, columnSet);
                    row[name] = array.ToJsonString();
                    break;
                case JsonValue scalar:
                    AddColumn(name, columns, columnSet);
                    row[name] = FormatScalar(scalar);
                    break;
                case null:
                    AddColumn(name, columns, columnSet);
                    row[name] = null;
                    break;
            }
        }
    }

    private static void AddColumn(string name, List<string> columns, HashSet<string> columnSet) {
        if (columnSet.Add(name)) {
            columns.Add(name);
        }
    }

    private static string? FormatScalar(JsonValue value) =>
        value.GetValueKind() switch {
            JsonValueKind.String => value.GetValue<string>(),
            JsonValueKind.Null => null,
            // Numbers and booleans serialize to their raw JSON token (e.g. 72, true).
            _ => value.ToJsonString(),
        };

    private static string EscapeField(string? field) {
        if (string.IsNullOrEmpty(field)) {
            return string.Empty;
        }

        return field.AsSpan().IndexOfAny(SpecialChars) >= 0
            ? $"\"{field.Replace("\"", "\"\"")}\""
            : field;
    }
}
