using System.Text;
using BinarySerialization;

namespace Shuttle.BinarySerde.Common.QFormat;

public static class QSerializerFactory {
    public static BinarySerializer Create() {
        return new() {
            Encoding = Encoding.BigEndianUnicode,
            Endianness = Endianness.Big
        };
    }

    public static T Deserialize<T>(Stream stream, string sourceName)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceName);

        var value = Create().Deserialize<T>(stream);
        if (stream.CanSeek && stream.Position != stream.Length)
        {
            throw new InvalidDataException($"{sourceName} contains unread bytes at offset {stream.Position}.");
        }

        return value;
    }

    public static T DeserializeOne<T>(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        return Create().Deserialize<T>(stream);
    }

    public static void Serialize<T>(Stream stream, T value)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(value);
        Create().Serialize(stream, value);
    }
}
