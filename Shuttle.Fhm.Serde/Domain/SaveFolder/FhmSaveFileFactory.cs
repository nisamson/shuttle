using Shuttle.Fhm.Serde.Domain.Files;

namespace Shuttle.Fhm.Serde.Domain.SaveFolder;

/// <summary>Creates supported save-file models from in-memory FHM file content.</summary>
internal static class FhmSaveFileFactory
{
    /// <summary>
    /// Parses a documented save file from its normalized relative path and complete wire bytes.
    /// Returns <see langword="null"/> when the path has no supported documented codec.
    /// </summary>
    public static IFhmSaveFile? TryRead(string relativePath, byte[] content)
    {
        ArgumentNullException.ThrowIfNull(content);
        var normalizedPath = FhmSavePath.NormalizeRelativePath(relativePath);
        return FhmFileCodecs.TryRead(normalizedPath, content);
    }

    /// <summary>Serializes a supported save file to its complete wire representation.</summary>
    public static byte[] Write(IFhmSaveFile file)
    {
        ArgumentNullException.ThrowIfNull(file);
        _ = FhmSavePath.NormalizeRelativePath(file.RelativePath);
        using var stream = new MemoryStream();
        file.WriteTo(stream);
        return stream.ToArray();
    }

    /// <summary>
    /// Creates a supported-file container that preserves the supplied wire bytes without parsing
    /// or reserializing them.
    /// </summary>
    public static IFhmSaveFile CreateRaw(string relativePath, byte[] content)
    {
        ArgumentNullException.ThrowIfNull(content);
        return new FhmRawSaveFile(FhmSavePath.NormalizeRelativePath(relativePath), content);
    }

    /// <summary>Normalizes and validates a relative save-file path.</summary>
    public static string NormalizeRelativePath(string relativePath) => FhmSavePath.NormalizeRelativePath(relativePath);
}
