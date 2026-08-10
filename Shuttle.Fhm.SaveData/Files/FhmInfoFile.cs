using Shuttle.Fhm.SaveData.Binary;

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

    internal static FhmInfoFile Read(FhmBinaryReader reader)
    {
        var result = new FhmInfoFile
        {
            Description = reader.ReadQString(),
            NameId = reader.ReadQString(),
        };
        reader.EnsureEof("info.dat");
        return result;
    }

    /// <inheritdoc />
    public void WriteTo(FhmBinaryWriter writer)
    {
        writer.WriteQString(Description);
        writer.WriteQString(NameId);
    }
}
