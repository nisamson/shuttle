using System.Text;

namespace Shuttle.Analysis.Flows;

/// <summary>
/// Writes a simple CSV table (ordered columns + string rows) using the same conventions as the
/// player export: a UTF-8 BOM, <c>\r\n</c> line endings, and RFC-4180 quoting/escaping.
/// </summary>
/// <remarks>
/// Reusable by any <see cref="IDataAnalysisFlow"/> that produces tabular CSV output. A <c>null</c>
/// cell is written as an empty field.
/// </remarks>
public static class CsvResultWriter {

    private static readonly char[] SpecialChars = ['"', ',', '\n', '\r'];

    /// <summary>
    /// Writes <paramref name="rows"/> to <paramref name="stream"/> under the given <paramref name="columns"/>.
    /// </summary>
    public static async Task WriteAsync(
        Stream stream,
        IReadOnlyList<string> columns,
        IReadOnlyList<IReadOnlyList<string?>> rows,
        CancellationToken cancellationToken
    ) {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(columns);
        ArgumentNullException.ThrowIfNull(rows);

        await using var writer = new StreamWriter(
            stream,
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: true),
            leaveOpen: true) {
            NewLine = "\r\n",
        };

        await writer.WriteLineAsync(string.Join(',', columns.Select(EscapeField)));
        foreach (var row in rows) {
            cancellationToken.ThrowIfCancellationRequested();
            await writer.WriteLineAsync(string.Join(',', row.Select(EscapeField)));
        }

        await writer.FlushAsync(cancellationToken);
    }

    /// <summary>Writes a CSV file at <paramref name="path"/> (overwriting any existing file).</summary>
    public static async Task WriteFileAsync(
        FileInfo path,
        IReadOnlyList<string> columns,
        IReadOnlyList<IReadOnlyList<string?>> rows,
        CancellationToken cancellationToken
    ) {
        ArgumentNullException.ThrowIfNull(path);
        path.Directory?.Create();
        await using var stream = path.Open(FileMode.Create, FileAccess.Write, FileShare.None);
        await WriteAsync(stream, columns, rows, cancellationToken);
    }

    private static string EscapeField(string? field) {
        if (string.IsNullOrEmpty(field)) {
            return string.Empty;
        }

        return field.AsSpan().IndexOfAny(SpecialChars) >= 0
            ? $"\"{field.Replace("\"", "\"\"")}\""
            : field;
    }
}
