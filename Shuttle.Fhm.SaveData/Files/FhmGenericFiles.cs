using Shuttle.Fhm.SaveData.Binary;
using Shuttle.Fhm.Serde.Generic;

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

    internal static FhmLengthPrefixedCatalogueFile Read(string relativePath, bool hasCount, Stream stream)
    {
        var header = hasCount
            ? FhmLengthPrefixedCatalogueSerializer.DeserializeCountedHeader(stream)
            : null;
        var result = new FhmLengthPrefixedCatalogueFile(relativePath, hasCount)
        {
            Version = header?.Version ?? FhmLengthPrefixedCatalogueSerializer.DeserializeHeader(stream).Version,
        };
        var declaredCount = header?.Count ?? -1;
        if (declaredCount < -1)
        {
            throw new FhmFormatException($"{relativePath} contains a negative block count.");
        }

        while (!stream.CanSeek || stream.Position < stream.Length)
        {
            if (hasCount && result.Blocks.Count >= declaredCount)
            {
                throw new FhmFormatException($"{relativePath} has bytes after its declared block count.");
            }

            var block = FhmLengthPrefixedCatalogueSerializer.DeserializeBlock(stream);
            if (block.ByteLength < 0)
            {
                throw new FhmFormatException($"{relativePath} contains a negative block length.");
            }

            result.Blocks.Add(new FhmLengthPrefixedBlock(block.Data));
        }

        if (hasCount && result.Blocks.Count != declaredCount)
        {
            throw new FhmFormatException($"{relativePath} expected {declaredCount} blocks but found {result.Blocks.Count}.");
        }

        return result;
    }

    public void WriteTo(Stream stream)
    {
        if (HasCount)
        {
            FhmLengthPrefixedCatalogueSerializer.SerializeCountedHeader(stream, new FhmCountedCatalogueHeaderData
            {
                Version = Version,
                Count = Blocks.Count,
            });
        }
        else
        {
            FhmLengthPrefixedCatalogueSerializer.SerializeHeader(stream, new FhmCatalogueHeaderData { Version = Version });
        }

        foreach (var block in Blocks)
        {
            FhmLengthPrefixedCatalogueSerializer.SerializeBlock(stream, new FhmLengthPrefixedBlockData
            {
                ByteLength = block.Data.Length,
                Data = block.Data,
            });
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

    internal static FhmTacticTemplatesFile Read(Stream stream)
    {
        var wire = FhmTacticTemplatesSerializer.Deserialize(stream);
        if (wire.Count < 0 || wire.Count != wire.Templates.Count)
        {
            throw new FhmFormatException($"tactic_templates.dat expected {wire.Count} templates but found {wire.Templates.Count}.");
        }

        var result = new FhmTacticTemplatesFile { Version = wire.Version };
        foreach (var template in wire.Templates)
        {
            result.Templates.Add(new(
                template.InternalKey.Value,
                template.TemplateIndex,
                template.DisplayName.Value,
                template.SettingsBlob));
        }

        return result;
    }

    public void WriteTo(Stream stream)
    {
        foreach (var template in Templates)
        {
            if (template.SettingsBlob.Length != SettingsBlobLength)
            {
                throw new FhmFormatException($"Tactic template settings blobs must be exactly {SettingsBlobLength} bytes.");
            }
        }

        FhmTacticTemplatesSerializer.Serialize(stream, new FhmTacticTemplatesData
        {
            Version = Version,
            Count = Templates.Count,
            Templates = Templates.Select(template => new FhmTacticTemplateData
            {
                InternalKey = new() { Value = template.InternalKey },
                TemplateIndex = template.TemplateIndex,
                DisplayName = new() { Value = template.DisplayName },
                SettingsBlob = template.SettingsBlob,
            }).ToList(),
        });
    }
}

/// <summary>One named tactic template retaining its unresolved settings blob exactly.</summary>
public sealed record FhmTacticTemplate(string? InternalKey, int TemplateIndex, string? DisplayName, byte[] SettingsBlob);
