using Shuttle.Fhm.SaveData.Binary;

namespace Shuttle.Fhm.SaveData.Files;

/// <summary>Active trade proposals in <c>trade.dat</c>.</summary>
public sealed class FhmTradeFile : IFhmSaveFile
{
    /// <inheritdoc />
    public string RelativePath => "trade.dat";

    /// <summary>Gets or sets the file format version.</summary>
    public int VersionTag { get; set; }

    /// <summary>Gets proposed trades in serialized order.</summary>
    public IList<FhmTradeRecord> Records { get; } = [];

    internal static FhmTradeFile Read(FhmBinaryReader reader)
    {
        var result = new FhmTradeFile { VersionTag = reader.ReadInt32() };
        var count = reader.ReadCount("active trades");
        for (var index = 0; index < count; index++)
        {
            result.Records.Add(FhmTradeRecord.Read(reader));
        }

        reader.EnsureEof("trade.dat");
        return result;
    }

    /// <inheritdoc />
    public void WriteTo(FhmBinaryWriter writer)
    {
        writer.WriteInt32(VersionTag);
        writer.WriteCount(Records.Count, "active trades");
        foreach (var record in Records)
        {
            record.WriteTo(writer);
        }
    }
}

/// <summary>Completed trade records in <c>trade_history.dat</c>.</summary>
public sealed class FhmTradeHistoryFile : IFhmSaveFile
{
    /// <inheritdoc />
    public string RelativePath => "trade_history.dat";

    /// <summary>Gets or sets the file format version.</summary>
    public int VersionTag { get; set; }

    /// <summary>Gets completed trades in serialized order.</summary>
    public IList<FhmTradeHistoryRecord> Records { get; } = [];

    internal static FhmTradeHistoryFile Read(FhmBinaryReader reader)
    {
        var result = new FhmTradeHistoryFile { VersionTag = reader.ReadInt32() };
        var count = reader.ReadCount("trade history");
        for (var index = 0; index < count; index++)
        {
            result.Records.Add(FhmTradeHistoryRecord.Read(reader));
        }

        reader.EnsureEof("trade_history.dat");
        return result;
    }

    /// <inheritdoc />
    public void WriteTo(FhmBinaryWriter writer)
    {
        writer.WriteInt32(VersionTag);
        writer.WriteCount(Records.Count, "trade history");
        foreach (var record in Records)
        {
            record.WriteTo(writer);
        }
    }
}

/// <summary>A proposed trade with eight list-valued asset slots.</summary>
public sealed class FhmTradeRecord
{
    /// <summary>Gets or sets the first unclassified scalar.</summary>
    public int Field0 { get; set; }

    /// <summary>Gets or sets the proposal date.</summary>
    public FhmDate Date { get; set; }

    /// <summary>Gets or sets unclassified signed scalar fields 1 through 3.</summary>
    public int[] Fields1To3 { get; } = new int[3];

    /// <summary>Gets or sets the first unclassified unsigned scalar.</summary>
    public ushort Field4 { get; set; }

    /// <summary>Gets or sets the fifth unclassified signed scalar.</summary>
    public int Field5 { get; set; }

    /// <summary>Gets eight serialized asset lists in wire order.</summary>
    public IList<IList<int>> AssetLists { get; } = CreateIntLists(8);

    /// <summary>Gets four unclassified unsigned scalars interleaved after asset lists A, F, G, and H.</summary>
    public ushort[] InterleavedUnsignedFields { get; } = new ushort[4];

    /// <summary>Gets or sets unclassified unsigned fields 10 and 11.</summary>
    public ushort[] Fields10And11 { get; } = new ushort[2];

    /// <summary>Gets or sets unclassified signed fields 12 and 13.</summary>
    public int[] Fields12And13 { get; } = new int[2];

    /// <summary>Gets or sets the record flag.</summary>
    public byte Flag { get; set; }

    /// <summary>Gets or sets unclassified unsigned field 14.</summary>
    public ushort Field14 { get; set; }

    /// <summary>Gets or sets unclassified signed fields 15 and 16.</summary>
    public int[] Fields15And16 { get; } = new int[2];

    /// <summary>Gets two lists of signed-id/unsigned-value pairs.</summary>
    public IList<IList<FhmIntUInt16Pair>> PairLists { get; } = [[], []];

