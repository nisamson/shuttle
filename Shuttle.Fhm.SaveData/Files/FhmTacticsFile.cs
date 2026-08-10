using Shuttle.Fhm.SaveData.Binary;

namespace Shuttle.Fhm.SaveData.Files;

/// <summary>The documented header and unresolved records payload of <c>tactics.dat</c>.</summary>
public sealed class FhmTacticsFile : IFhmSaveFile
{
    /// <inheritdoc />
    public string RelativePath => "tactics.dat";

    /// <summary>Gets or sets the tactic catalogue version.</summary>
    public int Version { get; set; }

    /// <summary>Gets or sets the documented tactic record count.</summary>
    public int TacticCount { get; set; }

    /// <summary>Gets or sets exact opaque record bytes.</summary>
    public byte[] RecordsOpaque { get; set; } = [];

    internal static FhmTacticsFile Read(FhmBinaryReader reader)
    {
        return new FhmTacticsFile
        {
            Version = reader.ReadInt32(),
            TacticCount = reader.ReadCount("tactics"),
            RecordsOpaque = reader.ReadRemaining(),
        };
    }

    /// <inheritdoc />
    public void WriteTo(FhmBinaryWriter writer)
    {
        writer.WriteInt32(Version);
        writer.WriteCount(TacticCount, "tactics");
        writer.WriteOpaqueBytes(new FhmOpaqueBytes(RecordsOpaque));
    }
}
