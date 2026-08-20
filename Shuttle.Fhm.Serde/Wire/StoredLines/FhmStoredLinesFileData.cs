using BinarySerialization;
using Shuttle.BinarySerde.Common.QFormat;

namespace Shuttle.Fhm.Serde.Wire.StoredLines;

/// <summary>The declarative <c>stored_lines.dat</c> wire contract.</summary>
public sealed class FhmStoredLinesFileData
{
    [FieldOrder(0)]
    public int StoredLineCount { get; set; }

    [FieldOrder(1)]
    [FieldCount(nameof(StoredLineCount))]
    public List<FhmStoredLineData> StoredLines { get; set; } = [];
}

/// <summary>One fixed-order saved lineup preset.</summary>
public sealed class FhmStoredLineData
{
    /// <summary>The fixed number of player and unit-lock groups in each preset.</summary>
    public const int GroupCount = 13;

    [FieldOrder(0)]
    public QString Name { get; set; } = new();

    [FieldOrder(1)]
    [FieldCount(GroupCount)]
    public List<QList<int>> PlayerGroups { get; set; } = [];

    [FieldOrder(2)]
    [FieldCount(GroupCount)]
    public List<QList<byte>> UnitLocks { get; set; } = [];
}

/// <summary>Serializes complete <c>stored_lines.dat</c> streams.</summary>
public static class FhmStoredLinesFileSerializer
{
    /// <summary>Deserializes a complete <c>stored_lines.dat</c> stream.</summary>
    public static FhmStoredLinesFileData Deserialize(Stream stream) =>
        QSerializerFactory.Deserialize<FhmStoredLinesFileData>(stream, "stored_lines.dat");

    /// <summary>Serializes a <c>stored_lines.dat</c> stream.</summary>
    public static void Serialize(Stream stream, FhmStoredLinesFileData value) =>
        QSerializerFactory.Serialize(stream, value);
}
