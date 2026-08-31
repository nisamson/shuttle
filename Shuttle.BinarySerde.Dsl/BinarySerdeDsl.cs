using System.Buffers.Binary;

namespace Shuttle.BinarySerde.Dsl;

/// <summary>Reads a value from a binary stream.</summary>
/// <typeparam name="T">The type of value read.</typeparam>
/// <param name="reader">The binary reader to read from.</param>
/// <returns>The deserialized value.</returns>
public delegate T BinaryReadDelegate<out T>(BigEndianBinaryReader reader);

/// <summary>Writes a value to a binary stream.</summary>
/// <typeparam name="T">The type of value written.</typeparam>
/// <param name="writer">The binary writer to write to.</param>
/// <param name="value">The value to serialize.</param>
public delegate void BinaryWriteDelegate<in T>(BigEndianBinaryWriter writer, T value);

/// <summary>Defines bidirectional binary serialization for a value.</summary>
/// <typeparam name="T">The type of value serialized by this codec.</typeparam>
public readonly record struct ValueCodec<T>
{
    /// <summary>Initializes a new instance of the <see cref="ValueCodec{T}"/> struct.</summary>
    /// <param name="read">The operation that deserializes a value.</param>
    /// <param name="write">The operation that serializes a value.</param>
    public ValueCodec(BinaryReadDelegate<T> read, BinaryWriteDelegate<T> write)
    {
        ArgumentNullException.ThrowIfNull(read);
        ArgumentNullException.ThrowIfNull(write);
        Read = read;
        Write = write;
    }

    /// <summary>Gets the operation that deserializes a value.</summary>
    public BinaryReadDelegate<T> Read { get; }

    /// <summary>Gets the operation that serializes a value.</summary>
    public BinaryWriteDelegate<T> Write { get; }
}

/// <summary>Serializes and deserializes a fixed-order record without reflection.</summary>
/// <typeparam name="T">The record type.</typeparam>
public sealed class RecordCodec<T>
    where T : new()
{
    private readonly Action<BigEndianBinaryReader, T>[] readers;
    private readonly Action<BigEndianBinaryWriter, T>[] writers;
    private readonly Action<BigEndianBinaryReader, T>[] streamingHeaderReaders;
    private readonly IStreamingField<T>? streamingField;

    internal RecordCodec(
        Action<BigEndianBinaryReader, T>[] readOperations,
        Action<BigEndianBinaryWriter, T>[] writeOperations,
        Action<BigEndianBinaryReader, T>[] streamingHeaderReadOperations,
        IStreamingField<T>? streamingRecordField)
    {
        readers = readOperations;
        writers = writeOperations;
        streamingHeaderReaders = streamingHeaderReadOperations;
        streamingField = streamingRecordField;
    }

    /// <summary>
    /// Deserializes one record, materializing any terminal streaming collection into a reusable list.
    /// </summary>
    /// <param name="reader">The binary reader to read from.</param>
    /// <returns>The deserialized record.</returns>
    public T Read(BigEndianBinaryReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);
        var result = new T();
        foreach (var read in readers)
        {
            read(reader, result);
        }

        return result;
    }

    /// <summary>
    /// Deserializes a record with its terminal collection left as a lazy, sequential, single-use sequence.
    /// </summary>
    /// <param name="source">
    /// The stream to read. It remains owned by the caller and must remain open until item enumeration
    /// completes or the returned <see cref="StreamingRecordRead{T}"/> is disposed.
    /// </param>
    /// <param name="containerName">The name used in malformed-data exceptions.</param>
    /// <returns>
    /// An owner for the reader and parsed record. Dispose it when item enumeration is finished; doing
    /// so releases the reader's buffer without closing <paramref name="source"/>.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when this codec has no terminal field configured with
    /// <see cref="BinaryRecordCodecBuilder{T}.StreamingField{TItem}"/>.
    /// </exception>
    public StreamingRecordRead<T> ReadStreaming(Stream source, string containerName)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentException.ThrowIfNullOrWhiteSpace(containerName);
        if (streamingField is null)
        {
            throw new InvalidOperationException(
                "Streaming reads require a terminal collection declared with StreamingField.");
        }

        var reader = new BigEndianBinaryReader(source);
        try
        {
            var result = new T();
            foreach (var read in streamingHeaderReaders)
            {
                read(reader, result);
            }

            streamingField.SetLazy(reader, result, containerName);
            return new StreamingRecordRead<T>(reader, result);
        }
        catch
        {
            reader.Dispose();
            throw;
        }
    }

    /// <summary>Serializes one record.</summary>
    /// <param name="writer">The binary writer to write to.</param>
    /// <param name="value">The record to serialize.</param>
    public void Write(BigEndianBinaryWriter writer, T value)
    {
        ArgumentNullException.ThrowIfNull(writer);
        foreach (var write in writers)
        {
            write(writer, value);
        }
    }

    internal bool HasStreamingField => streamingField is not null;
}

