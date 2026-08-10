using Shuttle.Fhm.SaveData.Binary;

namespace Shuttle.Fhm.SaveData.Files;

/// <summary>A supported FHM save-folder file with a symmetric binary writer.</summary>
public interface IFhmSaveFile
{
    /// <summary>Gets the normalized filename relative to the FHM save root.</summary>
    string RelativePath { get; }

    /// <summary>Writes this file's complete wire representation.</summary>
    void WriteTo(FhmBinaryWriter writer);
}