    internal static FhmTradeRecord Read(FhmBinaryReader reader)
    {
        var result = new FhmTradeRecord
        {
            Field0 = reader.ReadInt32(),
            Date = reader.ReadDate(),
        };
        ReadInts(reader, result.Fields1To3);
        result.Field4 = reader.ReadUInt16();
        result.Field5 = reader.ReadInt32();
        ReadIntList(reader, result.AssetLists[0]);
        ReadIntList(reader, result.AssetLists[1]);
        ReadIntList(reader, result.AssetLists[2]);
        ReadIntList(reader, result.AssetLists[3]);
        ReadIntList(reader, result.AssetLists[4]);
        result.InterleavedUnsignedFields[0] = reader.ReadUInt16();
        ReadIntList(reader, result.AssetLists[5]);
        result.InterleavedUnsignedFields[1] = reader.ReadUInt16();
        ReadIntList(reader, result.AssetLists[6]);
        result.InterleavedUnsignedFields[2] = reader.ReadUInt16();
        ReadIntList(reader, result.AssetLists[7]);
        result.InterleavedUnsignedFields[3] = reader.ReadUInt16();
        ReadUInt16s(reader, result.Fields10And11);
        ReadInts(reader, result.Fields12And13);
        result.Flag = reader.ReadByte();
        result.Field14 = reader.ReadUInt16();
        ReadInts(reader, result.Fields15And16);
        ReadPairs(reader, result.PairLists[0]);
        ReadPairs(reader, result.PairLists[1]);
        return result;
    }

    internal void WriteTo(FhmBinaryWriter writer)
    {
        RequireCount(AssetLists, 8, "trade asset lists");
        RequireLength(Fields1To3, 3, nameof(Fields1To3));
        RequireLength(InterleavedUnsignedFields, 4, nameof(InterleavedUnsignedFields));
        RequireLength(Fields10And11, 2, nameof(Fields10And11));
        RequireLength(Fields12And13, 2, nameof(Fields12And13));
        RequireLength(Fields15And16, 2, nameof(Fields15And16));
        RequireCount(PairLists, 2, "trade pair lists");
        writer.WriteInt32(Field0);
        writer.WriteDate(Date);
        WriteInts(writer, Fields1To3);
        writer.WriteUInt16(Field4);
        writer.WriteInt32(Field5);
        WriteIntList(writer, AssetLists[0]);
        WriteIntList(writer, AssetLists[1]);
        WriteIntList(writer, AssetLists[2]);
        WriteIntList(writer, AssetLists[3]);
        WriteIntList(writer, AssetLists[4]);
        writer.WriteUInt16(InterleavedUnsignedFields[0]);
        WriteIntList(writer, AssetLists[5]);
        writer.WriteUInt16(InterleavedUnsignedFields[1]);
        WriteIntList(writer, AssetLists[6]);
        writer.WriteUInt16(InterleavedUnsignedFields[2]);
        WriteIntList(writer, AssetLists[7]);
        writer.WriteUInt16(InterleavedUnsignedFields[3]);
        WriteUInt16s(writer, Fields10And11);
        WriteInts(writer, Fields12And13);
        writer.WriteByte(Flag);
        writer.WriteUInt16(Field14);
        WriteInts(writer, Fields15And16);
        WritePairs(writer, PairLists[0]);
        WritePairs(writer, PairLists[1]);
    }

    internal static void ReadIntList(FhmBinaryReader reader, IList<int> list)
    {
        var count = reader.ReadCount("trade id list");
        for (var index = 0; index < count; index++)
        {
            list.Add(reader.ReadInt32());
        }
    }

    internal static void WriteIntList(FhmBinaryWriter writer, IList<int> list)
    {
        writer.WriteCount(list.Count, "trade id list");
        foreach (var value in list)
        {
            writer.WriteInt32(value);
        }
    }

    internal static void ReadPairs(FhmBinaryReader reader, IList<FhmIntUInt16Pair> list)
    {
        var count = reader.ReadCount("trade pair list");
        for (var index = 0; index < count; index++)
        {
            list.Add(new FhmIntUInt16Pair(reader.ReadInt32(), reader.ReadUInt16()));
        }
    }

    internal static void WritePairs(FhmBinaryWriter writer, IList<FhmIntUInt16Pair> list)
    {
        writer.WriteCount(list.Count, "trade pair list");
        foreach (var value in list)
        {
            writer.WriteInt32(value.Id);
            writer.WriteUInt16(value.Value);
        }
    }

    internal static void ReadInts(FhmBinaryReader reader, int[] values)
    {
        for (var index = 0; index < values.Length; index++)
        {
            values[index] = reader.ReadInt32();
        }
    }

    internal static void WriteInts(FhmBinaryWriter writer, IEnumerable<int> values)
    {
        foreach (var value in values)
        {
            writer.WriteInt32(value);
        }
    }

    internal static void ReadUInt16s(FhmBinaryReader reader, ushort[] values)
    {
        for (var index = 0; index < values.Length; index++)
        {
            values[index] = reader.ReadUInt16();
        }
    }

