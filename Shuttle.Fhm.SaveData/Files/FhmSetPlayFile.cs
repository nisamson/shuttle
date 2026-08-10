using Shuttle.Fhm.SaveData.Binary;

namespace Shuttle.Fhm.SaveData.Files;

/// <summary>One of the eight <c>set_play_*.dat</c> formation catalogues.</summary>
public sealed class FhmSetPlayFile : IFhmSaveFile
{
    /// <summary>Initializes a set-play catalogue for its root-relative filename.</summary>
    public FhmSetPlayFile(string relativePath)
    {
        RelativePath = relativePath;
    }

    /// <inheritdoc />
    public string RelativePath { get; }

    /// <summary>Gets or sets the format version.</summary>
    public int Version { get; set; }

    /// <summary>Gets selectable formation records in serialized order.</summary>
    public IList<FhmLengthPrefixedBlock> Formations { get; } = [];

    /// <summary>Gets trailing documented but unresolved numeric catalogue records.</summary>
    public IList<FhmLengthPrefixedBlock> ExtraRecords { get; } = [];

    internal static FhmSetPlayFile Read(string relativePath, FhmBinaryReader reader)
    {
        var result = new FhmSetPlayFile(relativePath) { Version = reader.ReadInt32() };
        var formationCount = reader.ReadCount($"{relativePath} formations");
        ReadBlocks(reader, result.Formations, formationCount, $"{relativePath} formation");
        while (reader.Remaining > 0)
        {
            ReadBlocks(reader, result.ExtraRecords, 1, $"{relativePath} extra record");
        }

        return result;
    }

    /// <inheritdoc />
    public void WriteTo(FhmBinaryWriter writer)
    {
        writer.WriteInt32(Version);
        writer.WriteCount(Formations.Count, $"{RelativePath} formations");
        WriteBlocks(writer, Formations, $"{RelativePath} formation");
        WriteBlocks(writer, ExtraRecords, $"{RelativePath} extra record");
    }

    private static void ReadBlocks(FhmBinaryReader reader, ICollection<FhmLengthPrefixedBlock> destination, int count, string description)
    {
        for (var index = 0; index < count; index++)
        {
            var length = reader.ReadInt32();
            if (length < 0)
            {
                throw new FhmFormatException($"{description} has a negative byte length.");
            }

            destination.Add(new FhmLengthPrefixedBlock(reader.ReadOpaqueBytes(length).Value));
        }
    }

    private static void WriteBlocks(FhmBinaryWriter writer, IEnumerable<FhmLengthPrefixedBlock> blocks, string description)
    {
        foreach (var block in blocks)
        {
            writer.WriteCount(block.Data.Length, $"{description} bytes");
            writer.WriteOpaqueBytes(new FhmOpaqueBytes(block.Data));
        }
    }
}
