using Shuttle.Fhm.Serde.Info;

namespace Shuttle.Fhm.SaveData.Files;

/// <summary>The two Qt strings in <c>info.dat</c>.</summary>
public sealed class FhmInfoFile : IFhmSaveFile
{
    /// <inheritdoc />
    public string RelativePath => "info.dat";

    /// <summary>Gets or sets the load-dialog description.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the save or world name identifier.</summary>
    public string? NameId { get; set; }

    internal static FhmInfoFile Read(Stream stream)
    {
        var wire = FhmInfoFileSerializer.Deserialize(stream);
        return new FhmInfoFile
        {
            Description = wire.Description.Value,
            NameId = wire.NameId.Value,
        };
    }

    public void WriteTo(Stream stream)
    {
        FhmInfoFileSerializer.Serialize(stream, new FhmInfoFileData
        {
            Description = new() { Value = Description },
            NameId = new() { Value = NameId },
        });
    }
}
