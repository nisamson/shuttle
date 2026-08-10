using Shuttle.Fhm.SaveData.Binary;

namespace Shuttle.Fhm.SaveData.Files;

/// <summary>The <c>teams.dat</c> container.</summary>
public sealed class FhmTeamsFile : IFhmSaveFile
{
    /// <inheritdoc />
    public string RelativePath => "teams.dat";

    /// <summary>Gets or sets the teams file format version.</summary>
    public int VersionTag { get; set; }

    /// <summary>Gets team records in serialized order.</summary>
    public IList<FhmTeamRecord> Teams { get; } = [];

    internal static FhmTeamsFile Read(FhmBinaryReader reader)
    {
        var result = new FhmTeamsFile
        {
            VersionTag = reader.ReadInt32(),
        };
        var count = reader.ReadCount("teams");
        for (var index = 0; index < count; index++)
        {
            result.Teams.Add(FhmTeamRecord.Read(reader));
        }

        reader.EnsureEof("teams.dat");
        return result;
    }

    /// <inheritdoc />
    public void WriteTo(FhmBinaryWriter writer)
    {
        writer.WriteInt32(VersionTag);
        writer.WriteCount(Teams.Count, "teams");
        foreach (var team in Teams)
        {
            team.WriteTo(writer);
        }
    }
}
