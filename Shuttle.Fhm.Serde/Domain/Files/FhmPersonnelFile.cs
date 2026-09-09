using System.Buffers;
using System.Buffers.Binary;
using Shuttle.Fhm.Serde.Domain.Binary;

namespace Shuttle.Fhm.Serde.Domain.Files;

/// <summary>Job assigned to a personnel record.</summary>
public enum FhmPersonnelJob : byte
{
    GmHeadCoach = 0,
    GeneralManager = 1,
    HeadCoach = 2,
    Owner = 3,
    Scout = 4,
    AssistantCoach = 5,
    Trainer = 7,
}

/// <summary>Offensive coaching preference.</summary>
public enum FhmOffensivePreference : ushort
{
    VeryDefensive = 0,
    Defensive = 1,
    Neutral = 2,
    Offensive = 3,
    VeryOffensive = 4,
}

/// <summary>Physical coaching preference.</summary>
public enum FhmPhysicalPreference : ushort
{
    VeryPhysical = 0,
    Physical = 1,
    Neutral = 2,
    NonPhysical = 3,
    VeryNonPhysical = 4,
}

/// <summary>How aggressively a coach matches lines against the opposition.</summary>
public enum FhmLineMatchingTendency : ushort
{
    VeryPassive = 0,
    Passive = 1,
    Balanced = 2,
    Aggressive = 3,
    VeryAggressive = 4,
}

/// <summary>How aggressively a coach handles goalies.</summary>
public enum FhmGoalieHandlingTendency : ushort
{
    VeryConservative = 0,
    Conservative = 1,
    Balanced = 2,
    Aggressive = 3,
    VeryAggressive = 4,
}

/// <summary>Whether a staff member favors prospects or veterans.</summary>
public enum FhmVeteranPreference : ushort
{
    LovesProspects = 0,
    PrefersProspects = 1,
    Balanced = 2,
    PrefersVeterans = 3,
    LovesVeterans = 4,
}

/// <summary>How willing a staff member is to innovate.</summary>
public enum FhmInnovationTendency : ushort
{
    VeryConservative = 0,
    Conservative = 1,
    Balanced = 2,
    Innovative = 3,
    VeryInnovative = 4,
}

/// <summary>How loyal or ruthless a staff member is.</summary>
public enum FhmLoyaltyTendency : ushort
{
    VeryRuthless = 0,
    Ruthless = 1,
    Balanced = 2,
    Loyal = 3,
    VeryLoyal = 4,
}

/// <summary>The fixed header that precedes records in a <c>personal.dat</c> file.</summary>
public readonly record struct FhmPersonnelFileHeader(int Version, int RecordCount);

/// <summary>A lossless <c>personal.dat</c> model with editable verified fields.</summary>
public sealed class FhmPersonnelFile : IFhmSaveFile
{
    internal const int HeaderLength = 8;
    internal const int CurrentVersion = 35;
    internal const int MaximumRecordCount = FhmPlayerWireMapper.MaximumCollectionCount;
    private readonly byte[] sourceContent;
    private readonly HashSet<int> sourcePersonnelIds;

    private FhmPersonnelFile(byte[] sourceContent, IList<FhmPersonnelRecord> records)
    {
        this.sourceContent = sourceContent;
        sourcePersonnelIds = records.Select(record => record.PersonnelId).ToHashSet();
        Records = records;
    }

    public string RelativePath => "personal.dat";
    public int Version => ReadInt32(sourceContent, 0);
    /// <summary>Gets the serialized personnel-record count.</summary>
    public int RecordCount => ReadInt32(sourceContent, 4);
    /// <summary>Gets the serialized personnel-record count.</summary>
    public int NextPersonnelId => RecordCount;
    public IList<FhmPersonnelRecord> Records { get; }

    public static FhmPersonnelFile Read(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        var content = buffer.ToArray();
        var header = ReadAndValidateHeader(content);

        var boundaryReader = new PersonnelRecordBoundaryReader(content, HeaderLength);
        var records = new List<FhmPersonnelRecord>(header.RecordCount);
        for (var recordOrdinal = 0; recordOrdinal < header.RecordCount; recordOrdinal++)
        {
            var start = checked((int)boundaryReader.Offset);
            boundaryReader.AdvanceRecord(header.Version, recordOrdinal);
            records.Add(CreateRecord(content, start, checked((int)boundaryReader.Offset - start)));
        }

        if (boundaryReader.Offset != content.Length)
        {
            throw new FhmFormatException(
                $"personal.dat contains {content.Length - boundaryReader.Offset} unread bytes after {header.RecordCount} records at offset 0x{boundaryReader.Offset:X}.");
        }

        return new FhmPersonnelFile(content, records);
    }

