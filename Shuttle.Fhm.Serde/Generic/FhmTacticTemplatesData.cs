using BinarySerialization;
using Shuttle.BinarySerde.Common.QFormat;

namespace Shuttle.Fhm.Serde.Generic;

public sealed class FhmTacticTemplatesData
{
    [FieldOrder(0)]
    public int Version { get; set; }

    [FieldOrder(1)]
    public int Count { get; set; }

    [FieldOrder(2)]
    [FieldCount(nameof(Count))]
    public List<FhmTacticTemplateData> Templates { get; set; } = [];
}

public sealed class FhmTacticTemplateData
{
    [FieldOrder(0)]
    public QString InternalKey { get; set; } = new();

    [FieldOrder(1)]
    public int TemplateIndex { get; set; }

    [FieldOrder(2)]
    public QString DisplayName { get; set; } = new();

    [FieldOrder(3)]
    [FieldLength(4856)]
    public byte[] SettingsBlob { get; set; } = [];
}

public static class FhmTacticTemplatesSerializer
{
    public static FhmTacticTemplatesData Deserialize(Stream stream) =>
        QSerializerFactory.Deserialize<FhmTacticTemplatesData>(stream, "tactic_templates.dat");

    public static void Serialize(Stream stream, FhmTacticTemplatesData value) =>
        QSerializerFactory.Serialize(stream, value);
}
