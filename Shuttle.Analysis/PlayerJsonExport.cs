using System.Text.Json;
using System.Text.Json.Nodes;

namespace Shuttle.Analysis;

/// <summary>
/// Writes <see cref="PlayerExportRecord"/> data as a JSON array, optionally replacing each player's
/// stat attributes with their normalized form (see <see cref="PlayerStatNorms"/>).
/// </summary>
public static class PlayerJsonExport {

    public static async Task WriteAsync(
        Stream stream,
        IReadOnlyList<PlayerExportRecord> records,
        bool indented,
        StatNorm norm,
        CancellationToken cancellationToken
    ) {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(records);

        var options = PlayerExportJson.CreateOptions(indented);

        // Without normalization the records can be streamed directly (raw integer attributes).
        if (norm == StatNorm.None) {
            await JsonSerializer.SerializeAsync(stream, records, options, cancellationToken);
            return;
        }

        var array = new JsonArray();
        foreach (var record in records) {
            cancellationToken.ThrowIfCancellationRequested();
            var node = JsonSerializer.SerializeToNode(record, options)!.AsObject();
            PlayerStatNorms.Apply(node, norm);
            array.Add(node);
        }

        await JsonSerializer.SerializeAsync(stream, array, options, cancellationToken);
    }
}
