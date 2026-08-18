using BinarySerialization;
using Shuttle.BinarySerde.Common.QFormat;

namespace Shuttle.Fhm.Serde.Info;

public sealed class FhmInfoFileData
{
    [FieldOrder(0)]
    public QString Description { get; set; } = new();

    [FieldOrder(1)]
    public QString NameId { get; set; } = new();
}

public static class FhmInfoFileSerializer
{
    public static FhmInfoFileData Deserialize(Stream stream) =>
        QSerializerFactory.Deserialize<FhmInfoFileData>(stream, "info.dat");

    public static void Serialize(Stream stream, FhmInfoFileData value) =>
        QSerializerFactory.Serialize(stream, value);
}
