using BinarySerialization;
using Shuttle.BinarySerde.Common.QFormat;
using Shuttle.BinarySerde.Dsl;
using Shuttle.Fhm.Serde.Wire;
using static Shuttle.BinarySerde.Dsl.BinaryCodecs;

namespace Shuttle.Fhm.Serde.Wire.Leagues;

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
    public static FhmLeaguesFileHeaderData DeserializeHeader(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        using var reader = new BigEndianBinaryReader(stream, bufferSize: 1);
        return FhmLeaguesFileCodec.ReadHeader(reader);
    }

    /// <summary>Deserializes the known league record prefix.</summary>
    public static FhmLeagueRecordData DeserializeRecord(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        using var reader = new BigEndianBinaryReader(stream, bufferSize: 1);
        return FhmLeaguesFileCodec.ReadRecord(reader);
    }

    /// <summary>Serializes the container header.</summary>
    public static void SerializeHeader(Stream stream, FhmLeaguesFileHeaderData value)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(value);
        using var writer = new BigEndianBinaryWriter(stream);
        FhmLeaguesFileCodec.WriteHeader(writer, value);
        writer.Flush();
    }

    /// <summary>Serializes the known league record prefix.</summary>
    public static void SerializeRecord(Stream stream, FhmLeagueRecordData value)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(value);
        using var writer = new BigEndianBinaryWriter(stream);
        FhmLeaguesFileCodec.WriteRecord(writer, value);
        writer.Flush();
    }

    internal static FhmLeaguesFileWireReader CreateReader(Stream stream) => new(stream);
}

internal sealed class FhmLeaguesFileWireReader : IDisposable
{
    private readonly BigEndianBinaryReader reader;
    private bool recordRead;
    private bool disposed;

    internal FhmLeaguesFileWireReader(Stream source)
    {
        ArgumentNullException.ThrowIfNull(source);
        reader = new BigEndianBinaryReader(source);
        Header = FhmLeaguesFileCodec.ReadHeader(reader);
    }

    internal FhmLeaguesFileHeaderData Header { get; }

    internal FhmLeagueRecordData ReadRecord()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (recordRead)
        {
            throw new InvalidOperationException("The leagues.dat record has already been read.");
        }

        recordRead = true;
        return FhmLeaguesFileCodec.ReadRecord(reader);
    }

    internal byte[] ReadRemaining()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (!recordRead)
        {
            throw new InvalidOperationException("The leagues.dat record must be read before its opaque body.");
        }

        return reader.ReadToEnd();
    }

    public void Dispose()
    {
        if (!disposed)
        {
            disposed = true;
            reader.Dispose();
        }
    }
}

internal static class FhmLeaguesFileCodec
{
    private static readonly RecordCodec<FhmLeaguesFileHeaderData> Header = Record<FhmLeaguesFileHeaderData>()
        .Field(static value => value.VersionTag, static (value, field) => value.VersionTag = field, FhmWireCodecs.Int32)
        .Field(static value => value.RecordCount, static (value, field) => value.RecordCount = field, FhmWireCodecs.Int32)
        .Build();

    private static readonly RecordCodec<FhmLeagueRecordData> League = Record<FhmLeagueRecordData>()
        .Field(static value => value.LeagueId, static (value, field) => value.LeagueId = field, FhmWireCodecs.Int32)
        .Field(static value => value.Flag0, static (value, field) => value.Flag0 = field, FhmWireCodecs.Byte)
        .Field(static value => value.Flag1, static (value, field) => value.Flag1 = field, FhmWireCodecs.Byte)
        .Field(static value => value.Flag2, static (value, field) => value.Flag2 = field, FhmWireCodecs.Byte)
        .Field(static value => value.Name, static (value, field) => value.Name = field, FhmWireCodecs.QString)
        .Field(static value => value.ShortName, static (value, field) => value.ShortName = field, FhmWireCodecs.QString)
        .Field(static value => value.Abbreviation, static (value, field) => value.Abbreviation = field, FhmWireCodecs.QString)
        .Field(static value => value.Nickname, static (value, field) => value.Nickname = field, FhmWireCodecs.QString)
        .Field(static value => value.TypeParentId, static (value, field) => value.TypeParentId = field, FhmWireCodecs.UInt16)
        .Field(static value => value.TypeLevelId, static (value, field) => value.TypeLevelId = field, FhmWireCodecs.UInt16)
        .Field(static value => value.ConfigDouble0, static (value, field) => value.ConfigDouble0 = field, FhmWireCodecs.Double)
        .Field(static value => value.EarlyInt0, static (value, field) => value.EarlyInt0 = field, FhmWireCodecs.Int32)
        .Field(static value => value.EarlyInt1, static (value, field) => value.EarlyInt1 = field, FhmWireCodecs.Int32)
        .Field(static value => value.ConfigDoublesPrimary, static (value, field) => value.ConfigDoublesPrimary = field, FixedList(17, FhmWireCodecs.Double))
        .Field(static value => value.ConfigInt0, static (value, field) => value.ConfigInt0 = field, FhmWireCodecs.Int32)
        .Field(static value => value.ConfigInt1, static (value, field) => value.ConfigInt1 = field, FhmWireCodecs.Int32)
        .Field(static value => value.ConfigUInt160, static (value, field) => value.ConfigUInt160 = field, FhmWireCodecs.UInt16)
        .Field(static value => value.ConfigInt2, static (value, field) => value.ConfigInt2 = field, FhmWireCodecs.Int32)
        .Field(static value => value.ConfigUInt161, static (value, field) => value.ConfigUInt161 = field, FhmWireCodecs.UInt16)
        .Field(static value => value.FoundingDate, static (value, field) => value.FoundingDate = field, FhmWireCodecs.QDate)
        .Build();

    internal static FhmLeaguesFileHeaderData ReadHeader(BigEndianBinaryReader reader) => Header.Read(reader);

    internal static FhmLeagueRecordData ReadRecord(BigEndianBinaryReader reader) => League.Read(reader);

    internal static void WriteHeader(BigEndianBinaryWriter writer, FhmLeaguesFileHeaderData value) => Header.Write(writer, value);

    internal static void WriteRecord(BigEndianBinaryWriter writer, FhmLeagueRecordData value) => League.Write(writer, value);
}