    internal static FhmPersonnelFileHeader ReadAndValidateHeader(Stream stream)
    {
        var header = new byte[HeaderLength];
        var bytesRead = 0;
        while (bytesRead < header.Length)
        {
            var count = stream.Read(header, bytesRead, header.Length - bytesRead);
            if (count == 0)
            {
                throw new FhmFormatException("personal.dat is shorter than its 8-byte header.");
            }

            bytesRead += count;
        }

        return ReadAndValidateHeader(header);
    }

    private static FhmPersonnelFileHeader ReadAndValidateHeader(byte[] content)
    {
        if (content.Length < HeaderLength)
        {
            throw new FhmFormatException("personal.dat is shorter than its 8-byte header.");
        }

        var version = ReadInt32(content, 0);
        if (version < 0 || version > CurrentVersion)
        {
            throw new FhmFormatException(
                $"Unsupported personal.dat version {version}. Supported versions are 0 through {CurrentVersion}.");
        }

        var recordCount = ReadInt32(content, sizeof(int));
        if (recordCount < 0 || recordCount > MaximumRecordCount)
        {
            throw new FhmFormatException($"Invalid personal.dat record count {recordCount}.");
        }

        return new(version, recordCount);
    }

    internal static FhmPersonnelRecord CreateRecord(byte[] content, int start, int length)
    {
        try
        {
            return new FhmPersonnelRecord(content, start, length);
        }
        catch (ArgumentOutOfRangeException exception)
        {
            throw new FhmFormatException(
                $"Personnel {ReadInt32(content, start + FhmPersonnelRecord.PersonnelIdOffset)} at offset 0x{start:X} contains an unsupported documented value: {exception.Message}");
        }
    }

    public void WriteTo(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        if (!Records.Select(record => record.PersonnelId).ToHashSet().SetEquals(sourcePersonnelIds))
        {
            throw new FhmFormatException("personal.dat personnel identities cannot be inserted, deleted, or replaced.");
        }

        var output = sourceContent.ToArray();
        foreach (var record in Records)
        {
            record.WriteKnownFields(output);
        }

        stream.Write(output);
    }

    internal sealed class PersonnelRecordBoundaryReader : IDisposable
    {
        private readonly byte[]? content;
        private readonly Stream? source;
        private readonly MemoryStream? recordContent;
        private int recordOrdinal;

        public PersonnelRecordBoundaryReader(byte[] content, int offset)
        {
            this.content = content;
            Offset = offset;
        }

        public PersonnelRecordBoundaryReader(Stream source, long offset)
        {
            ArgumentNullException.ThrowIfNull(source);
            this.source = source;
            recordContent = new MemoryStream();
            Offset = offset;
        }

        public long Offset { get; private set; }

        public byte[] GetRecordBytes() =>
            recordContent?.ToArray() ??
            throw new InvalidOperationException("Record bytes are available only when reading from a stream.");

        public void Dispose() => recordContent?.Dispose();

