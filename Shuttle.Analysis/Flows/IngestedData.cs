using System.Collections.ObjectModel;

namespace Shuttle.Analysis.Flows;

/// <summary>
/// An immutable, schema-flexible tabular projection of an ingested analysis data file.
/// </summary>
/// <remarks>
/// The export CSV produced by <c>download-players</c> flattens nested attribute objects into dotted
/// column names (e.g. <c>skaterAttributes.checking</c>) and takes the union of the skater and
/// goaltender columns, so any given row only populates a subset of the columns. To stay decoupled
/// from the export schema, <see cref="IngestedData"/> keeps the column order and exposes each row as a
/// case-sensitive dictionary of column name to cell value, where an empty cell is represented as
/// <c>null</c>. Individual <see cref="IDataAnalysisFlow"/> implementations project the columns they
/// care about into their own ML.NET typed schema / <c>IDataView</c>.
/// </remarks>
public sealed class IngestedData {

    /// <summary>
    /// Creates a new <see cref="IngestedData"/> from the given ordered columns and rows.
    /// </summary>
    /// <param name="columns">The ordered column names, as they appeared in the source header.</param>
    /// <param name="rows">
    /// The data rows, each mapping a column name to its cell value (<c>null</c> for empty cells).
    /// </param>
    public IngestedData(IReadOnlyList<string> columns, IReadOnlyList<IReadOnlyDictionary<string, string?>> rows) {
        ArgumentNullException.ThrowIfNull(columns);
        ArgumentNullException.ThrowIfNull(rows);
        Columns = new ReadOnlyCollection<string>([.. columns]);
        Rows = new ReadOnlyCollection<IReadOnlyDictionary<string, string?>>([.. rows]);
    }

    /// <summary>The ordered column names from the source header.</summary>
    public IReadOnlyList<string> Columns { get; }

    /// <summary>
    /// The data rows. Each row maps a column name to its cell value; an empty cell is <c>null</c>, and
    /// a column absent from a row (e.g. a goaltender column on a skater row) may be missing entirely.
    /// </summary>
    public IReadOnlyList<IReadOnlyDictionary<string, string?>> Rows { get; }

    /// <summary>The number of data rows (excluding the header).</summary>
    public int RowCount => Rows.Count;

    /// <summary>Returns <c>true</c> if a column with the given name is present in the header.</summary>
    public bool HasColumn(string name) => Columns.Contains(name);
}
