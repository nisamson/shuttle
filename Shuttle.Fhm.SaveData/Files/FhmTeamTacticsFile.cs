using Shuttle.Fhm.SaveData.Binary;
using Shuttle.Fhm.SaveData.Model;
using Shuttle.Fhm.Serde.Teams;

namespace Shuttle.Fhm.SaveData.Files;

/// <summary>The selectable per-zone tactic-system catalogue in <c>team_tactics.dat</c>.</summary>
public sealed class FhmTeamTacticsFile : IFhmSaveFile
{
    private const int MaximumCollectionCount = 10_000_000;

    /// <inheritdoc />
    public string RelativePath => "team_tactics.dat";

    /// <summary>Gets or sets the catalogue format version.</summary>
    public int VersionTag { get; set; }

    /// <summary>Gets selectable tactic systems in serialized order.</summary>
    public IList<FhmTacticSystem> Records { get; } = [];

    internal static FhmTeamTacticsFile Read(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        FhmTeamTacticsFileData wire;
        try
        {
            wire = FhmTeamTacticsFileSerializer.Deserialize(stream);
        }
        catch (InvalidDataException exception)
        {
            throw new FhmFormatException(exception.Message);
        }

        if (wire.RecordCount < 0 || wire.RecordCount > MaximumCollectionCount || wire.Records.Count != wire.RecordCount)
        {
            throw new FhmFormatException($"Invalid team tactic systems count {wire.RecordCount}.");
        }

        var result = new FhmTeamTacticsFile { VersionTag = wire.VersionTag };
        foreach (var record in wire.Records)
        {
            result.Records.Add(new()
            {
                GlobalId = record.GlobalId,
                ZoneGroupRaw = record.ZoneGroupRaw,
                Name = record.Name.Value,
                RatingA = record.RatingA,
                RatingB = record.RatingB,
            });
        }

        return result;
    }

    public void WriteTo(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        if (Records.Count > MaximumCollectionCount)
        {
            throw new FhmFormatException($"Invalid team tactic systems count {Records.Count}.");
        }

        FhmTeamTacticsFileSerializer.Serialize(stream, new()
        {
            VersionTag = VersionTag,
            RecordCount = Records.Count,
            Records = Records.Select(record =>
            {
                ArgumentNullException.ThrowIfNull(record);
                return new FhmTacticSystemData
                {
                    GlobalId = record.GlobalId,
                    ZoneGroupRaw = record.ZoneGroupRaw,
                    Name = new() { Value = record.Name },
                    RatingA = record.RatingA,
                    RatingB = record.RatingB,
                };
            }).ToList(),
        });
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