/// <summary>Builds a reflection-free, fixed-order <see cref="RecordCodec{T}"/>.</summary>
/// <typeparam name="T">The record type.</typeparam>
public sealed class BinaryRecordCodecBuilder<T>
    where T : new()
{
    private readonly List<Action<BigEndianBinaryReader, T>> readers = [];
    private readonly List<Action<BigEndianBinaryWriter, T>> writers = [];
    private IStreamingField<T>? streamingField;
    private int streamingFieldIndex = -1;

    /// <summary>Adds a field to the record in its serialized order.</summary>
    /// <typeparam name="TValue">The field type.</typeparam>
    /// <param name="get">Gets the field from a record being serialized.</param>
    /// <param name="set">Sets the field on a record being deserialized.</param>
    /// <param name="codec">The field codec.</param>
    /// <returns>This builder.</returns>
    public BinaryRecordCodecBuilder<T> Field<TValue>(
        Func<T, TValue> get,
        Action<T, TValue> set,
        ValueCodec<TValue> codec)
    {
        ArgumentNullException.ThrowIfNull(get);
        ArgumentNullException.ThrowIfNull(set);
        readers.Add((reader, value) => set(value, codec.Read(reader)));
        writers.Add((writer, value) => codec.Write(writer, get(value)));
        return this;
    }

    /// <summary>
    /// Adds the terminal, top-level collection field that can be read lazily from a caller-owned stream.
    /// </summary>
    /// <typeparam name="TItem">The collection element type.</typeparam>
    /// <param name="get">Gets the sequence from a record being serialized.</param>
    /// <param name="set">
    /// Sets the sequence on a record being deserialized. A streaming read sets a single-use sequence;
    /// an ordinary <see cref="RecordCodec{T}.Read"/> sets a reusable list.
    /// </param>
    /// <param name="count">
    /// Gets the already-read and separately serialized item count from the enclosing record. During
    /// writing this declared count must exactly match the number of items yielded by <paramref name="get"/>.
    /// </param>
    /// <param name="elementCodec">The codec for each collection item.</param>
    /// <returns>This builder.</returns>
    /// <remarks>
    /// A streaming field is supported only as the final field in a top-level record. A later field, a
    /// second streaming field, or wrapping this codec with <see cref="BinaryCodecs.Object{T}"/> causes
    /// <see cref="Build"/> (or <c>Object</c>) to throw.
    /// </remarks>
    public BinaryRecordCodecBuilder<T> StreamingField<TItem>(
        Func<T, IEnumerable<TItem>> get,
        Action<T, IEnumerable<TItem>> set,
        Func<T, int> count,
        ValueCodec<TItem> elementCodec)
    {
        ArgumentNullException.ThrowIfNull(get);
        ArgumentNullException.ThrowIfNull(set);
        ArgumentNullException.ThrowIfNull(count);
        if (streamingField is not null)
        {
            throw new InvalidOperationException("A record can contain only one streaming field.");
        }

        var field = new StreamingField<T, TItem>(get, set, count, elementCodec);
        readers.Add(field.ReadMaterialized);
        writers.Add(field.Write);
        streamingField = field;
        streamingFieldIndex = readers.Count - 1;
        return this;
    }

    /// <summary>Creates the configured record codec.</summary>
    /// <returns>A codec whose fields are serialized in the order they were added.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when a streaming field is not the terminal field in the record.
    /// </exception>
    public RecordCodec<T> Build()
    {
        if (streamingFieldIndex >= 0 && streamingFieldIndex != readers.Count - 1)
        {
            throw new InvalidOperationException(
                "A streaming field must be the terminal field in a top-level record.");
        }

        Action<BigEndianBinaryReader, T>[] headerReaders;
        if (streamingFieldIndex < 0)
        {
            headerReaders = [];
        }
        else
        {
            headerReaders = new Action<BigEndianBinaryReader, T>[streamingFieldIndex];
            readers.CopyTo(0, headerReaders, 0, streamingFieldIndex);
        }

        return new RecordCodec<T>([.. readers], [.. writers], headerReaders, streamingField);
    }
}

