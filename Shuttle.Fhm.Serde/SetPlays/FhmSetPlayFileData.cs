using BinarySerialization;
using Shuttle.BinarySerde.Common.QFormat;

namespace Shuttle.Fhm.Serde.SetPlays;

/// <summary>The fixed header common to the <c>set_play_*.dat</c> formation catalogues.</summary>
public sealed class FhmSetPlayHeaderData
{
    [FieldOrder(0)]
    public int Version { get; set; }

    [FieldOrder(1)]
    public int FormationCount { get; set; }
}

/// <summary>One length-prefixed opaque set-play payload.</summary>
public sealed class FhmSetPlayBlockData
{
    [FieldOrder(0)]
    public int ByteLength { get; set; }

    [FieldOrder(1)]
    [FieldLength(nameof(ByteLength))]
    public byte[] Data { get; set; } = [];
}

/// <summary>Serializes the header and opaque records in <c>set_play_*.dat</c> streams.</summary>
public static class FhmSetPlayFileSerializer
{
    /// <summary>Deserializes a set-play header without consuming trailing records.</summary>
    public static FhmSetPlayHeaderData DeserializeHeader(Stream stream) =>
        QSerializerFactory.DeserializeOne<FhmSetPlayHeaderData>(stream);

    /// <summary>Deserializes a single length-prefixed set-play record.</summary>
    public static FhmSetPlayBlockData DeserializeBlock(Stream stream) =>
        QSerializerFactory.DeserializeOne<FhmSetPlayBlockData>(stream);

    /// <summary>Serializes a set-play header.</summary>
    public static void SerializeHeader(Stream stream, FhmSetPlayHeaderData value) =>
        QSerializerFactory.Serialize(stream, value);

    /// <summary>Serializes a single length-prefixed set-play record.</summary>
    public static void SerializeBlock(Stream stream, FhmSetPlayBlockData value) =>
        QSerializerFactory.Serialize(stream, value);
}
