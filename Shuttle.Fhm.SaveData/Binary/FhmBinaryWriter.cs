using System.Buffers.Binary;

namespace Shuttle.Fhm.SaveData.Binary;

/// <summary>Big-endian Qt QDataStream primitives used to write FHM 10 files.</summary>
public sealed class FhmBinaryWriter : IDisposable
{
    private readonly Stream stream;
    private readonly bool leaveOpen;

    /// <summary>Initializes a new writer over <paramref name="stream"/>.</summary>
    public FhmBinaryWriter(Stream stream, bool leaveOpen = false)
    {
        ArgumentNullException.ThrowIfNull(stream);
        if (!stream.CanWrite)
        {
            throw new ArgumentException("The stream must be writable.", nameof(stream));
        }

        this.stream = stream;
        this.leaveOpen = leaveOpen;
    }

    /// <summary>Writes a signed big-endian 32-bit value.</summary>
    public void WriteInt32(int value)
    {
        Span<byte> bytes = stackalloc byte[sizeof(int)];
        BinaryPrimitives.WriteInt32BigEndian(bytes, value);
        stream.Write(bytes);
    }

    /// <summary>Writes an unsigned big-endian 16-bit value.</summary>
    public void WriteUInt16(ushort value)
    {
        Span<byte> bytes = stackalloc byte[sizeof(ushort)];
        BinaryPrimitives.WriteUInt16BigEndian(bytes, value);
        stream.Write(bytes);
    }

    /// <summary>Writes one unsigned byte.</summary>
    public void WriteByte(byte value) => stream.WriteByte(value);

    /// <summary>Writes a signed big-endian 64-bit value.</summary>
    public void WriteInt64(long value)
    {
        Span<byte> bytes = stackalloc byte[sizeof(long)];
        BinaryPrimitives.WriteInt64BigEndian(bytes, value);
        stream.Write(bytes);
    }

    /// <summary>Writes an IEEE-754 big-endian double.</summary>
    public void WriteDouble(double value) => WriteInt64(BitConverter.DoubleToInt64Bits(value));

    /// <summary>Writes a nullable Qt QString.</summary>
    public void WriteQString(string? value)
    {
        if (value is null)
        {
            WriteInt32(-1);
            return;
        }

        WriteInt32(checked(value.Length * sizeof(char)));
        Span<byte> bytes = stackalloc byte[sizeof(char)];
        foreach (var character in value)
        {
            BinaryPrimitives.WriteUInt16BigEndian(bytes, character);
            stream.Write(bytes);
        }
    }

    /// <summary>Writes a three-int32 QDate.</summary>
    public void WriteDate(FhmDate value)
    {
        WriteInt32(value.Year);
        WriteInt32(value.Month);
        WriteInt32(value.Day);
    }

    /// <summary>Writes an exact opaque byte sequence.</summary>
    public void WriteOpaqueBytes(FhmOpaqueBytes value)
    {
        ArgumentNullException.ThrowIfNull(value);
        stream.Write(value.Value);
    }

    /// <summary>Writes a validated QList count.</summary>
    public void WriteCount(int count, string collectionName)
    {
        if (count < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count), $"{collectionName} cannot have a negative count.");
        }

        WriteInt32(count);
    }

    /// <summary>Disposes the writer.</summary>
    public void Dispose()
    {
        if (!leaveOpen)
        {
            stream.Dispose();
        }
    }
}
