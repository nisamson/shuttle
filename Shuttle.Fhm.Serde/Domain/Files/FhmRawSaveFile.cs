namespace Shuttle.Fhm.Serde.Domain.Files;

/// <summary>
/// A supported save-file entry retained as complete wire bytes. This is useful when a caller
/// needs lossless persistence without changing the file's documented model.
/// </summary>
public sealed class FhmRawSaveFile : IFhmSaveFile
{
    /// <summary>Initializes a raw supported-file entry.</summary>
    public FhmRawSaveFile(string relativePath, byte[] content)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);
        ArgumentNullException.ThrowIfNull(content);
        RelativePath = relativePath;
        Content = content;
    }

    /// <inheritdoc />
    public string RelativePath { get; }

    /// <summary>Gets the complete original wire content.</summary>
    public byte[] Content { get; }

    /// <inheritdoc />
    public void WriteTo(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        stream.Write(Content);
    }
}
