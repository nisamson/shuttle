using Shuttle.Fhm.SaveData.Binary;

namespace Shuttle.Fhm.SaveData.Files;

/// <summary>A length-prefixed opaque data item in an FHM catalogue.</summary>
public sealed class FhmLengthPrefixedBlock
{
    /// <summary>Initializes a new length-prefixed block.</summary>
    public FhmLengthPrefixedBlock(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);
        Data = data;
    }

    /// <summary>Gets the opaque bytes after their int32 length.</summary>
    public byte[] Data { get; }
}

/// <summary>A versioned catalogue containing an optional count followed by length-prefixed opaque data blocks.</summary>
public sealed class FhmLengthPrefixedCatalogueFile : IFhmSaveFile
{
    /// <summary>Initializes a new catalogue file.</summary>
    public FhmLengthPrefixedCatalogueFile(string relativePath, bool hasCount)
    {
        RelativePath = relativePath;
        HasCount = hasCount;
    }

    /// <inheritdoc />
    public string RelativePath { get; }

    /// <summary>Gets whether a count follows the version.</summary>
    public bool HasCount { get; }

    /// <summary>Gets or sets the version tag.</summary>
    public int Version { get; set; }

    /// <summary>Gets blocks in serialized order.</summary>
    public IList<FhmLengthPrefixedBlock> Blocks { get; } = [];

    internal static FhmLengthPrefixedCatalogueFile Read(string relativePath, bool hasCount, FhmBinaryReader reader)
    {
        var result = new FhmLengthPrefixedCatalogueFile(relativePath, hasCount) { Version = reader.ReadInt32() };
        var declaredCount = hasCount ? reader.ReadCount($"{relativePath} records") : -1;
        while (reader.Remaining > 0)
        {
            if (hasCount && result.Blocks.Count >= declaredCount)
            {
                throw new FhmFormatException($"{relativePath} has bytes after its declared block count.");
            }

            var length = reader.ReadInt32();
            if (length < 0)
            {
                throw new FhmFormatException($"{relativePath} contains a negative block length.");
            }

            result.Blocks.Add(new FhmLengthPrefixedBlock(reader.ReadOpaqueBytes(length).Value));
        }

        if (hasCount && result.Blocks.Count != declaredCount)
        {
            throw new FhmFormatException($"{relativePath} expected {declaredCount} blocks but found {result.Blocks.Count}.");
        }

        return result;
    }

    /// <inheritdoc />
    public void WriteTo(FhmBinaryWriter writer)
    {
        writer.WriteInt32(Version);
        if (HasCount)
        {
            writer.WriteCount(Blocks.Count, $"{RelativePath} records");
        }

        foreach (var block in Blocks)
        {
            writer.WriteCount(block.Data.Length, $"{RelativePath} block bytes");
            writer.WriteOpaqueBytes(new FhmOpaqueBytes(block.Data));
        }
    }
}

/// <summary>The named 4,856-byte tactic templates in <c>tactic_templates.dat</c>.</summary>
public sealed class FhmTacticTemplatesFile : IFhmSaveFile
{
    /// <summary>Size of each documented template settings payload.</summary>
    public const int SettingsBlobLength = 4856;

    /// <inheritdoc />
    public string RelativePath => "tactic_templates.dat";

    /// <summary>Gets or sets the file format version.</summary>
    public int Version { get; set; }

    /// <summary>Gets named templates in wire order.</summary>
    public IList<FhmTacticTemplate> Templates { get; } = [];

    internal static FhmTacticTemplatesFile Read(FhmBinaryReader reader)
    {
        var result = new FhmTacticTemplatesFile { Version = reader.ReadInt32() };
        var count = reader.ReadCount("tactic templates");
        for (var index = 0; index < count; index++)
        {
            result.Templates.Add(new FhmTacticTemplate(
                reader.ReadQString(),
                reader.ReadInt32(),
                reader.ReadQString(),
                reader.ReadOpaqueBytes(SettingsBlobLength).Value));
        }

        reader.EnsureEof("tactic_templates.dat");
        return result;
    }

    /// <inheritdoc />
    public void WriteTo(FhmBinaryWriter writer)
    {
        writer.WriteInt32(Version);
        writer.WriteCount(Templates.Count, "tactic templates");
        foreach (var template in Templates)
        {
            if (template.SettingsBlob.Length != SettingsBlobLength)
            {
                throw new FhmFormatException($"Tactic template settings blobs must be exactly {SettingsBlobLength} bytes.");
            }

            writer.WriteQString(template.InternalKey);
            writer.WriteInt32(template.TemplateIndex);
            writer.WriteQString(template.DisplayName);
            writer.WriteOpaqueBytes(new FhmOpaqueBytes(template.SettingsBlob));
        }
    }
}

/// <summary>One named tactic template retaining its unresolved settings blob exactly.</summary>
public sealed record FhmTacticTemplate(string? InternalKey, int TemplateIndex, string? DisplayName, byte[] SettingsBlob);