/// <summary>Creates reusable codecs for records and fixed-size collections.</summary>
public static class BinaryCodecs
{
    /// <summary>Starts building a reflection-free record codec.</summary>
    /// <typeparam name="T">The record type.</typeparam>
    /// <returns>A record codec builder.</returns>
    public static BinaryRecordCodecBuilder<T> Record<T>()
        where T : new() =>
        new();

    /// <summary>Wraps a record codec so it can be used as a field codec.</summary>
    /// <typeparam name="T">The record type.</typeparam>
    /// <param name="codec">The record codec to wrap.</param>
    /// <returns>A value codec for the record.</returns>
    public static ValueCodec<T> Object<T>(RecordCodec<T> codec)
        where T : new()
    {
        ArgumentNullException.ThrowIfNull(codec);
        if (codec.HasStreamingField)
        {
            throw new InvalidOperationException(
                "A codec with a streaming field cannot be used as a nested object.");
        }

        return new ValueCodec<T>(codec.Read, codec.Write);
    }

    /// <summary>Creates a codec for a fixed number of list elements.</summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="length">The number of serialized elements.</param>
    /// <param name="element">The codec for each element.</param>
    /// <returns>A fixed-size list codec.</returns>
    public static ValueCodec<List<T>> FixedList<T>(int length, ValueCodec<T> element) =>
        new(
            reader =>
            {
                var result = new List<T>(length);
                for (var index = 0; index < length; index++)
                {
                    result.Add(element.Read(reader));
                }

                return result;
            },
            (writer, value) =>
            {
                for (var index = 0; index < length; index++)
                {
                    element.Write(writer, value[index]);
                }
            });

    /// <summary>Creates a codec for a fixed-size array.</summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="length">The number of serialized elements.</param>
    /// <param name="element">The codec for each element.</param>
    /// <returns>A fixed-size array codec.</returns>
    public static ValueCodec<T[]> FixedArray<T>(int length, ValueCodec<T> element) =>
        new(
            reader =>
            {
                var result = new T[length];
                for (var index = 0; index < length; index++)
                {
                    result[index] = element.Read(reader);
                }

                return result;
            },
            (writer, value) =>
            {
                for (var index = 0; index < length; index++)
                {
                    element.Write(writer, value[index]);
                }
            });

    /// <summary>Creates a codec for a fixed number of raw bytes.</summary>
    /// <param name="length">The number of bytes to read.</param>
    /// <returns>A fixed-size byte codec.</returns>
    public static ValueCodec<byte[]> FixedBytes(int length) =>
        new(
            reader => reader.ReadBytes(length),
            (writer, value) => writer.WriteBytes(value));
}

