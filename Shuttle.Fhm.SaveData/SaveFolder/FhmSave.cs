using Shuttle.Fhm.SaveData.Files;

namespace Shuttle.Fhm.SaveData.SaveFolder;

/// <summary>One FHM 10 save folder, including modeled files and every unsupported opaque file.</summary>
public sealed class FhmSave
{
    /// <summary>Gets modeled documented files keyed by normalized relative path.</summary>
    public IDictionary<string, IFhmSaveFile> Files { get; } = new Dictionary<string, IFhmSaveFile>(StringComparer.OrdinalIgnoreCase);

    /// <summary>Gets unsupported path-preserving file content.</summary>
    public IList<FhmOpaqueFile> OpaqueFiles { get; } = [];
}
