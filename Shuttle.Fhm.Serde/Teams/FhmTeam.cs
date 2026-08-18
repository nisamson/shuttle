using BinarySerialization;
using Shuttle.BinarySerde.Common.QFormat;

namespace Shuttle.Fhm.Serde.Teams;

/// <summary>The declarative <c>team_tactics.dat</c> wire contract.</summary>
public sealed class FhmTeamTacticsFileData
{
    [FieldOrder(0)]
    public int VersionTag { get; set; }

    [FieldOrder(1)]
    public int RecordCount { get; set; }

    [FieldOrder(2)]
    [FieldCount(nameof(RecordCount))]
    public List<FhmTacticSystemData> Records { get; set; } = [];
}

/// <summary>One selectable tactic system for a tactical zone.</summary>
public sealed class FhmTacticSystemData
{
    [FieldOrder(0)]
    public int GlobalId { get; set; }

    [FieldOrder(1)]
    public int ZoneGroupRaw { get; set; }

    [FieldOrder(2)]
    public QString Name { get; set; } = new();

    [FieldOrder(3)]
    public int RatingA { get; set; }

    [FieldOrder(4)]
    public int RatingB { get; set; }
}

/// <summary>Serializes complete <c>team_tactics.dat</c> streams.</summary>
public static class FhmTeamTacticsFileSerializer
{
    /// <summary>Deserializes a complete <c>team_tactics.dat</c> stream.</summary>
    public static FhmTeamTacticsFileData Deserialize(Stream stream) =>
        QSerializerFactory.Deserialize<FhmTeamTacticsFileData>(stream, "team_tactics.dat");

    /// <summary>Serializes a <c>team_tactics.dat</c> stream.</summary>
    public static void Serialize(Stream stream, FhmTeamTacticsFileData value) =>
        QSerializerFactory.Serialize(stream, value);
}