internal interface IStreamingField<T>
    where T : new()
{
    void SetLazy(BigEndianBinaryReader reader, T value, string containerName);
}

internal sealed class StreamingField<TRecord, TItem> : IStreamingField<TRecord>
    where TRecord : new()
{
    private readonly Func<TRecord, IEnumerable<TItem>> getItems;
    private readonly Action<TRecord, IEnumerable<TItem>> setItems;
    private readonly Func<TRecord, int> getCount;
    private readonly ValueCodec<TItem> itemCodec;

    public StreamingField(
        Func<TRecord, IEnumerable<TItem>> get,
        Action<TRecord, IEnumerable<TItem>> set,
        Func<TRecord, int> count,
        ValueCodec<TItem> elementCodec)
    {
        getItems = get;
        setItems = set;
        getCount = count;
        itemCodec = elementCodec;
    }

    public void ReadMaterialized(BigEndianBinaryReader reader, TRecord value)
    {
        var itemCount = GetValidatedCount(value);
        var items = new List<TItem>(itemCount);
        for (var index = 0; index < itemCount; index++)
        {
            items.Add(itemCodec.Read(reader));
        }

        setItems(value, items);
    }

    public void SetLazy(BigEndianBinaryReader reader, TRecord value, string containerName)
    {
        var itemCount = GetValidatedCount(value);
        setItems(value, new StreamingEnumerable<TItem>(reader, itemCount, itemCodec.Read, containerName));
    }

    public void Write(BigEndianBinaryWriter writer, TRecord value)
    {
        var expectedCount = GetValidatedCount(value);
        var items = getItems(value);
        ArgumentNullException.ThrowIfNull(items);

        using var enumerator = items.GetEnumerator();
        for (var index = 0; index < expectedCount; index++)
        {
            if (!enumerator.MoveNext())
            {
                throw new InvalidDataException(
                    $"The declared count {expectedCount} does not match the {index} items in the streaming field.");
            }

            itemCodec.Write(writer, enumerator.Current);
        }

        if (enumerator.MoveNext())
        {
            throw new InvalidDataException(
                $"The declared count {expectedCount} does not match the items in the streaming field.");
        }
    }

    private int GetValidatedCount(TRecord value)
    {
        var itemCount = getCount(value);
        if (itemCount < 0)
        {
            throw new InvalidDataException($"A streaming field cannot have a negative count ({itemCount}).");
        }

        return itemCount;
    }
}

internal sealed class StreamingEnumerable<T> : IEnumerable<T>
{
    private readonly BigEndianBinaryReader sourceReader;
    private readonly int itemCount;
    private readonly BinaryReadDelegate<T> readItem;
    private readonly string sourceName;
    private bool enumerated;

    public StreamingEnumerable(
        BigEndianBinaryReader reader,
        int count,
        BinaryReadDelegate<T> read,
        string containerName)
    {
        sourceReader = reader;
        itemCount = count;
        readItem = read;
        sourceName = containerName;
    }

    public IEnumerator<T> GetEnumerator()
    {
        sourceReader.EnsureNotDisposed();
        if (enumerated)
        {
            throw new InvalidOperationException("A streaming sequence can be enumerated only once.");
        }

        enumerated = true;
        return new Enumerator(sourceReader, itemCount, readItem, sourceName);
    }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

    private sealed class Enumerator : IEnumerator<T>
    {
        private readonly BigEndianBinaryReader sourceReader;
        private readonly int itemCount;
        private readonly BinaryReadDelegate<T> readItem;
        private readonly string sourceName;
        private int index;
        private bool completed;

        public Enumerator(
            BigEndianBinaryReader reader,
            int count,
            BinaryReadDelegate<T> read,
            string containerName)
        {
            sourceReader = reader;
            itemCount = count;
            readItem = read;
            sourceName = containerName;
            Current = default!;
        }

        public T Current { get; private set; }

        object? System.Collections.IEnumerator.Current => Current;

