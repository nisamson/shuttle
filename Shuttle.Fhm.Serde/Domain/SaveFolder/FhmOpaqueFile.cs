namespace Shuttle.Fhm.Serde.Domain.SaveFolder;

/// <summary>An unsupported file retained exactly, relative to one FHM save root.</summary>
public sealed class FhmOpaqueFile
{
    /// <summary>Initializes a new opaque file.</summary>
    public FhmOpaqueFile(string relativePath, byte[] content)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);
        ArgumentNullException.ThrowIfNull(content);
        RelativePath = relativePath;
        Content = content;
    }

    /// <summary>Gets the normalized relative path using forward slashes.</summary>
    public string RelativePath { get; }

    /// <summary>Gets the exact file content.</summary>
    public byte[] Content { get; }
}