        public void AdvanceRecord(int version, int ordinal)
        {
            recordOrdinal = ordinal;
            recordContent?.SetLength(0);

            ReadInt32("first name ID");
            ReadInt32("surname ID");
            ReadInt32("nickname ID");
            ReadInt32("birth year");
            ReadInt32("birth month");
            ReadInt32("birth day");
            ReadUInt16("nationality");
            ReadUInt16("unknown field at native +0x6A");
            ReadInt32("birth city ID");
            ReadInt32("native +0x74");
            ReadInt32("team record index");
            ReadInt32("native +0x80");
            ReadInt32("personnel ID");
            ReadInt32("native +0x5C");
            ReadQString("native +0x260");
            ReadInt32("native +0x268");
            ReadBoolean("native +0x26C");
            ReadUInt16("native +0x26E");
            ReadInt32("native +0x88");
            ReadBoolean("native +0x270");
            ReadBoolean("native +0x271");
            ReadPersonnelReferenceList("native +0x118");

            ReadBoolean("native +0x1F4");
            ReadUInt16("native +0x1F6");
            ReadUInt16("native +0x1FA");
            ReadUInt16("native +0x23C");
            if (version < 4)
            {
                ReadUInt16("legacy version < 4 field after native +0x23C");
            }

            ReadUInt16("native +0x1FC");
            ReadUInt16("native +0x23E");
            ReadUInt16("native +0x202");
            ReadUInt16("native +0x204");
            ReadUInt16("native +0x206");
            ReadUInt16("native +0x208");
            if (version < 4)
            {
                ReadUInt16("legacy version < 4 field after native +0x208");
            }

            ReadUInt16("native +0x218");
            ReadUInt16("native +0x21A");
            if (version < 4)
            {
                ReadUInt16("legacy version < 4 field 1 after native +0x21A");
                ReadUInt16("legacy version < 4 field 2 after native +0x21A");
                ReadUInt16("legacy version < 4 field 3 after native +0x21A");
            }

            ReadUInt16("native +0xCA");
            ReadQDate("native +0xD0");
            ReadUInt16("native +0x15A");
            ReadUInt16("native +0x226");
            ReadInt32("native +0x274");
            ReadInt32("native +0x278");
            ReadSub38List();

            ReadBoolean("native +0x289");
            ReadBoolean("native +0x122");
            ReadBoolean("native +0x123");
            ReadBoolean("native +0x124");
            ReadBoolean("native +0x126");
            ReadBoolean("native +0x128");
            ReadBoolean("native +0x127");
            if (version > 34)
            {
                ReadBoolean("native +0x125");
            }

            ReadInt32("native +0x12C first value");
            ReadInt32("native +0x130");
            ReadBooleans(8, "native +0x140 through +0x147");
            if (version > 22)
            {
                ReadBoolean("native +0x148");
            }

            if (version > 27)
            {
                ReadBoolean("native +0x14A");
            }

            if (version > 24)
            {
                ReadBoolean("native +0x149");
            }

            if (version > 31)
            {
                ReadBoolean("native +0x14B");
            }

            ReadUInt16("native +0x15E");
            ReadUInt16("native +0x168");
            ReadInt32("native +0x178");
            ReadUInt16("native +0x17C");
            ReadUInt16("native +0x17E");
            ReadUInt16("native +0x188");
            ReadInt32("native +0x184");
            ReadBoolean("native +0x180");
            ReadUInt16("native +0x18A");
            ReadFixedList("native +0x190 integer list", sizeof(int));
            ReadInt32("native +0x198");
            ReadInt32("native +0x19C");
            if (version > 23)
            {
                ReadInt32("native +0x1A0");
            }

            ReadUInt16("native +0xC8");
            ReadFixedList("native +0xC0 double list", sizeof(double));
            ReadPersonnelReferenceList("native +0xD8");
            ReadBoolean("native +0x1A4");
            ReadUInt16("native +0x1F8");
            ReadInt32("native +0x1A8");
            ReadSub21List(version);
            ReadBoolean("native +0x1BE");
            ReadQString(version < 30 ? "native +0x1C0 account" : "native +0x1C0 encoded account");
            ReadUInt16("native +0x20A");
            ReadUInt16("native +0x20C");
            ReadUInt16("native +0x20E");
            ReadByte("native +0x3C");
            ReadByte("native +0x48");
            ReadBoolean("native +0x4C");
            ReadByte("native +0x8C");
            ReadUInt16("native +0x1C8");
            ReadUInt16("native +0x1CA");
            ReadPersonnelReferenceList("native +0xE0");
            ReadFixedList("native +0x1D0 ushort list", sizeof(ushort));
            ReadUInt16("native +0x1BC");
            ReadQDate("native +0x150");
            ReadFixedList("native +0x1D8 integer list", sizeof(int));
            ReadUInt16("native +0x1E0");
            ReadInt32("native +0x1E4");
            ReadIdAndBooleanList("native +0xA0", version);

            ReadUInt16("native +0x210");
            if (version > 0)
            {
                ReadBooleans(4, "native +0x14C through +0x14F");
            }

            if (version == 2)
            {
                ReadBoolean("version 2 obsolete field");
            }
            else if (version > 1)
            {
                ReadInt32("native +0x1F0");
            }

            if (version > 3)
            {
                ReadUInt16s(6, "version > 3 fields");
            }

            if (version > 4)
            {
                ReadInt32("native +0x228");
            }

            if (version > 6)
            {
                ReadIdAndBooleanList("native +0xA8", version);
            }

            if (version > 7)
            {
                ReadStringPersonnelReferenceList("native +0x98");
            }

            if (version > 8)
            {
                ReadBoolean("native +0x22C");
                ReadUInt16s(5, "native +0x22E through +0x236");
            }

            if (version > 9)
            {
                ReadCurrentIdAndBooleanList("native +0xB0");
            }

            if (version > 10)
            {
                ReadBoolean("native +0x238");
            }

            if (version > 11)
            {
                ReadUInt16("native +0x23A");
            }

            if (version > 12)
            {
                ReadBoolean("native +0x129");
                ReadInt32("native +0x12C replacement value");
            }

            if (version > 13)
            {
                ReadInt32("native +0x134");
            }

            ReadInt32("native +0x1B4");
            ReadInt32("native +0x1B8");
            ReadInt16("native +0x90");
            ReadInt16("native +0x92");
            ReadInt16("native +0x94");
            ReadFixedList("native +0x28 byte-pair list", 2);
            ReadInt32("native +0x30 first value");
            ReadInt32("native +0x30 second value");
            ReadPersonnelReferenceList("native +0x1E8 mixed reference list");
            ReadInt16s(5, "personality tendencies");

            if (version > 16)
            {
                ReadDouble("native +0x138");
            }

            if (version > 17)
            {
                ReadInt32("native +0x244");
                ReadInt32("native +0x248");
                ReadUInt16s(6, "native +0x24C through +0x15C");
            }

            if (version == 19)
            {
                ReadUInt16s(10, "version 19 obsolete fields");
            }

            if (version > 20)
            {
                ReadInt32("native +0x16C");
                ReadUInt16("native +0x170");
            }

            if (version > 21)
            {
                ReadBoolean("native +0x256");
            }

            if (version > 28)
            {
                ReadInt32("native +0x174");
            }

            if (version > 30)
            {
                ReadBoolean("native +0x257");
            }

            if (version > 32)
            {
                ReadByte("native +0x4D");
                ReadBoolean("native +0x258");
            }

            if (version > 33)
            {
                ReadInt32("native +0xE8");
            }
        }

