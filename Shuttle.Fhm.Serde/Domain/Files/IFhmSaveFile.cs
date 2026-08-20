namespace Shuttle.Fhm.Serde.Domain.Files;

/// <summary>A supported FHM save-folder file with a symmetric serializer.</summary>
public interface IFhmSaveFile
{
    /// <summary>Gets the normalized filename relative to the FHM save root.</summary>
    string RelativePath { get; }

    /// <summary>Writes this file's complete wire representation.</summary>
    void WriteTo(Stream stream);
}
