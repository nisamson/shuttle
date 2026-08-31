using Shuttle.Fhm.Serde.Domain.Files;

namespace Shuttle.Fhm.Serde.Domain.SaveFolder;

/// <summary>Creates supported save-file models from FHM file content.</summary>
internal static class FhmSaveFileFactory
{
    /// <summary>Gets whether a normalized path has a documented file codec.</summary>
    public static bool IsDocumentedPath(string relativePath) =>
        FhmFileCodecs.IsDocumentedPath(FhmSavePath.NormalizeRelativePath(relativePath));

    /// <summary>
    /// Parses a documented save file from its normalized relative path and wire stream.
    /// Returns <see langword="null"/> when the path has no supported documented codec.
    /// The caller retains ownership of <paramref name="content"/>.
    /// </summary>
    public static IFhmSaveFile? TryRead(string relativePath, Stream content)
    {
        ArgumentNullException.ThrowIfNull(content);
        var normalizedPath = FhmSavePath.NormalizeRelativePath(relativePath);
        return FhmFileCodecs.TryRead(normalizedPath, content);
    }

    /// <summary>
    /// Parses a documented save file from its normalized relative path and complete wire bytes.
    /// Returns <see langword="null"/> when the path has no supported documented codec.
    /// </summary>
    public static IFhmSaveFile? TryRead(string relativePath, byte[] content)
    {
        ArgumentNullException.ThrowIfNull(content);
        using var stream = new MemoryStream(content, writable: false);
        return TryRead(relativePath, stream);
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