        private void ReadSub38List()
        {
            var count = ReadCount("native +0x280 record list");
            for (var index = 0; index < count; index++)
            {
                ReadInt32($"native +0x280 item {index} field 1");
                ReadInt32($"native +0x280 item {index} field 2");
                ReadQString($"native +0x280 item {index} text");
                ReadUInt16s(14, $"native +0x280 item {index} ushort fields");
                ReadInt32($"native +0x280 item {index} final field");
            }
        }

        private void ReadSub21List(int version)
        {
            var count = ReadCount("native +0x110 record list");
            for (var index = 0; index < count; index++)
            {
                ReadUInt16s(5, $"native +0x110 item {index} initial ushort fields");
                ReadInt32($"native +0x110 item {index} integer field");
                ReadBooleans(3, $"native +0x110 item {index} boolean fields");
                ReadUInt16($"native +0x110 item {index} ushort field");
                if (version < 5)
                {
                    ReadUInt16($"native +0x110 item {index} legacy version < 5 field");
                }

                ReadUInt16s(7, $"native +0x110 item {index} final ushort fields");
            }
        }

        private void ReadIdAndBooleanList(string field, int version)
        {
            var count = ReadCount($"{field} record list");
            for (var index = 0; index < count; index++)
            {
                if (version < 10)
                {
                    ReadInt32($"{field} item {index} legacy field 1");
                    ReadInt32($"{field} item {index} legacy field 2");
                }
                else
                {
                    ReadIdAndEightBooleans($"{field} item {index}");
                }
            }
        }

        private void ReadCurrentIdAndBooleanList(string field)
        {
            var count = ReadCount($"{field} record list");
            for (var index = 0; index < count; index++)
            {
                ReadIdAndEightBooleans($"{field} item {index}");
            }
        }

        private void ReadIdAndEightBooleans(string field)
        {
            ReadInt32($"{field} ID");
            ReadBooleans(8, $"{field} boolean fields");
        }

        private void ReadStringPersonnelReferenceList(string field)
        {
            var count = ReadCount($"{field} record list");
            for (var index = 0; index < count; index++)
            {
                ReadQString($"{field} item {index} text");
                ReadPersonnelReferenceList($"{field} item {index} personnel references");
            }
        }

        private void ReadPersonnelReferenceList(string field) =>
            ReadFixedList(field, sizeof(int));

        private void ReadFixedList(string field, int itemLength)
        {
            var count = ReadCount(field);
            Skip((long)count * itemLength, $"{field} items");
        }

        private int ReadCount(string field)
        {
            var fieldOffset = Offset;
            var count = ReadInt32(field);
            if (count < 0 || count > MaximumRecordCount)
            {
                throw new FhmFormatException(
                    $"Invalid {field} count {count} in personal.dat record {recordOrdinal + 1} at offset 0x{fieldOffset:X}.");
            }

            return count;
        }

        private void ReadQString(string field)
        {
            var byteLength = ReadUInt32($"{field} byte length");
            if (byteLength != uint.MaxValue)
            {
                Skip(byteLength, $"{field} payload");
            }
        }

        private void ReadQDate(string field) => Skip(sizeof(long), field);
        private void ReadDouble(string field) => Skip(sizeof(double), field);
        private void ReadInt16(string field) => Skip(sizeof(short), field);
        private void ReadUInt16(string field) => Skip(sizeof(ushort), field);
        private void ReadByte(string field) => Skip(sizeof(byte), field);
        private void ReadBoolean(string field) => Skip(sizeof(byte), field);
        private void ReadUInt16s(int count, string field) => Skip((long)count * sizeof(ushort), field);
        private void ReadInt16s(int count, string field) => Skip((long)count * sizeof(short), field);
        private void ReadBooleans(int count, string field) => Skip((long)count * sizeof(byte), field);

        private int ReadInt32(string field)
        {
            Span<byte> bytes = stackalloc byte[sizeof(int)];
            ReadBytes(bytes, field);
            var value = BinaryPrimitives.ReadInt32BigEndian(bytes);
            return value;
        }

