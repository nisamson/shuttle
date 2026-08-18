using BinarySerialization;
using Shuttle.BinarySerde.Common.QFormat;

namespace Shuttle.Fhm.Serde.ZoneEvents;

/// <summary>The documented header of <c>zone_event_mod.dat</c>.</summary>
public sealed class FhmZoneEventModifiersFileData
{
    [FieldOrder(0)]
    public int Version { get; set; }

    [FieldOrder(1)]
    public int ZoneCount { get; set; }
}

/// <summary>One length-prefixed opaque modifier grid.</summary>
public sealed class FhmZoneEventModifierGridData
{
    [FieldOrder(0)]
    public int ByteLength { get; set; }

    [FieldOrder(1)]
    [FieldLength(nameof(ByteLength))]
    public byte[] Data { get; set; } = [];
}

/// <summary>Serializes the header and modifier grids in <c>zone_event_mod.dat</c>.</summary>
public static class FhmZoneEventModifiersFileSerializer
{
    /// <summary>Deserializes the file header.</summary>
    public static FhmZoneEventModifiersFileData DeserializeHeader(Stream stream) =>
        QSerializerFactory.DeserializeOne<FhmZoneEventModifiersFileData>(stream);

    /// <summary>Deserializes one modifier grid.</summary>
    public static FhmZoneEventModifierGridData DeserializeGrid(Stream stream) =>
        QSerializerFactory.DeserializeOne<FhmZoneEventModifierGridData>(stream);

    /// <summary>Serializes the file header.</summary>
    public static void SerializeHeader(Stream stream, FhmZoneEventModifiersFileData value) =>
        QSerializerFactory.Serialize(stream, value);

    /// <summary>Serializes one modifier grid.</summary>
    public static void SerializeGrid(Stream stream, FhmZoneEventModifierGridData value) =>
        QSerializerFactory.Serialize(stream, value);
}
