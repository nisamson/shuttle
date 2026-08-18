using BinarySerialization;
using Shuttle.BinarySerde.Common.QFormat;

namespace Shuttle.Fhm.Serde.Generic;

public sealed class FhmCatalogueHeaderData
{
    [FieldOrder(0)]
    public int Version { get; set; }
}

public sealed class FhmCountedCatalogueHeaderData
{
    [FieldOrder(0)]
    public int Version { get; set; }

    [FieldOrder(1)]
    public int Count { get; set; }
}

public sealed class FhmLengthPrefixedBlockData
{
    [FieldOrder(0)]
    public int ByteLength { get; set; }

    [FieldOrder(1)]
    [FieldLength(nameof(ByteLength))]
    public byte[] Data { get; set; } = [];
}

public static class FhmLengthPrefixedCatalogueSerializer
{
    public static FhmCatalogueHeaderData DeserializeHeader(Stream stream) =>
        QSerializerFactory.DeserializeOne<FhmCatalogueHeaderData>(stream);

    public static FhmCountedCatalogueHeaderData DeserializeCountedHeader(Stream stream) =>
        QSerializerFactory.DeserializeOne<FhmCountedCatalogueHeaderData>(stream);

    public static FhmLengthPrefixedBlockData DeserializeBlock(Stream stream) =>
        QSerializerFactory.DeserializeOne<FhmLengthPrefixedBlockData>(stream);

    public static void SerializeHeader(Stream stream, FhmCatalogueHeaderData value) =>
        QSerializerFactory.Serialize(stream, value);

    public static void SerializeCountedHeader(Stream stream, FhmCountedCatalogueHeaderData value) =>
        QSerializerFactory.Serialize(stream, value);

    public static void SerializeBlock(Stream stream, FhmLengthPrefixedBlockData value) =>
        QSerializerFactory.Serialize(stream, value);
}
