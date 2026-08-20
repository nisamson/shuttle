using BinarySerialization;
using Shuttle.BinarySerde.Common.QFormat;

namespace Shuttle.Fhm.Serde.Wire.Tactics;

/// <summary>The documented header preceding the opaque <c>tactics.dat</c> records.</summary>
public sealed class FhmTacticsFileData
{
    [FieldOrder(0)]
    public int Version { get; set; }

    [FieldOrder(1)]
    public int TacticCount { get; set; }
}

/// <summary>Serializes the documented <c>tactics.dat</c> header without consuming opaque records.</summary>
public static class FhmTacticsFileSerializer
{
    /// <summary>Deserializes the file header.</summary>
    public static FhmTacticsFileData DeserializeHeader(Stream stream) =>
        QSerializerFactory.DeserializeOne<FhmTacticsFileData>(stream);

    /// <summary>Serializes the file header.</summary>
    public static void SerializeHeader(Stream stream, FhmTacticsFileData value) =>
        QSerializerFactory.Serialize(stream, value);
}
