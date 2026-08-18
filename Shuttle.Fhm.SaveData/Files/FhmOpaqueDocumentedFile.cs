using System.Buffers.Binary;

namespace Shuttle.Fhm.SaveData.Files;

/// <summary>A documented file whose file-level header is known while its payload remains intentionally opaque.</summary>
public sealed class FhmOpaqueDocumentedFile : IFhmSaveFile
{
    /// <summary>Initializes a new opaque documented file.</summary>
    public FhmOpaqueDocumentedFile(string relativePath, int? version, byte[] payload)
    {
        RelativePath = relativePath;
        Version = version;
        ArgumentNullException.ThrowIfNull(payload);
        Payload = payload;
    }

    /// <inheritdoc />
    public string RelativePath { get; }

    /// <summary>Gets the known leading version, if the format has one.</summary>
    public int? Version { get; set; }

    /// <summary>Gets the exact opaque payload after the documented header.</summary>
    public byte[] Payload { get; }

    /// <inheritdoc />
    public void WriteTo(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        if (Version is int version)
        {
            Span<byte> versionBytes = stackalloc byte[sizeof(int)];
            BinaryPrimitives.WriteInt32BigEndian(versionBytes, version);
            stream.Write(versionBytes);
        }

        stream.Write(Payload);
    }
}