        private uint ReadUInt32(string field)
        {
            Span<byte> bytes = stackalloc byte[sizeof(uint)];
            ReadBytes(bytes, field);
            var value = BinaryPrimitives.ReadUInt32BigEndian(bytes);
            return value;
        }

        private void Skip(long length, string field)
        {
            if (length > int.MaxValue)
            {
                throw new FhmFormatException(
                    $"{field} in personal.dat record {recordOrdinal + 1} at offset 0x{Offset:X} is too large ({length} bytes).");
            }

            if (length == 0)
            {
                return;
            }

            if (content is not null)
            {
                EnsureAvailable((int)length, field);
                Offset += (int)length;
                return;
            }

            var buffer = ArrayPool<byte>.Shared.Rent(Math.Min((int)length, 64 * 1024));
            try
            {
                var remaining = (int)length;
                var bytesRead = 0;
                while (remaining > 0)
                {
                    var chunkLength = Math.Min(remaining, buffer.Length);
                    ReadBytes(buffer.AsSpan(0, chunkLength), field, (int)length, bytesRead);
                    remaining -= chunkLength;
                    bytesRead += chunkLength;
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        private void EnsureAvailable(int length, string field)
        {
            if (content is not null && content.Length - Offset < length)
            {
                throw new FhmFormatException(
                    $"Truncated personal.dat record {recordOrdinal + 1} at offset 0x{Offset:X} while reading {field}: requires {length} bytes but only {content.Length - Offset} remain.");
            }
        }

        private void ReadBytes(Span<byte> destination, string field) =>
            ReadBytes(destination, field, destination.Length, 0);

        private void ReadBytes(Span<byte> destination, string field, int fieldLength, int bytesPreviouslyRead)
        {
            if (content is not null)
            {
                EnsureAvailable(destination.Length, field);
                content.AsSpan(checked((int)Offset), destination.Length).CopyTo(destination);
                Offset += destination.Length;
                return;
            }

            var totalRead = 0;
            while (totalRead < destination.Length)
            {
                var count = source!.Read(destination[totalRead..]);
                if (count == 0)
                {
                    throw new FhmFormatException(
                        $"Truncated personal.dat record {recordOrdinal + 1} at offset 0x{Offset:X} while reading {field}: requires {fieldLength} bytes but only {bytesPreviouslyRead + totalRead} remain.");
                }

                recordContent!.Write(destination.Slice(totalRead, count));
                totalRead += count;
            }

            Offset += destination.Length;
        }
    }

    internal static int ReadInt32(byte[] content, int offset) =>
        BinaryPrimitives.ReadInt32BigEndian(content.AsSpan(offset, sizeof(int)));

    internal static ushort ReadUInt16(byte[] content, int offset) =>
        BinaryPrimitives.ReadUInt16BigEndian(content.AsSpan(offset, sizeof(ushort)));

    internal static void WriteInt32(byte[] content, int offset, int value) =>
        BinaryPrimitives.WriteInt32BigEndian(content.AsSpan(offset, sizeof(int)), value);

    internal static void WriteUInt16(byte[] content, int offset, ushort value) =>
        BinaryPrimitives.WriteUInt16BigEndian(content.AsSpan(offset, sizeof(ushort)), value);
}

/// <summary>Streams validated <c>personal.dat</c> records in serialized order.</summary>
/// <remarks>
/// The caller owns the source stream and must keep it open for the entire enumeration. Disposing this reader does
/// not dispose the source stream. A reader may be enumerated exactly once.
/// </remarks>
public sealed class FhmPersonnelFileReader : IEnumerable<FhmPersonnelRecord>, IDisposable
{
    private readonly Stream source;
    private readonly FhmPersonnelFile.PersonnelRecordBoundaryReader boundaryReader;
    private bool enumerationStarted;
    private bool disposed;

    /// <summary>Initializes a reader over a caller-owned <c>personal.dat</c> stream.</summary>
    public FhmPersonnelFileReader(Stream source)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (!source.CanRead)
        {
            throw new ArgumentException("The source stream must be readable.", nameof(source));
        }

        this.source = source;
        Header = FhmPersonnelFile.ReadAndValidateHeader(source);
        boundaryReader = new(source, FhmPersonnelFile.HeaderLength);
    }

    /// <summary>Gets the validated file header.</summary>
    public FhmPersonnelFileHeader Header { get; }

    /// <summary>Gets the validated personnel format version.</summary>
    public int Version => Header.Version;

    /// <summary>Gets the validated number of records available from this reader.</summary>
    public int RecordCount => Header.RecordCount;

    /// <inheritdoc />
    public IEnumerator<FhmPersonnelRecord> GetEnumerator()
    {
        ThrowIfDisposed();
        if (enumerationStarted)
        {
            throw new InvalidOperationException("A FhmPersonnelFileReader can be enumerated only once.");
        }

        enumerationStarted = true;
        return EnumerateRecords().GetEnumerator();
    }

    /// <inheritdoc />
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

    /// <inheritdoc />
    public void Dispose()
    {
        if (!disposed)
        {
            disposed = true;
            boundaryReader.Dispose();
        }
    }

    private IEnumerable<FhmPersonnelRecord> EnumerateRecords()
    {
        for (var ordinal = 0; ordinal < RecordCount; ordinal++)
        {
            boundaryReader.AdvanceRecord(Version, ordinal);
            var content = boundaryReader.GetRecordBytes();
            yield return FhmPersonnelFile.CreateRecord(content, 0, content.Length);
        }

        long unreadByteCount = 0;
        while (source.ReadByte() != -1)
        {
            unreadByteCount++;
        }

        if (unreadByteCount > 0)
        {
            throw new FhmFormatException(
                $"personal.dat contains {unreadByteCount} unread bytes after {RecordCount} records at offset 0x{boundaryReader.Offset:X}.");
        }
    }

    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(disposed, this);
}

/// <summary>One editable personnel record retained inside its original opaque framing.</summary>
public sealed class FhmPersonnelRecord
{
    private const int ReferenceRecordLength = 0x1B6;
    internal const int BirthDateOffset = 0x0C;
    internal const int TeamRecordIndexOffset = 0x28;
    internal const int PersonnelIdOffset = 0x30;
    internal const int JobOffset = 0x42;
    internal const int RetiredOffset = 0xE5;

