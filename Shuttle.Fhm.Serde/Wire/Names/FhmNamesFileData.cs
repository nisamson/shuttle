using BinarySerialization;
using Shuttle.BinarySerde.Common.QFormat;

namespace Shuttle.Fhm.Serde.Wire.Names;

/// <summary>The declarative <c>names.dat</c> wire contract.</summary>
public sealed class FhmNamesFileData
{
    /// <summary>The number of nation-indexed entries in each names table.</summary>
    public const int NationCount = 102;

    [FieldOrder(0)]
    public int ReservedZero { get; set; }

    [FieldOrder(1)]
    public int MasterNameCount { get; set; }

    [FieldOrder(2)]
    [FieldCount(nameof(MasterNameCount))]
    public List<FhmNameData> MasterNames { get; set; } = [];

    [FieldOrder(3)]
    [FieldCount(NationCount)]
    public List<QList<int>> FirstNameLists { get; set; } = [];

    [FieldOrder(4)]
    [FieldCount(NationCount)]
    public List<QList<int>> SurnameLists { get; set; } = [];

    [FieldOrder(5)]
    [FieldCount(NationCount)]
    public List<int> ScalarArrayA { get; set; } = [];

    [FieldOrder(6)]
    [FieldCount(NationCount)]
    public List<int> ScalarArrayB { get; set; } = [];
}

/// <summary>One master-name wire record.</summary>
public sealed class FhmNameData
{
    [FieldOrder(0)]
    public QString Text { get; set; } = new();

    [FieldOrder(1)]
    public int NameId { get; set; }

    [FieldOrder(2)]
    public int GroupId { get; set; }

    [FieldOrder(3)]
    public ushort CategoryWeight { get; set; }

    [FieldOrder(4)]
    public byte FlagA { get; set; }

    [FieldOrder(5)]
    public byte FlagB { get; set; }

    [FieldOrder(6)]
    public byte FlagC { get; set; }
}

/// <summary>Serializes complete <c>names.dat</c> streams.</summary>
public static class FhmNamesFileSerializer
{
    /// <summary>Deserializes a complete <c>names.dat</c> stream.</summary>
    public static FhmNamesFileData Deserialize(Stream stream) =>
        QSerializerFactory.Deserialize<FhmNamesFileData>(stream, "names.dat");

    /// <summary>Serializes a <c>names.dat</c> stream.</summary>
    public static void Serialize(Stream stream, FhmNamesFileData value) =>
        QSerializerFactory.Serialize(stream, value);
}