        public bool MoveNext()
        {
            sourceReader.EnsureNotDisposed();
            if (index == itemCount)
            {
                if (!completed)
                {
                    sourceReader.EnsureEndOfStream(sourceName);
                    completed = true;
                }

                return false;
            }

            Current = readItem(sourceReader);
            index++;
            return true;
        }

        public void Reset() => throw new NotSupportedException("Streaming enumeration cannot be reset.");

        public void Dispose()
        {
        }
    }
}

/// <summary>
/// Owns a streaming record read and its buffered reader without owning the underlying source stream.
/// </summary>
/// <typeparam name="T">The parsed record type.</typeparam>
/// <remarks>
/// The <see cref="Value"/> header is available immediately. Its streaming collection is lazy and may be
/// enumerated once only. Dispose this owner after enumeration; the caller-owned source stream remains open,
/// while the sequence and reader become unusable.
/// </remarks>
public sealed class StreamingRecordRead<T> : IDisposable
    where T : new()
{
    private readonly BigEndianBinaryReader sourceReader;
    private bool disposed;

    internal StreamingRecordRead(BigEndianBinaryReader reader, T value)
    {
        sourceReader = reader;
        Value = value;
    }

    /// <summary>Gets the parsed record, including its lazy terminal collection.</summary>
    public T Value { get; }

    /// <summary>Releases the buffered reader without closing the caller-owned source stream.</summary>
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        sourceReader.Dispose();
    }
}

/// <summary>Provides buffered big-endian primitive reads from a caller-owned stream.</summary>
public sealed class BigEndianBinaryReader : IDisposable
{
    private const int BufferSize = 65_536;
    private readonly Stream stream;
    private byte[] buffer;
    private int bufferOffset;
    private int bufferLength;
    private long position;
    private bool disposed;

