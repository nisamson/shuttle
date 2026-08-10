using System.Buffers.Binary;

namespace Shuttle.Fhm.SaveData.Binary;

/// <summary>Strict, big-endian Qt QDataStream primitives used by FHM 10 files.</summary>
public sealed class FhmBinaryReader : IDisposable
{
    private const int MaximumCollectionCount = 10_000_000;
    private readonly Stream stream;
    private readonly bool leaveOpen;

    /// <summary>Initializes a new reader over <paramref name="stream"/>.</summary>
    public FhmBinaryReader(Stream stream, bool leaveOpen = false)
    {
        ArgumentNullException.ThrowIfNull(stream);
        if (!stream.CanRead)
        {
            throw new ArgumentException("The stream must be readable.", nameof(stream));
        }

        this.stream = stream;
        this.leaveOpen = leaveOpen;
    }

    /// <summary>Gets the current byte offset.</summary>
    public long Position => stream.Position;

    /// <summary>Gets remaining bytes when the input is seekable.</summary>
    public long Remaining => stream.CanSeek ? stream.Length - stream.Position : throw new NotSupportedException("The input stream is not seekable.");

    /// <summary>Reads a signed big-endian 32-bit value.</summary>
    public int ReadInt32() => BinaryPrimitives.ReadInt32BigEndian(ReadExactly(sizeof(int)));

    /// <summary>Reads an unsigned big-endian 16-bit value.</summary>
    public ushort ReadUInt16() => BinaryPrimitives.ReadUInt16BigEndian(ReadExactly(sizeof(ushort)));

    /// <summary>Reads an unsigned byte.</summary>
    public byte ReadByte()
    {
        var value = stream.ReadByte();
        return value < 0 ? throw UnexpectedEndOfStream() : (byte)value;
    }

    /// <summary>Reads a signed big-endian 64-bit value.</summary>
    public long ReadInt64() => BinaryPrimitives.ReadInt64BigEndian(ReadExactly(sizeof(long)));

    /// <summary>Reads an IEEE-754 big-endian double.</summary>
    public double ReadDouble() => BitConverter.Int64BitsToDouble(ReadInt64());

    /// <summary>Reads a nullable Qt QString, preserving the null versus empty distinction.</summary>
    public string? ReadQString()
    {
        var byteLength = ReadInt32();
        if (byteLength == -1)
        {
            return null;
        }

        if (byteLength < 0 || (byteLength & 1) != 0)
        {
            throw new FhmFormatException($"Invalid QString length {byteLength} at offset {Position - sizeof(int)}.");
        }

        ValidateLength(byteLength, "QString");
        if (byteLength == 0)
        {
            return string.Empty;
        }

        var bytes = ReadExactly(byteLength);
        var characters = new char[byteLength / sizeof(char)];
        for (var index = 0; index < characters.Length; index++)
        {
            characters[index] = (char)BinaryPrimitives.ReadUInt16BigEndian(bytes.AsSpan(index * sizeof(char), sizeof(char)));
        }

        return new string(characters);
    }

    /// <summary>Reads a three-int32 QDate.</summary>
    public FhmDate ReadDate() => new(ReadInt32(), ReadInt32(), ReadInt32());

    /// <summary>Reads an exact fixed-size opaque block.</summary>
    public FhmOpaqueBytes ReadOpaqueBytes(int length) => new(ReadExactly(length));

    /// <summary>Reads a validated QList count.</summary>
    public int ReadCount(string collectionName)
    {
        var count = ReadInt32();
        if (count < 0 || count > MaximumCollectionCount)
        {
            throw new FhmFormatException($"Invalid {collectionName} count {count} at offset {Position - sizeof(int)}.");
        }

        return count;
    }

    /// <summary>Reads all remaining bytes.</summary>
    public byte[] ReadRemaining()
    {
        if (!stream.CanSeek)
        {
            using var memory = new MemoryStream();
            stream.CopyTo(memory);
            return memory.ToArray();
        }

        return ReadExactly(checked((int)Remaining));
    }

    /// <summary>Peeks a signed big-endian 32-bit value without advancing the reader.</summary>
    public int PeekInt32(int relativeOffset = 0) => BinaryPrimitives.ReadInt32BigEndian(PeekBytes(sizeof(int), relativeOffset));

    /// <summary>Peeks exact bytes relative to the current reader position without advancing it.</summary>
    public byte[] PeekBytes(int length, int relativeOffset = 0)
    {
        if (!stream.CanSeek)
        {
            throw new NotSupportedException("Peeking FHM data requires a seekable input stream.");
        }

        if (length < 0 || relativeOffset < 0)
        {
            throw new ArgumentOutOfRangeException(length < 0 ? nameof(length) : nameof(relativeOffset));
        }

        var position = stream.Position;
        try
        {
            var target = checked(position + relativeOffset);
            if (target > stream.Length || length > stream.Length - target)
            {
                throw new FhmFormatException($"Cannot peek {length} bytes at offset {target}; input ends at {stream.Length}.");
            }

            stream.Position = target;
            var result = new byte[length];
            var read = 0;
            while (read < result.Length)
            {
                var current = stream.Read(result, read, result.Length - read);
                if (current == 0)
                {
                    throw UnexpectedEndOfStream();
                }

                read += current;
            }

            return result;
        }
        finally
        {
            stream.Position = position;
        }
    }

    /// <summary>Requires the reader to be at EOF.</summary>
    public void EnsureEof(string fileName)
    {
        if (stream.ReadByte() != -1)
        {
            throw new FhmFormatException($"{fileName} contains unread bytes at offset {Position - 1}.");
        }
    }

    /// <summary>Disposes the reader.</summary>
    public void Dispose()
    {
        if (!leaveOpen)
        {
            stream.Dispose();
        }
    }

    private byte[] ReadExactly(int length)
    {
        if (length < 0)
        {
            throw new FhmFormatException($"Negative byte count {length} at offset {Position}.");
        }

        ValidateLength(length, "block");
        var result = new byte[length];
        var read = 0;
        while (read < length)
        {
            var current = stream.Read(result, read, length - read);
            if (current == 0)
            {
                throw UnexpectedEndOfStream();
            }

            read += current;
        }

        return result;
    }

    private void ValidateLength(int length, string valueName)
    {
        if (stream.CanSeek && length > Remaining)
        {
            throw new FhmFormatException($"The {valueName} length {length} at offset {Position} exceeds remaining input.");
        }
    }

    private EndOfStreamException UnexpectedEndOfStream() => new($"Unexpected end of FHM data at offset {Position}.");
}
