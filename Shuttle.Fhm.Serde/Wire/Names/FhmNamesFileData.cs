using BinarySerialization;
using Shuttle.BinarySerde.Common.QFormat;
using Shuttle.BinarySerde.Dsl;
using Shuttle.Fhm.Serde.Wire;
using static Shuttle.BinarySerde.Dsl.BinaryCodecs;

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
    public static FhmNamesFileData Deserialize(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        using var reader = new BigEndianBinaryReader(stream);
        var result = FhmNamesFileCodec.Read(reader);
        reader.EnsureEndOfStream("names.dat");
        return result;
    }

    /// <summary>Serializes a <c>names.dat</c> stream.</summary>
    public static void Serialize(Stream stream, FhmNamesFileData value)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(value);
        using var writer = new BigEndianBinaryWriter(stream);
        FhmNamesFileCodec.Write(writer, value);
        writer.Flush();
    }
}

internal static class FhmNamesFileCodec
{
    private static readonly RecordCodec<FhmNameData> Name = Record<FhmNameData>()
        .Field(static value => value.Text, static (value, field) => value.Text = field, FhmWireCodecs.QString)
        .Field(static value => value.NameId, static (value, field) => value.NameId = field, FhmWireCodecs.Int32)
        .Field(static value => value.GroupId, static (value, field) => value.GroupId = field, FhmWireCodecs.Int32)
        .Field(static value => value.CategoryWeight, static (value, field) => value.CategoryWeight = field, FhmWireCodecs.UInt16)
        .Field(static value => value.FlagA, static (value, field) => value.FlagA = field, FhmWireCodecs.Byte)
        .Field(static value => value.FlagB, static (value, field) => value.FlagB = field, FhmWireCodecs.Byte)
        .Field(static value => value.FlagC, static (value, field) => value.FlagC = field, FhmWireCodecs.Byte)
        .Build();

    private static readonly ValueCodec<QList<int>> NameIdList =
        FhmWireCodecs.QList(FhmWireCodecs.Int32, "names.dat name ids");

    internal static FhmNamesFileData Read(BigEndianBinaryReader reader)
    {
        var result = new FhmNamesFileData
        {
            ReservedZero = reader.ReadInt32(),
            MasterNameCount = reader.ReadInt32(),
        };
        result.MasterNames = FhmWireCodecs.ReadCountedList(
            reader,
            result.MasterNameCount,
            Object(Name),
            "names.dat master names");
        result.FirstNameLists = ReadNationLists(reader);
        result.SurnameLists = ReadNationLists(reader);
        result.ScalarArrayA = ReadScalars(reader);
        result.ScalarArrayB = ReadScalars(reader);
        return result;
    }

    internal static void Write(BigEndianBinaryWriter writer, FhmNamesFileData value)
    {
        writer.WriteInt32(value.ReservedZero);
        writer.WriteInt32(value.MasterNameCount);
        FhmWireCodecs.WriteCountedList(writer, value.MasterNames, Object(Name));
        WriteNationLists(writer, value.FirstNameLists);
        WriteNationLists(writer, value.SurnameLists);
        WriteScalars(writer, value.ScalarArrayA);
        WriteScalars(writer, value.ScalarArrayB);
    }

    private static List<QList<int>> ReadNationLists(BigEndianBinaryReader reader)
    {
        var result = new List<QList<int>>(FhmNamesFileData.NationCount);
        for (var index = 0; index < FhmNamesFileData.NationCount; index++)
        {
            result.Add(NameIdList.Read(reader));
        }

        return result;
    }

    private static void WriteNationLists(BigEndianBinaryWriter writer, IEnumerable<QList<int>> lists)
    {
        foreach (var list in lists)
        {
            NameIdList.Write(writer, list);
        }
    }

    private static List<int> ReadScalars(BigEndianBinaryReader reader)
    {
        var result = new List<int>(FhmNamesFileData.NationCount);
        for (var index = 0; index < FhmNamesFileData.NationCount; index++)
        {
            result.Add(reader.ReadInt32());
        }

        return result;
    }

    private static void WriteScalars(BigEndianBinaryWriter writer, IEnumerable<int> values)
    {
        foreach (var value in values)
        {
            writer.WriteInt32(value);
        }
    }
}