    internal static void WriteUInt16s(FhmBinaryWriter writer, IEnumerable<ushort> values)
    {
        foreach (var value in values)
        {
            writer.WriteUInt16(value);
        }
    }

    internal static IList<IList<int>> CreateIntLists(int count)
    {
        var result = new List<IList<int>>(count);
        for (var index = 0; index < count; index++)
        {
            result.Add([]);
        }

        return result;
    }

    internal static void RequireCount<T>(ICollection<T> values, int expected, string name)
    {
        if (values.Count != expected)
        {
            throw new FhmFormatException($"{name} must contain exactly {expected} items.");
        }
    }

    internal static void RequireLength<T>(T[] values, int expected, string name)
    {
        if (values.Length != expected)
        {
            throw new FhmFormatException($"{name} must contain exactly {expected} items.");
        }
    }
}

/// <summary>A completed trade record with ten asset lists and two draft-pick lists.</summary>
public sealed class FhmTradeHistoryRecord
{
    /// <summary>Gets or sets the completed trade identity.</summary>
    public int Id { get; set; }

    /// <summary>Gets or sets the completion date.</summary>
    public FhmDate Date { get; set; }

    /// <summary>Gets or sets participating team identities.</summary>
    public int TeamA { get; set; }

    /// <summary>Gets or sets participating team identities.</summary>
    public int TeamB { get; set; }

    /// <summary>Gets or sets two unclassified signed fields.</summary>
    public int[] Fields2And3 { get; } = new int[2];

    /// <summary>Gets ten asset lists in wire order.</summary>
    public IList<IList<int>> AssetLists { get; } = FhmTradeRecord.CreateIntLists(10);

    /// <summary>Gets two historical draft-pick lists in wire order.</summary>
    public IList<IList<FhmDraftPickDescriptor>> DraftPickLists { get; } = [[], []];

    /// <summary>Gets or sets the trailing record flag.</summary>
    public byte Flag { get; set; }

    internal static FhmTradeHistoryRecord Read(FhmBinaryReader reader)
    {
        var result = new FhmTradeHistoryRecord
        {
            Id = reader.ReadInt32(),
            Date = reader.ReadDate(),
            TeamA = reader.ReadInt32(),
            TeamB = reader.ReadInt32(),
        };
        FhmTradeRecord.ReadInts(reader, result.Fields2And3);
        foreach (var list in result.AssetLists)
        {
            FhmTradeRecord.ReadIntList(reader, list);
        }

        foreach (var list in result.DraftPickLists)
        {
            var count = reader.ReadCount("historical draft picks");
            for (var index = 0; index < count; index++)
            {
                list.Add(new FhmDraftPickDescriptor(
                    reader.ReadUInt16(), reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32(), reader.ReadUInt16(), reader.ReadUInt16()));
            }
        }

        result.Flag = reader.ReadByte();
        return result;
    }

    internal void WriteTo(FhmBinaryWriter writer)
    {
        FhmTradeRecord.RequireLength(Fields2And3, 2, nameof(Fields2And3));
        FhmTradeRecord.RequireCount(AssetLists, 10, "trade-history asset lists");
        FhmTradeRecord.RequireCount(DraftPickLists, 2, "trade-history draft-pick lists");
        writer.WriteInt32(Id);
        writer.WriteDate(Date);
        writer.WriteInt32(TeamA);
        writer.WriteInt32(TeamB);
        FhmTradeRecord.WriteInts(writer, Fields2And3);
        foreach (var list in AssetLists)
        {
            FhmTradeRecord.WriteIntList(writer, list);
        }

        foreach (var list in DraftPickLists)
        {
            writer.WriteCount(list.Count, "historical draft picks");
            foreach (var pick in list)
            {
                writer.WriteUInt16(pick.FieldUInt16);
                writer.WriteInt32(pick.FieldInt0);
                writer.WriteInt32(pick.FieldInt1);
                writer.WriteInt32(pick.FieldInt2);
                writer.WriteUInt16(pick.FieldUInt160);
                writer.WriteUInt16(pick.FieldUInt161);
            }
        }

        writer.WriteByte(Flag);
    }
}

/// <summary>A serialized signed-id/unsigned-value pair.</summary>
public readonly record struct FhmIntUInt16Pair(int Id, ushort Value);

/// <summary>A historical draft-pick descriptor retaining the documented wire fields.</summary>
public readonly record struct FhmDraftPickDescriptor(
    ushort FieldUInt16,
    int FieldInt0,
    int FieldInt1,
    int FieldInt2,
    ushort FieldUInt160,
    ushort FieldUInt161);
