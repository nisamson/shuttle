using Shuttle.Fhm.SaveData.Binary;
using Shuttle.Fhm.Serde.SetPlays;

namespace Shuttle.Fhm.SaveData.Files;

/// <summary>One of the eight <c>set_play_*.dat</c> formation catalogues.</summary>
public sealed class FhmSetPlayFile : IFhmSaveFile
{
    private const int MaximumCollectionCount = 10_000_000;

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

    internal static FhmSetPlayFile Read(string relativePath, Stream stream)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);
        ArgumentNullException.ThrowIfNull(stream);

        FhmSetPlayHeaderData header;
        try
        {
            header = FhmSetPlayFileSerializer.DeserializeHeader(stream);
        }
        catch (InvalidDataException exception)
        {
            throw new FhmFormatException(exception.Message);
        }

        if (header.FormationCount < 0 || header.FormationCount > MaximumCollectionCount)
        {
            throw new FhmFormatException($"Invalid {relativePath} formations count {header.FormationCount}.");
        }

        var result = new FhmSetPlayFile(relativePath) { Version = header.Version };
        ReadBlocks(stream, result.Formations, header.FormationCount, $"{relativePath} formation");
        while (!stream.CanSeek || stream.Position < stream.Length)
        {
            ReadBlocks(stream, result.ExtraRecords, 1, $"{relativePath} extra record");
        }

        return result;
    }

    public void WriteTo(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ValidateForWrite();
        FhmSetPlayFileSerializer.SerializeHeader(stream, new FhmSetPlayHeaderData
        {
            Version = Version,
            FormationCount = Formations.Count,
        });
        WriteBlocks(stream, Formations);
        WriteBlocks(stream, ExtraRecords);
    }

    private static void ReadBlocks(Stream stream, ICollection<FhmLengthPrefixedBlock> destination, int count, string description)
    {
        for (var index = 0; index < count; index++)
        {
            FhmSetPlayBlockData block;
            try
            {
                block = FhmSetPlayFileSerializer.DeserializeBlock(stream);
            }
            catch (InvalidDataException exception)
            {
                throw new FhmFormatException(exception.Message);
            }

            if (block.ByteLength < 0)
            {
                throw new FhmFormatException($"{description} has a negative byte length.");
            }

            destination.Add(new FhmLengthPrefixedBlock(block.Data));
        }
    }

    private static void WriteBlocks(Stream stream, IEnumerable<FhmLengthPrefixedBlock> blocks)
    {
        foreach (var block in blocks)
        {
            ArgumentNullException.ThrowIfNull(block);
            FhmSetPlayFileSerializer.SerializeBlock(stream, new FhmSetPlayBlockData
            {
                ByteLength = block.Data.Length,
                Data = block.Data,
            });
        }
    }

    private void ValidateForWrite()
    {
        if (Formations.Count > MaximumCollectionCount)
        {
            throw new FhmFormatException($"Invalid {RelativePath} formations count {Formations.Count}.");
        }
    }
}
