using Shuttle.BinarySerde.Common.QFormat;

namespace Shuttle.Tests.Fhm;

public sealed class QFormatSerializationTests
{
    [Fact]
    public void Serializer_WritesBigEndianDate()
    {
        var date = new QDate { Year = 2030, Month = 2, Day = 3 };
        using var stream = new MemoryStream();

        QSerializerFactory.Create().Serialize(stream, date);

        Assert.Equal([0, 0, 7, 0xEE, 0, 0, 0, 2, 0, 0, 0, 3], stream.ToArray());
    }

    [Fact]
    public void Serializer_RoundTripsCountPrefixedList()
    {
        var source = new QList<ushort> { Length = 2, Items = [0x0102, 0xABCD] };
        using var stream = new MemoryStream();
        var serializer = QSerializerFactory.Create();

        serializer.Serialize(stream, source);
        var bytes = stream.ToArray();
        var roundTripped = serializer.Deserialize<QList<ushort>>(bytes);

        Assert.Equal([0, 0, 0, 2, 1, 2, 0xAB, 0xCD], bytes);
        Assert.Equal(2, roundTripped.Length);
        Assert.Equal(source.Items, roundTripped.Items);
    }

    [Fact]
    public void Serializer_WritesBigEndianQString()
    {
        var value = new QString { Value = "ÅΩ" };
        using var stream = new MemoryStream();

        QSerializerFactory.Create().Serialize(stream, value);

        Assert.Equal([0, 0, 0, 4, 0, 0xC5, 0x03, 0xA9], stream.ToArray());
    }

    [Fact]
    public void Serializer_WritesNullQString()
    {
        var value = new QString();
        using var stream = new MemoryStream();

        QSerializerFactory.Create().Serialize(stream, value);

        Assert.Equal([0xFF, 0xFF, 0xFF, 0xFF], stream.ToArray());
    }

    [Fact]
    public void Serializer_RoundTripsNullAndUnpairedSurrogateQString()
    {
        var serializer = QSerializerFactory.Create();
        var source = new QString { Value = "\uD800\uDC00\uD801" };
        using var stream = new MemoryStream();

        serializer.Serialize(stream, source);
        var nonNull = serializer.Deserialize<QString>(stream.ToArray());
        var nullValue = serializer.Deserialize<QString>([0xFF, 0xFF, 0xFF, 0xFF]);

        Assert.Equal(source.Value, nonNull.Value);
        Assert.Null(nullValue.Value);
    }
}
