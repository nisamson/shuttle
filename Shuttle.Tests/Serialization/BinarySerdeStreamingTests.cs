using Shuttle.BinarySerde.Dsl;
using static Shuttle.BinarySerde.Dsl.BinaryCodecs;

namespace Shuttle.Tests.Serialization;

public sealed class BinarySerdeStreamingTests
{
    private const string ContainerName = "streaming test record";

    [Fact]
    public void ReadStreaming_ExposesHeaderBeforeItemsAreEnumerated()
    {
        var itemReads = 0;
        var codec = CreateCodec(() => itemReads++);
        using var source = CreatePayload(3, [11, 22, 33]);

        using var read = codec.ReadStreaming(source, ContainerName);

        Assert.Equal(3, read.Value.Count);
        Assert.Equal(0, itemReads);
    }

    [Fact]
    public void ReadStreaming_ProducesOrderedItemsLazily()
    {
        var itemReads = 0;
        var codec = CreateCodec(() => itemReads++);
        using var source = CreatePayload(3, [11, 22, 33]);
        using var read = codec.ReadStreaming(source, ContainerName);
        using var items = read.Value.Items.GetEnumerator();

        Assert.True(items.MoveNext());
        Assert.Equal(11, items.Current);
        Assert.Equal(1, itemReads);
        Assert.True(items.MoveNext());
        Assert.Equal(22, items.Current);
        Assert.Equal(2, itemReads);
        Assert.True(items.MoveNext());
        Assert.Equal(33, items.Current);
        Assert.Equal(3, itemReads);
        Assert.False(items.MoveNext());
    }

    [Fact]
    public void ReadStreaming_RejectsASecondEnumeration()
    {
        var codec = CreateCodec();
        using var source = CreatePayload(2, [4, 9]);
        using var read = codec.ReadStreaming(source, ContainerName);

        Assert.Equal([4, 9], read.Value.Items);
        var exception = Assert.Throws<InvalidOperationException>(
            () =>
            {
                _ = read.Value.Items.ToArray();
            });

        Assert.Contains("once", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void StreamingReadDisposal_LeavesCallerOwnedSourceOpen()
    {
        var codec = CreateCodec();
        using var source = CreatePayload(1, [42]);
        var read = codec.ReadStreaming(source, ContainerName);

        read.Dispose();

        Assert.True(source.CanRead);
        source.Position = 0;
        Assert.Equal(0, source.ReadByte());
        Assert.Throws<ObjectDisposedException>(
            () =>
            {
                _ = read.Value.Items.GetEnumerator();
            });
    }

    [Fact]
    public void MaterializedRead_HasReusableItemsAndMatchesStreamingWireBytes()
    {
        var codec = CreateCodec();
        var expectedBytes = Write(
            codec,
            new StreamingRecord { Count = 3, Items = Yield(7, 8, 9) });
        using var source = new MemoryStream(expectedBytes);
        using var reader = new BigEndianBinaryReader(source);

        var materialized = codec.Read(reader);
        var firstEnumeration = materialized.Items.ToArray();
        var secondEnumeration = materialized.Items.ToArray();
        var rewrittenBytes = Write(codec, materialized);

        Assert.Equal(3, materialized.Count);
        Assert.Equal([7, 8, 9], firstEnumeration);
        Assert.Equal(firstEnumeration, secondEnumeration);
        Assert.Equal(
            [0, 0, 0, 3, 0, 0, 0, 7, 0, 0, 0, 8, 0, 0, 0, 9],
            expectedBytes);
        Assert.Equal(expectedBytes, rewrittenBytes);
    }

    [Fact]
    public void Write_RejectsDeclaredCountThatDoesNotMatchItems()
    {
        var codec = CreateCodec();

        var tooFew = Assert.Throws<InvalidDataException>(
            () => Write(codec, new StreamingRecord { Count = 2, Items = [1] }));
        var tooMany = Assert.Throws<InvalidDataException>(
            () => Write(codec, new StreamingRecord { Count = 1, Items = [1, 2] }));

        Assert.Contains("count", tooFew.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("count", tooMany.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ReadStreaming_RejectsTrailingBytesAfterFinalItem()
    {
        var codec = CreateCodec();
        using var source = CreatePayload(2, [5, 6], trailingBytes: [0x7F]);
        using var read = codec.ReadStreaming(source, ContainerName);

        var exception = Assert.Throws<InvalidDataException>(
            () =>
            {
                _ = read.Value.Items.ToArray();
            });

        Assert.Contains(ContainerName, exception.Message, StringComparison.Ordinal);
        Assert.Contains("unread bytes", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Build_RejectsAStreamingFieldThatIsNotTerminal()
    {
        var int32 = Int32Codec();
        var builder = Record<StreamingRecord>()
            .Field(
                static value => value.Count,
                static (value, field) => value.Count = field,
                int32)
            .StreamingField(
                static value => value.Items,
                static (value, field) => value.Items = field,
                static value => value.Count,
                int32)
            .Field(
                static value => value.Count,
                static (value, field) => value.Count = field,
                int32);

        var exception = Assert.Throws<InvalidOperationException>(
            () =>
            {
                _ = builder.Build();
            });

        Assert.Contains("terminal", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static RecordCodec<StreamingRecord> CreateCodec(Action? onItemRead = null)
    {
        var itemCodec = new ValueCodec<int>(
            reader =>
            {
                onItemRead?.Invoke();
                return reader.ReadInt32();
            },
            static (writer, value) => writer.WriteInt32(value));

        return Record<StreamingRecord>()
            .Field(
                static value => value.Count,
                static (value, field) => value.Count = field,
                Int32Codec())
            .StreamingField(
                static value => value.Items,
                static (value, field) => value.Items = field,
                static value => value.Count,
                itemCodec)
            .Build();
    }

    private static ValueCodec<int> Int32Codec() =>
        new(
            static reader => reader.ReadInt32(),
            static (writer, value) => writer.WriteInt32(value));

    private static MemoryStream CreatePayload(
        int count,
        IEnumerable<int> items,
        byte[]? trailingBytes = null)
    {
        var stream = new MemoryStream();
        using (var writer = new BigEndianBinaryWriter(stream))
        {
            writer.WriteInt32(count);
            foreach (var item in items)
            {
                writer.WriteInt32(item);
            }

            if (trailingBytes is not null)
            {
                writer.WriteBytes(trailingBytes);
            }

            writer.Flush();
        }

        stream.Position = 0;
        return stream;
    }

    private static byte[] Write(RecordCodec<StreamingRecord> codec, StreamingRecord value)
    {
        using var stream = new MemoryStream();
        using var writer = new BigEndianBinaryWriter(stream);
        codec.Write(writer, value);
        writer.Flush();
        return stream.ToArray();
    }

    private static IEnumerable<int> Yield(params int[] values)
    {
        foreach (var value in values)
        {
            yield return value;
        }
    }

    private sealed record StreamingRecord
    {
        public int Count { get; set; }

        public IEnumerable<int> Items { get; set; } = [];
    }
}