    private readonly byte[] sourceContent;
    private readonly int sourceOffset;
    private readonly int sourceLength;
    private int? teamRecordIndex;
    private int? negotiating;
    private int? playerManagement;
    private int? coachingDefense;
    private int? coachingForwards;
    private int? coachingGoalies;
    private int? coachingProspects;
    private int? evaluateAbilities;
    private int? evaluatePotential;
    private int? defensiveSkills;
    private int? offensiveSkills;
    private int? physicalTraining;
    private int? tactics;
    private int? discipline;
    private int? selfPreservation;
    private int? motivation;
    private int? ingameTactics;
    private int? trainerSkill;
    private readonly byte retiredRaw;
    private bool retired;

    internal FhmPersonnelRecord(byte[] sourceContent, int sourceOffset, int sourceLength)
    {
        this.sourceContent = sourceContent;
        this.sourceOffset = sourceOffset;
        this.sourceLength = sourceLength;
        FirstNameNameId = ReadInt32(0);
        SurnameNameId = ReadInt32(4);
        NicknameNameId = ToNullableReference(ReadInt32(8));
        BirthDate = new(ReadInt32(0x0C), ReadInt32(0x10), ReadInt32(0x14));
        NationalityId = ReadUInt16(0x18);
        BirthCityId = ReadInt32(0x1C);
        PersonnelId = ReadInt32(PersonnelIdOffset);
        teamRecordIndex = ToNullableReference(ReadInt32(TeamRecordIndexOffset));
        Job = (FhmPersonnelJob)sourceContent[sourceOffset + JobOffset];
        Negotiating = ReadNullableRating(0x4C);
        OffensivePreference = (FhmOffensivePreference)ReadUInt16(0x4E);
        PlayerManagement = ReadNullableRating(0x50);
        PhysicalPreference = (FhmPhysicalPreference)ReadUInt16(0x52);
        CoachingDefense = ReadNullableRating(0x54);
        CoachingForwards = ReadNullableRating(0x56);
        CoachingGoalies = ReadNullableRating(0x58);
        CoachingProspects = ReadNullableRating(0x5A);
        EvaluateAbilities = ReadNullableRating(0x5C);
        EvaluatePotential = ReadNullableRating(0x5E);
        Reputation = ReadUInt16(0x6A);
        Salary = ReadInt32(0x6E);
        ContractLength = sourceContent[sourceOffset + 0x99] is 0 ? null : sourceContent[sourceOffset + 0x99];
        retiredRaw = sourceContent[sourceOffset + RetiredOffset];
        retired = retiredRaw == 1;
        OffensiveSkills = ReadNullableRating(TailOffset(0xF5));
        DefensiveSkills = ReadNullableRating(TailOffset(0xF7));
        BasedInLocationId = ReadUInt16(0xF9);
        PhysicalTraining = ReadPhysicalTraining();
        Tactics = ReadNullableRating(TailOffset(0x123));
        Discipline = ReadNullableRating(TailOffset(0x12D));
        SelfPreservation = ReadNullableRating(TailOffset(0x12F));
        Motivation = ReadNullableRating(TailOffset(0x133));
        IngameTactics = ReadNullableRating(TailOffset(0x135));
        TrainerSkill = ReadNullableRating(TailOffset(0x137));
        LineMatchingTendency = (FhmLineMatchingTendency)ReadUInt16(TailOffset(0x17E));
        GoalieHandlingTendency = (FhmGoalieHandlingTendency)ReadUInt16(TailOffset(0x180));
        VeteranPreference = (FhmVeteranPreference)ReadUInt16(TailOffset(0x182));
        InnovationTendency = (FhmInnovationTendency)ReadUInt16(TailOffset(0x184));
        LoyaltyTendency = (FhmLoyaltyTendency)ReadUInt16(TailOffset(0x186));
    }

