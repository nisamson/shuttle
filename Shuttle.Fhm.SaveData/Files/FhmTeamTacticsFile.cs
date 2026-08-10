using Shuttle.Fhm.SaveData.Binary;
using Shuttle.Fhm.SaveData.Model;

namespace Shuttle.Fhm.SaveData.Files;

/// <summary>The selectable per-zone tactic-system catalogue in <c>team_tactics.dat</c>.</summary>
public sealed class FhmTeamTacticsFile : IFhmSaveFile
{
    /// <inheritdoc />
    public string RelativePath => "team_tactics.dat";

    /// <summary>Gets or sets the catalogue format version.</summary>
    public int VersionTag { get; set; }

    /// <summary>Gets selectable tactic systems in serialized order.</summary>
    public IList<FhmTacticSystem> Records { get; } = [];

    internal static FhmTeamTacticsFile Read(FhmBinaryReader reader)
    {
        var result = new FhmTeamTacticsFile { VersionTag = reader.ReadInt32() };
        var count = reader.ReadCount("team tactic systems");
        for (var index = 0; index < count; index++)
        {
            result.Records.Add(new FhmTacticSystem
            {
                GlobalId = reader.ReadInt32(),
                ZoneGroupRaw = reader.ReadInt32(),
                Name = reader.ReadQString(),
                RatingA = reader.ReadInt32(),
                RatingB = reader.ReadInt32(),
            });
        }

        reader.EnsureEof("team_tactics.dat");
        return result;
    }

    /// <inheritdoc />
    public void WriteTo(FhmBinaryWriter writer)
    {
        writer.WriteInt32(VersionTag);
        writer.WriteCount(Records.Count, "team tactic systems");
        foreach (var record in Records)
        {
            writer.WriteInt32(record.GlobalId);
            writer.WriteInt32(record.ZoneGroupRaw);
            writer.WriteQString(record.Name);
            writer.WriteInt32(record.RatingA);
            writer.WriteInt32(record.RatingB);
        }
    }
}

/// <summary>One selectable tactic system for a tactical zone.</summary>
public sealed class FhmTacticSystem
{
    /// <summary>Gets or sets the globally unique system id selected by teams.</summary>
    public int GlobalId { get; set; }

    /// <summary>Gets or sets the raw signed 32-bit tactical-zone value.</summary>
    public int ZoneGroupRaw { get; set; }

    /// <summary>Gets the known tactical-zone value, or <see langword="null"/> for a future raw value.</summary>
    public FhmTacticZone? ZoneGroup => Enum.IsDefined((FhmTacticZone)ZoneGroupRaw) ? (FhmTacticZone)ZoneGroupRaw : null;

    /// <summary>Gets or sets the system display name.</summary>
    public string? Name { get; set; }

    /// <summary>Gets or sets the first documented descriptor rating.</summary>
    public int RatingA { get; set; }

    /// <summary>Gets or sets the second documented descriptor rating.</summary>
    public int RatingB { get; set; }
}