    /// <summary>Initializes a new reader without taking ownership of <paramref name="source"/>.</summary>
    /// <param name="source">The source stream.</param>
    /// <param name="bufferSize">The size of the internal read buffer.</param>
    public BigEndianBinaryReader(Stream source, int bufferSize = BufferSize)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentOutOfRangeException.ThrowIfLessThan(bufferSize, 1);
        stream = source;
        buffer = new byte[bufferSize];
        position = source.CanSeek ? source.Position : 0;
    }

    /// <summary>Gets the offset after the last byte returned by this reader.</summary>
    /// <exception cref="ObjectDisposedException">Thrown after this reader has been disposed.</exception>
    public long Position
    {
        get
        {
            ThrowIfDisposed();
            return position;
        }
    }

    /// <summary>Reads one byte.</summary>
    /// <returns>The byte read.</returns>
    public byte ReadByte()
    {
        ThrowIfDisposed();
        if (bufferOffset == bufferLength)
        {
            FillBuffer();
        }

        position++;
        return buffer[bufferOffset++];
    }

    /// <summary>Reads an unsigned 16-bit integer in big-endian byte order.</summary>
    /// <returns>The integer read.</returns>
    public ushort ReadUInt16()
    {
        Span<byte> bytes = stackalloc byte[sizeof(ushort)];
        ReadExactly(bytes);
        return BinaryPrimitives.ReadUInt16BigEndian(bytes);
    }

    /// <summary>Reads a signed 32-bit integer in big-endian byte order.</summary>
    /// <returns>The integer read.</returns>
    public int ReadInt32()
    {
        Span<byte> bytes = stackalloc byte[sizeof(int)];
        ReadExactly(bytes);
        return BinaryPrimitives.ReadInt32BigEndian(bytes);
    }

    /// <summary>Reads a signed 64-bit integer in big-endian byte order.</summary>
    /// <returns>The integer read.</returns>
    public long ReadInt64()
    {
        Span<byte> bytes = stackalloc byte[sizeof(long)];
        ReadExactly(bytes);
        return BinaryPrimitives.ReadInt64BigEndian(bytes);
    }

    /// <summary>Reads a double-precision floating-point number in big-endian byte order.</summary>
    /// <returns>The number read.</returns>
    public double ReadDouble() => BitConverter.Int64BitsToDouble(ReadInt64());

    /// <summary>Reads an exact number of raw bytes.</summary>
    /// <param name="length">The number of bytes to read.</param>
    /// <returns>The bytes read.</returns>
    public byte[] ReadBytes(int length)
    {
        var result = new byte[length];
        ReadExactly(result);
        return result;
    }

    /// <summary>Peeks at raw bytes without advancing the reader position.</summary>
    /// <param name="length">The number of bytes to inspect.</param>
    /// <param name="relativeOffset">The offset from the current reader position.</param>
    /// <returns>A copy of the requested bytes.</returns>
    public byte[] PeekBytes(int length, int relativeOffset = 0)
    {
        ThrowIfDisposed();
        ArgumentOutOfRangeException.ThrowIfNegative(length);
        ArgumentOutOfRangeException.ThrowIfNegative(relativeOffset);
        var requiredLength = checked(length + relativeOffset);
        if (requiredLength > buffer.Length)
        {
            throw new ArgumentOutOfRangeException(
                nameof(length),
                length,
                "The requested peek exceeds the reader buffer size.");
        }

        EnsureBuffered(requiredLength);
        return buffer.AsSpan(bufferOffset + relativeOffset, length).ToArray();
    }

    /// <summary>Reads all remaining raw bytes from the buffered source.</summary>
    /// <returns>The remaining bytes in their original order.</returns>
    public byte[] ReadToEnd()
    {
        ThrowIfDisposed();
        using var result = new MemoryStream();
        var bufferedLength = bufferLength - bufferOffset;
        if (bufferedLength > 0)
        {
            result.Write(buffer, bufferOffset, bufferedLength);
            bufferOffset = bufferLength;
            position += bufferedLength;
        }

        stream.CopyTo(result);
        position += result.Length - bufferedLength;
        return result.ToArray();
    }

    /// <summary>Verifies that no bytes remain in the stream.</summary>
    /// <param name="containerName">The name of the container being validated.</param>
    /// <exception cref="InvalidDataException">Thrown when unread bytes remain.</exception>
    public void EnsureEndOfStream(string containerName)
    {
        ThrowIfDisposed();
        if (bufferOffset != bufferLength)
        {
            throw new InvalidDataException($"{containerName} contains unread bytes at offset {position}.");
        }

        if (stream.ReadByte() != -1)
        {
            throw new InvalidDataException($"{containerName} contains unread bytes at offset {position}.");
        }
    }

    /// <summary>Releases the internal buffer without closing the caller-owned stream.</summary>
    public void Dispose()
    {
        disposed = true;
        buffer = [];
        bufferOffset = 0;
        bufferLength = 0;
    }

    private void ReadExactly(Span<byte> destination)
    {
        ThrowIfDisposed();
        while (!destination.IsEmpty)
        {
            if (bufferOffset == bufferLength)
            {
                FillBuffer();
            }

            var bytesToCopy = Math.Min(destination.Length, bufferLength - bufferOffset);
            buffer.AsSpan(bufferOffset, bytesToCopy).CopyTo(destination);
            bufferOffset += bytesToCopy;
            position += bytesToCopy;
            destination = destination[bytesToCopy..];
        }
    }

    private void FillBuffer()
    {
        bufferOffset = 0;
        bufferLength = stream.Read(buffer, 0, buffer.Length);
        if (bufferLength == 0)
        {
            throw new InvalidDataException($"Unexpected end of stream at offset {position}.");
        }
    }

    private void EnsureBuffered(int requiredLength)
    {
        var unreadLength = bufferLength - bufferOffset;
        if (unreadLength >= requiredLength)
        {
            return;
        }

        if (unreadLength > 0)
        {
            buffer.AsSpan(bufferOffset, unreadLength).CopyTo(buffer);
        }

        bufferOffset = 0;
        bufferLength = unreadLength;
        while (bufferLength < requiredLength)
        {
            var bytesRead = stream.Read(buffer, bufferLength, buffer.Length - bufferLength);
            if (bytesRead == 0)
            {
                throw new InvalidDataException($"Unexpected end of stream at offset {position}.");
            }

            bufferLength += bytesRead;
        }
    }

    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(disposed, this);

    internal void EnsureNotDisposed() => ThrowIfDisposed();
}