    public int PersonnelId { get; }
    public int FirstNameNameId { get; set; }
    public int SurnameNameId { get; set; }
    public int? NicknameNameId { get; set; }
    public FhmDate BirthDate { get; set; }
    public ushort NationalityId { get; set; }
    public int BirthCityId { get; set; }
    public int? TeamRecordIndex
    {
        get => teamRecordIndex;
        set => teamRecordIndex = value is null or >= 0
            ? value
            : throw new ArgumentOutOfRangeException(nameof(value), value, "Team record index cannot be negative.");
    }
    public FhmPersonnelJob Job { get; set; }
    public int? Negotiating { get => negotiating; set => negotiating = ValidateRating(value, nameof(Negotiating)); }
    public FhmOffensivePreference OffensivePreference { get; set; }
    public int? PlayerManagement { get => playerManagement; set => playerManagement = ValidateRating(value, nameof(PlayerManagement)); }
    public FhmPhysicalPreference PhysicalPreference { get; set; }
    public int? CoachingDefense { get => coachingDefense; set => coachingDefense = ValidateRating(value, nameof(CoachingDefense)); }
    public int? CoachingForwards { get => coachingForwards; set => coachingForwards = ValidateRating(value, nameof(CoachingForwards)); }
    public int? CoachingGoalies { get => coachingGoalies; set => coachingGoalies = ValidateRating(value, nameof(CoachingGoalies)); }
    public int? CoachingProspects { get => coachingProspects; set => coachingProspects = ValidateRating(value, nameof(CoachingProspects)); }
    public int? EvaluateAbilities { get => evaluateAbilities; set => evaluateAbilities = ValidateRating(value, nameof(EvaluateAbilities)); }
    public int? EvaluatePotential { get => evaluatePotential; set => evaluatePotential = ValidateRating(value, nameof(EvaluatePotential)); }
    public ushort Reputation { get; set; }
    public int Salary { get; set; }
    public byte? ContractLength { get; set; }
    public bool Retired
    {
        get => retired;
        set => retired = value;
    }
    public int? DefensiveSkills { get => defensiveSkills; set => defensiveSkills = ValidateRating(value, nameof(DefensiveSkills)); }
    public int? OffensiveSkills { get => offensiveSkills; set => offensiveSkills = ValidateRating(value, nameof(OffensiveSkills)); }
    public ushort BasedInLocationId { get; set; }
    public int? PhysicalTraining { get => physicalTraining; set => physicalTraining = ValidateRating(value, nameof(PhysicalTraining)); }
    public int? Tactics { get => tactics; set => tactics = ValidateRating(value, nameof(Tactics)); }
    public int? Discipline { get => discipline; set => discipline = ValidateRating(value, nameof(Discipline)); }
    public int? SelfPreservation { get => selfPreservation; set => selfPreservation = ValidateRating(value, nameof(SelfPreservation)); }
    public int? Motivation { get => motivation; set => motivation = ValidateRating(value, nameof(Motivation)); }
    public int? IngameTactics { get => ingameTactics; set => ingameTactics = ValidateRating(value, nameof(IngameTactics)); }
    public int? TrainerSkill { get => trainerSkill; set => trainerSkill = ValidateRating(value, nameof(TrainerSkill)); }
    public FhmLineMatchingTendency LineMatchingTendency { get; set; }
    public FhmGoalieHandlingTendency GoalieHandlingTendency { get; set; }
    public FhmVeteranPreference VeteranPreference { get; set; }
    public FhmInnovationTendency InnovationTendency { get; set; }
    public FhmLoyaltyTendency LoyaltyTendency { get; set; }

    public byte[] GetSourceBytes() => sourceContent.AsSpan(sourceOffset, sourceLength).ToArray();

