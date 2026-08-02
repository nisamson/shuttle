using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;

namespace Shuttle.Analysis.Flows;

/// <summary>
/// Reads a CSV data file produced by the <c>download-players</c> export into <see cref="IngestedData"/>.
/// </summary>
/// <remarks>
/// This is the ingestion step of an analysis flow. It handles the specifics of the export CSV: the
/// leading UTF-8 BOM (detected automatically by <see cref="StreamReader"/>), RFC-4180 quoting, and the
/// union header that mixes skater and goaltender columns. Empty cells are surfaced as <c>null</c> so
/// downstream flows can distinguish "missing" from a genuine empty string.
/// </remarks>
public static class CsvDataIngestor {

    /// <summary>
    /// Reads <paramref name="input"/> into an <see cref="IngestedData"/> table.
    /// </summary>
    /// <param name="input">The CSV file to ingest.</param>
    /// <param name="cancellationToken">A token to cancel the read.</param>
    /// <returns>The ingested table.</returns>
    /// <exception cref="FileNotFoundException">The file does not exist.</exception>
    /// <exception cref="InvalidDataException">The file is empty or has no header row.</exception>
    public static async Task<IngestedData> IngestAsync(FileInfo input, CancellationToken cancellationToken) {
        ArgumentNullException.ThrowIfNull(input);
        input.Refresh();

        if (!input.Exists) {
            throw new FileNotFoundException($"Input data file not found: {input.FullName}", input.FullName);
        }

        if (input.Length == 0) {
            throw new InvalidDataException($"Input data file is empty: {input.FullName}");
        }

        var config = new CsvConfiguration(CultureInfo.InvariantCulture) {
            DetectColumnCountChanges = false,
            MissingFieldFound = null,
            BadDataFound = null,
        };

        await using var stream = input.Open(FileMode.Open, FileAccess.Read, FileShare.Read);
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        using var csv = new CsvReader(reader, config);

        if (!await csv.ReadAsync() || !csv.ReadHeader()) {
            throw new InvalidDataException($"Input data file has no header row: {input.FullName}");
        }

        var columns = csv.HeaderRecord ?? [];
        if (columns.Length == 0) {
            throw new InvalidDataException($"Input data file has an empty header row: {input.FullName}");
        }

        var rows = new List<IReadOnlyDictionary<string, string?>>();
        while (await csv.ReadAsync()) {
            cancellationToken.ThrowIfCancellationRequested();

            var row = new Dictionary<string, string?>(columns.Length, StringComparer.Ordinal);
            for (var i = 0; i < columns.Length; i++) {
                var value = csv.TryGetField<string>(i, out var field) ? field : null;
                row[columns[i]] = string.IsNullOrEmpty(value) ? null : value;
            }

            rows.Add(row);
        }

        return new IngestedData(columns, rows);
    }
}