/// <summary>Provides buffered big-endian primitive writes to a caller-owned stream.</summary>
public sealed class BigEndianBinaryWriter : IDisposable
{
    private const int BufferSize = 65_536;
    private readonly Stream stream;
    private byte[] buffer = new byte[BufferSize];
    private int bufferLength;
    private bool disposed;

    /// <summary>Initializes a new writer without taking ownership of <paramref name="destination"/>.</summary>
    /// <param name="destination">The destination stream.</param>
    public BigEndianBinaryWriter(Stream destination)
    {
        ArgumentNullException.ThrowIfNull(destination);
        stream = destination;
    }

    /// <summary>Writes one byte.</summary>
    /// <param name="value">The byte to write.</param>
    public void WriteByte(byte value)
    {
        ThrowIfDisposed();
        if (bufferLength == buffer.Length)
        {
            Flush();
        }

        buffer[bufferLength++] = value;
    }

    /// <summary>Writes an unsigned 16-bit integer in big-endian byte order.</summary>
    /// <param name="value">The integer to write.</param>
    public void WriteUInt16(ushort value)
    {
        Span<byte> bytes = stackalloc byte[sizeof(ushort)];
        BinaryPrimitives.WriteUInt16BigEndian(bytes, value);
        WriteBytes(bytes);
    }

    /// <summary>Writes a signed 32-bit integer in big-endian byte order.</summary>
    /// <param name="value">The integer to write.</param>
    public void WriteInt32(int value)
    {
        Span<byte> bytes = stackalloc byte[sizeof(int)];
        BinaryPrimitives.WriteInt32BigEndian(bytes, value);
        WriteBytes(bytes);
    }

    /// <summary>Writes a signed 64-bit integer in big-endian byte order.</summary>
    /// <param name="value">The integer to write.</param>
    public void WriteInt64(long value)
    {
        Span<byte> bytes = stackalloc byte[sizeof(long)];
        BinaryPrimitives.WriteInt64BigEndian(bytes, value);
        WriteBytes(bytes);
    }

    /// <summary>Writes a double-precision floating-point number in big-endian byte order.</summary>
    /// <param name="value">The number to write.</param>
    public void WriteDouble(double value) => WriteInt64(BitConverter.DoubleToInt64Bits(value));

    /// <summary>Writes raw bytes.</summary>
    /// <param name="value">The bytes to write.</param>
    public void WriteBytes(ReadOnlySpan<byte> value)
    {
        ThrowIfDisposed();
        while (!value.IsEmpty)
        {
            if (bufferLength == buffer.Length)
            {
                Flush();
            }

            var bytesToCopy = Math.Min(value.Length, buffer.Length - bufferLength);
            value[..bytesToCopy].CopyTo(buffer.AsSpan(bufferLength));
            bufferLength += bytesToCopy;
            value = value[bytesToCopy..];
        }
    }

    /// <summary>Flushes buffered bytes to the caller-owned stream.</summary>
    public void Flush()
    {
        ThrowIfDisposed();
        if (bufferLength != 0)
        {
            stream.Write(buffer, 0, bufferLength);
            bufferLength = 0;
        }
    }

    /// <summary>Releases the internal buffer without closing the caller-owned stream.</summary>
    public void Dispose()
    {
        disposed = true;
        buffer = [];
        bufferLength = 0;
    }

    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(disposed, this);
}