    internal void WriteKnownFields(byte[] output)
    {
        WriteInt32(output, 0, FirstNameNameId);
        WriteInt32(output, 4, SurnameNameId);
        WriteInt32(output, 8, NicknameNameId ?? -1);
        WriteInt32(output, 0x0C, BirthDate.Year);
        WriteInt32(output, 0x10, BirthDate.Month);
        WriteInt32(output, 0x14, BirthDate.Day);
        WriteUInt16(output, 0x18, NationalityId);
        WriteInt32(output, 0x1C, BirthCityId);
        WriteInt32(output, TeamRecordIndexOffset, TeamRecordIndex ?? -1);
        output[sourceOffset + JobOffset] = (byte)Job;
        WriteNullableRating(output, 0x4C, Negotiating);
        WriteUInt16(output, 0x4E, (ushort)OffensivePreference);
        WriteNullableRating(output, 0x50, PlayerManagement);
        WriteUInt16(output, 0x52, (ushort)PhysicalPreference);
        WriteNullableRating(output, 0x54, CoachingDefense);
        WriteNullableRating(output, 0x56, CoachingForwards);
        WriteNullableRating(output, 0x58, CoachingGoalies);
        WriteNullableRating(output, 0x5A, CoachingProspects);
        WriteNullableRating(output, 0x5C, EvaluateAbilities);
        WriteNullableRating(output, 0x5E, EvaluatePotential);
        WriteUInt16(output, 0x6A, Reputation);
        WriteInt32(output, 0x6E, Salary);
        output[sourceOffset + 0x99] = ContractLength ?? 0;
        if (Retired != (retiredRaw == 1))
        {
            output[sourceOffset + RetiredOffset] = Retired ? (byte)1 : (byte)0;
        }
        WriteNullableRating(output, TailOffset(0xF5), OffensiveSkills);
        WriteNullableRating(output, TailOffset(0xF7), DefensiveSkills);
        WriteUInt16(output, 0xF9, BasedInLocationId);
        WritePhysicalTraining(output);
        WriteNullableRating(output, TailOffset(0x123), Tactics);
        WriteNullableRating(output, TailOffset(0x12D), Discipline);
        WriteNullableRating(output, TailOffset(0x12F), SelfPreservation);
        WriteNullableRating(output, TailOffset(0x133), Motivation);
        WriteNullableRating(output, TailOffset(0x135), IngameTactics);
        WriteNullableRating(output, TailOffset(0x137), TrainerSkill);
        WriteUInt16(output, TailOffset(0x17E), (ushort)LineMatchingTendency);
        WriteUInt16(output, TailOffset(0x180), (ushort)GoalieHandlingTendency);
        WriteUInt16(output, TailOffset(0x182), (ushort)VeteranPreference);
        WriteUInt16(output, TailOffset(0x184), (ushort)InnovationTendency);
        WriteUInt16(output, TailOffset(0x186), (ushort)LoyaltyTendency);
    }

    private int ReadInt32(int relativeOffset) => FhmPersonnelFile.ReadInt32(sourceContent, sourceOffset + relativeOffset);
    private ushort ReadUInt16(int relativeOffset) => FhmPersonnelFile.ReadUInt16(sourceContent, sourceOffset + relativeOffset);
    private int? ReadNullableRating(int relativeOffset)
    {
        var value = ReadUInt16(relativeOffset);
        return value <= 20 ? value : null;
    }
    private int? ReadNullableByteRating(int relativeOffset)
    {
        var value = sourceContent[sourceOffset + relativeOffset];
        return value <= 20 ? value : null;
    }
    private int? ReadPhysicalTraining()
    {
        var offset = TailOffset(0xFA);
        return offset > 0xFA ? ReadNullableByteRating(offset) : null;
    }
    private void WriteInt32(byte[] output, int relativeOffset, int value) =>
        FhmPersonnelFile.WriteInt32(output, sourceOffset + relativeOffset, value);
    private void WriteUInt16(byte[] output, int relativeOffset, ushort value) =>
        FhmPersonnelFile.WriteUInt16(output, sourceOffset + relativeOffset, value);
    private void WriteNullableRating(byte[] output, int relativeOffset, int? value)
    {
        var sourceValue = ReadUInt16(relativeOffset);
        if (value is null && sourceValue is > 20 and < ushort.MaxValue)
        {
            return;
        }

        WriteUInt16(output, relativeOffset, ToWireRating(value));
    }
    private void WriteNullableByteRating(byte[] output, int relativeOffset, int? value)
    {
        if (value is not null)
        {
            output[sourceOffset + relativeOffset] = checked((byte)value.Value);
        }
    }
    private void WritePhysicalTraining(byte[] output)
    {
        var offset = TailOffset(0xFA);
        if (offset > 0xFA)
        {
            WriteNullableByteRating(output, offset, PhysicalTraining);
        }
    }
    private int TailOffset(int referenceOffset) => sourceLength - (ReferenceRecordLength - referenceOffset);

    private static int? ToNullableReference(int value) => value == -1 ? null : value;
    private static ushort ToWireRating(int? value) => value is null ? ushort.MaxValue : checked((ushort)value.Value);
    private static int? ValidateRating(int? value, string parameterName) =>
        value is null or >= 0 and <= 20
            ? value
            : throw new ArgumentOutOfRangeException(parameterName, value, "Personnel ratings must be between 0 and 20.");
}
