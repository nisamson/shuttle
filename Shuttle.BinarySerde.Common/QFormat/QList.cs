using System.Buffers.Binary;
using BinarySerialization;

namespace Shuttle.BinarySerde.Common.QFormat;

public record QList<T> {
    [FieldOrder(0)]
    public int Length { get; set; }
    
    [FieldOrder(1)]
    [FieldCount(nameof(Length))]
    public List<T> Items { get; set; } = new List<T>();
}

public sealed record QString : IBinarySerializable {
    public string? Value { get; set; }

    public void Serialize(Stream stream, Endianness endianness, BinarySerializationContext context)
    {
        ArgumentNullException.ThrowIfNull(stream);
        if (endianness != Endianness.Big)
        {
            throw new NotSupportedException("Qt QString values require big-endian serialization.");
        }

        if (Value is null)
        {
            WriteInt32(stream, -1);
            return;
        }

        WriteInt32(stream, checked(Value.Length * sizeof(char)));
        Span<byte> bytes = stackalloc byte[sizeof(char)];
        foreach (var character in Value)
        {
            BinaryPrimitives.WriteUInt16BigEndian(bytes, character);
            stream.Write(bytes);
        }
    }

    public void Deserialize(Stream stream, Endianness endianness, BinarySerializationContext context)
    {
        ArgumentNullException.ThrowIfNull(stream);
        if (endianness != Endianness.Big)
        {
            throw new NotSupportedException("Qt QString values require big-endian serialization.");
        }

        var byteLength = ReadInt32(stream);
        if (byteLength == -1)
        {
            Value = null;
            return;
        }

        if (byteLength < 0 || (byteLength & 1) != 0)
        {
            throw new InvalidDataException($"Qt QString has invalid byte length {byteLength}.");
        }

        var bytes = new byte[byteLength];
        stream.ReadExactly(bytes);
        var characters = new char[byteLength / sizeof(char)];
        for (var index = 0; index < characters.Length; index++)
        {
            characters[index] = (char)BinaryPrimitives.ReadUInt16BigEndian(bytes.AsSpan(index * sizeof(char), sizeof(char)));
        }

        Value = new string(characters);
    }

    private static int ReadInt32(Stream stream)
    {
        Span<byte> bytes = stackalloc byte[sizeof(int)];
        stream.ReadExactly(bytes);
        return BinaryPrimitives.ReadInt32BigEndian(bytes);
    }

    private static void WriteInt32(Stream stream, int value)
    {
        Span<byte> bytes = stackalloc byte[sizeof(int)];
        BinaryPrimitives.WriteInt32BigEndian(bytes, value);
        stream.Write(bytes);
    }
}