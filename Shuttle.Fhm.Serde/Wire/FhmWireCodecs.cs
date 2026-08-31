using Shuttle.BinarySerde.Common.QFormat;
using Shuttle.BinarySerde.Dsl;

namespace Shuttle.Fhm.Serde.Wire;

/// <summary>Reflection-free codecs for Qt primitives used by FHM wire formats.</summary>
internal static class FhmWireCodecs
{
    internal const int MaximumCollectionCount = 10_000_000;

    internal static readonly ValueCodec<byte> Byte = new(
        static reader => reader.ReadByte(),
        static (writer, value) => writer.WriteByte(value));
    internal static readonly ValueCodec<ushort> UInt16 = new(
        static reader => reader.ReadUInt16(),
        static (writer, value) => writer.WriteUInt16(value));
    internal static readonly ValueCodec<int> Int32 = new(
        static reader => reader.ReadInt32(),
        static (writer, value) => writer.WriteInt32(value));
    internal static readonly ValueCodec<double> Double = new(
        static reader => reader.ReadDouble(),
        static (writer, value) => writer.WriteDouble(value));
    internal static readonly ValueCodec<QString> QString = new(
        static reader => ReadQString(reader),
        static (writer, value) => WriteQString(writer, value));
    internal static readonly ValueCodec<QDate> QDate = new(
        static reader => new QDate
        {
            Year = reader.ReadInt32(),
            Month = reader.ReadInt32(),
            Day = reader.ReadInt32(),
        },
        static (writer, value) =>
        {
            writer.WriteInt32(value.Year);
            writer.WriteInt32(value.Month);
            writer.WriteInt32(value.Day);
        });

    internal static ValueCodec<QList<T>> QList<T>(ValueCodec<T> element, string fieldName) =>
        new(
            reader =>
            {
                var length = reader.ReadInt32();
                ValidateCount(length, fieldName);
                var items = new List<T>(length);
                for (var index = 0; index < length; index++)
                {
                    items.Add(element.Read(reader));
                }

                return new QList<T> { Length = length, Items = items };
            },
            (writer, value) =>
            {
                writer.WriteInt32(value.Length);
                foreach (var item in value.Items)
                {
                    element.Write(writer, item);
                }
            });

    internal static List<T> ReadCountedList<T>(
        BigEndianBinaryReader reader,
        int count,
        ValueCodec<T> element,
        string fieldName)
    {
        ValidateCount(count, fieldName);
        var items = new List<T>(count);
        for (var index = 0; index < count; index++)
        {
            items.Add(element.Read(reader));
        }

        return items;
    }

    internal static void WriteCountedList<T>(
        BigEndianBinaryWriter writer,
        IEnumerable<T> values,
        ValueCodec<T> element)
    {
        foreach (var value in values)
        {
            element.Write(writer, value);
        }
    }

    internal static void ValidateCount(int count, string fieldName)
    {
        if (count < 0 || count > MaximumCollectionCount)
        {
            throw new InvalidDataException($"Invalid {fieldName} count {count}.");
        }
    }

    private static QString ReadQString(BigEndianBinaryReader reader)
    {
        var byteLength = reader.ReadInt32();
        if (byteLength == -1)
        {
            return new QString { Value = null };
        }

        if (byteLength < 0 || (byteLength & 1) != 0)
        {
            throw new InvalidDataException($"Qt QString has invalid byte length {byteLength}.");
        }

        var characters = new char[byteLength / sizeof(char)];
        for (var index = 0; index < characters.Length; index++)
        {
            characters[index] = (char)reader.ReadUInt16();
        }

        return new QString { Value = new string(characters) };
    }

    private static void WriteQString(BigEndianBinaryWriter writer, QString value)
    {
        if (value.Value is null)
        {
            writer.WriteInt32(-1);
            return;
        }

        writer.WriteInt32(checked(value.Value.Length * sizeof(char)));
        foreach (var character in value.Value)
        {
            writer.WriteUInt16(character);
        }
    }
}
