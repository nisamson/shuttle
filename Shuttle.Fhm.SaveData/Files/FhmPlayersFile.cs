using Shuttle.Fhm.SaveData.Binary;

namespace Shuttle.Fhm.SaveData.Files;

/// <summary>The version-58 <c>players.dat</c> container.</summary>
public sealed class FhmPlayersFile : IFhmSaveFile
{
    /// <summary>Current documented players format version.</summary>
    public const int CurrentVersion = 58;

    /// <inheritdoc />
    public string RelativePath => "players.dat";

    /// <summary>Gets or sets the validated FHM players format version.</summary>
    public int FormatVersion { get; set; } = CurrentVersion;

    /// <summary>Gets player records in serialized order.</summary>
    public IList<FhmPlayerRecord> Players { get; } = [];

    internal static FhmPlayersFile Read(FhmBinaryReader reader)
    {
        var result = new FhmPlayersFile
        {
            FormatVersion = reader.ReadInt32(),
        };
        if (result.FormatVersion != CurrentVersion)
        {
            throw new FhmFormatException($"players.dat format version {result.FormatVersion} is unsupported; expected {CurrentVersion}.");
        }

        var count = reader.ReadCount("players");
        for (var index = 0; index < count; index++)
        {
            result.Players.Add(FhmPlayerRecord.Read(reader));
        }

        reader.EnsureEof("players.dat");
        return result;
    }

    /// <inheritdoc />
    public void WriteTo(FhmBinaryWriter writer)
    {
        if (FormatVersion != CurrentVersion)
        {
            throw new FhmFormatException($"players.dat can write only format version {CurrentVersion}.");
        }

        writer.WriteInt32(FormatVersion);
        writer.WriteCount(Players.Count, "players");
        foreach (var player in Players)
        {
            player.WriteTo(writer);
        }
    }
}
