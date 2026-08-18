using BinarySerialization;
using Shuttle.BinarySerde.Common.QFormat;

namespace Shuttle.Fhm.Serde.Leagues;

/// <summary>The declarative documented prefix of <c>leagues.dat</c>.</summary>
public sealed class FhmLeaguesFileHeaderData
{
    [FieldOrder(0)]
    public int VersionTag { get; set; }

    [FieldOrder(1)]
    public int RecordCount { get; set; }
}

/// <summary>The structurally known fields preceding the opaque league body.</summary>
public sealed class FhmLeagueRecordData
{
    [FieldOrder(0)]
    public int LeagueId { get; set; }

    [FieldOrder(1)]
    public byte Flag0 { get; set; }

    [FieldOrder(2)]
    public byte Flag1 { get; set; }

    [FieldOrder(3)]
    public byte Flag2 { get; set; }

    [FieldOrder(4)]
    public QString Name { get; set; } = new();

    [FieldOrder(5)]
    public QString ShortName { get; set; } = new();

    [FieldOrder(6)]
    public QString Abbreviation { get; set; } = new();

    [FieldOrder(7)]
    public QString Nickname { get; set; } = new();

    [FieldOrder(8)]
    public ushort TypeParentId { get; set; }

    [FieldOrder(9)]
    public ushort TypeLevelId { get; set; }

    [FieldOrder(10)]
    public double ConfigDouble0 { get; set; }

    [FieldOrder(11)]
    public int EarlyInt0 { get; set; }

    [FieldOrder(12)]
    public int EarlyInt1 { get; set; }

    [FieldOrder(13)]
    [FieldCount(17)]
    public List<double> ConfigDoublesPrimary { get; set; } = [];

    [FieldOrder(14)]
    public int ConfigInt0 { get; set; }

    [FieldOrder(15)]
    public int ConfigInt1 { get; set; }

    [FieldOrder(16)]
    public ushort ConfigUInt160 { get; set; }

    [FieldOrder(17)]
    public int ConfigInt2 { get; set; }

    [FieldOrder(18)]
    public ushort ConfigUInt161 { get; set; }

    [FieldOrder(19)]
    public QDate FoundingDate { get; set; } = new();
}

/// <summary>Serializes the documented <c>leagues.dat</c> prefix without consuming its opaque body.</summary>
public static class FhmLeaguesFileSerializer
{
    /// <summary>Deserializes the container header.</summary>
    public static FhmLeaguesFileHeaderData DeserializeHeader(Stream stream) =>
        QSerializerFactory.DeserializeOne<FhmLeaguesFileHeaderData>(stream);

    /// <summary>Deserializes the known league record prefix.</summary>
    public static FhmLeagueRecordData DeserializeRecord(Stream stream) =>
        QSerializerFactory.DeserializeOne<FhmLeagueRecordData>(stream);

    /// <summary>Serializes the container header.</summary>
    public static void SerializeHeader(Stream stream, FhmLeaguesFileHeaderData value) =>
        QSerializerFactory.Serialize(stream, value);

    /// <summary>Serializes the known league record prefix.</summary>
    public static void SerializeRecord(Stream stream, FhmLeagueRecordData value) =>
        QSerializerFactory.Serialize(stream, value);
}
