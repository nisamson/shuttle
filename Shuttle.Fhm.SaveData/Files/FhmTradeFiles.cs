using Shuttle.BinarySerde.Common.QFormat;
using Shuttle.Fhm.SaveData.Binary;
using Shuttle.Fhm.Serde.Trades;

namespace Shuttle.Fhm.SaveData.Files;

/// <summary>Active trade proposals in <c>trade.dat</c>.</summary>
public sealed class FhmTradeFile : IFhmSaveFile
{
    private const int MaximumCollectionCount = 10_000_000;

    /// <inheritdoc />
    public string RelativePath => "trade.dat";

    /// <summary>Gets or sets the file format version.</summary>
    public int VersionTag { get; set; }

    /// <summary>Gets proposed trades in serialized order.</summary>
    public IList<FhmTradeRecord> Records { get; } = [];

    internal static FhmTradeFile Read(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        FhmTradeFileData wire;
        try
        {
            wire = FhmTradeFileSerializer.Deserialize(stream);
        }
        catch (InvalidDataException exception)
        {
            throw new FhmFormatException(exception.Message);
        }

        ValidateWire(wire);
        var result = new FhmTradeFile { VersionTag = wire.VersionTag };
        foreach (var record in wire.Records)
        {
            result.Records.Add(FhmTradeRecord.FromWire(record));
        }

        return result;
    }

    public void WriteTo(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ValidateForWrite();
        FhmTradeFileSerializer.Serialize(stream, new FhmTradeFileData
        {
            VersionTag = VersionTag,
            TradeCount = Records.Count,
            Records = Records.Select(FhmTradeRecord.ToWire).ToList(),
        });
    }

    private static void ValidateWire(FhmTradeFileData wire)
    {
        if (wire.TradeCount < 0 || wire.TradeCount > MaximumCollectionCount || wire.Records.Count != wire.TradeCount)
        {
            throw new FhmFormatException($"Invalid active trades count {wire.TradeCount}.");
        }

        foreach (var record in wire.Records)
        {
            FhmTradeRecord.ValidateWire(record);
        }
    }

    private void ValidateForWrite()
    {
        if (Records.Count > MaximumCollectionCount)
        {
            throw new FhmFormatException($"Invalid active trades count {Records.Count}.");
        }

        foreach (var record in Records)
        {
            ArgumentNullException.ThrowIfNull(record);
            record.ValidateForWrite();
        }
    }

    internal static void ValidateListCount(int count, string collectionName)
    {
        if (count < 0 || count > MaximumCollectionCount)
        {
            throw new FhmFormatException($"Invalid {collectionName} count {count}.");
        }
    }

    internal static void ValidateWireList<T>(QList<T> list, string collectionName)
    {
        if (list is null || list.Length < 0 || list.Length > MaximumCollectionCount || list.Items.Count != list.Length)
        {
            throw new FhmFormatException($"Invalid {collectionName} count {list?.Length}.");
        }
    }

    internal static QList<T> ToWireList<T>(IList<T> list, string collectionName)
    {
        ArgumentNullException.ThrowIfNull(list);
        ValidateListCount(list.Count, collectionName);
        return new QList<T> { Length = list.Count, Items = list.ToList() };
    }

    internal static void CopyList<T>(QList<T> source, IList<T> destination)
    {
        foreach (var value in source.Items)
        {
            destination.Add(value);
        }
    }
}

/// <summary>Completed trade records in <c>trade_history.dat</c>.</summary>
public sealed class FhmTradeHistoryFile : IFhmSaveFile
{
    private const int MaximumCollectionCount = 10_000_000;

    /// <inheritdoc />
    public string RelativePath => "trade_history.dat";

    /// <summary>Gets or sets the file format version.</summary>
    public int VersionTag { get; set; }

    /// <summary>Gets completed trades in serialized order.</summary>
    public IList<FhmTradeHistoryRecord> Records { get; } = [];

    internal static FhmTradeHistoryFile Read(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        FhmTradeHistoryFileData wire;
        try
        {
            wire = FhmTradeHistoryFileSerializer.Deserialize(stream);
        }
        catch (InvalidDataException exception)
        {
            throw new FhmFormatException(exception.Message);
        }

        ValidateWire(wire);
        var result = new FhmTradeHistoryFile { VersionTag = wire.VersionTag };
        foreach (var record in wire.Records)
        {
            result.Records.Add(FhmTradeHistoryRecord.FromWire(record));
        }

        return result;
    }

    public void WriteTo(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ValidateForWrite();
        FhmTradeHistoryFileSerializer.Serialize(stream, new FhmTradeHistoryFileData
        {
            VersionTag = VersionTag,
            TradeCount = Records.Count,
            Records = Records.Select(FhmTradeHistoryRecord.ToWire).ToList(),
        });
    }

    private static void ValidateWire(FhmTradeHistoryFileData wire)
    {
        if (wire.TradeCount < 0 || wire.TradeCount > MaximumCollectionCount || wire.Records.Count != wire.TradeCount)
        {
            throw new FhmFormatException($"Invalid trade history count {wire.TradeCount}.");
        }

        foreach (var record in wire.Records)
        {
            FhmTradeHistoryRecord.ValidateWire(record);
        }
    }

    private void ValidateForWrite()
    {
        if (Records.Count > MaximumCollectionCount)
        {
            throw new FhmFormatException($"Invalid trade history count {Records.Count}.");
        }

        foreach (var record in Records)
        {
            ArgumentNullException.ThrowIfNull(record);
            record.ValidateForWrite();
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

    internal static FhmTradeRecord FromWire(FhmTradeRecordData wire)
    {
        var result = new FhmTradeRecord
        {
            Field0 = wire.Field0,
            Date = new(wire.Date.Year, wire.Date.Month, wire.Date.Day),
            Field4 = wire.Field4,
            Field5 = wire.Field5,
            Flag = wire.Flag,
            Field14 = wire.Field14,
        };
        result.Fields1To3[0] = wire.Field1;
        result.Fields1To3[1] = wire.Field2;
        result.Fields1To3[2] = wire.Field3;
        result.InterleavedUnsignedFields[0] = wire.InterleavedUnsignedField0;
        result.InterleavedUnsignedFields[1] = wire.InterleavedUnsignedField1;
        result.InterleavedUnsignedFields[2] = wire.InterleavedUnsignedField2;
        result.InterleavedUnsignedFields[3] = wire.InterleavedUnsignedField3;
        result.Fields10And11[0] = wire.Field10;
        result.Fields10And11[1] = wire.Field11;
        result.Fields12And13[0] = wire.Field12;
        result.Fields12And13[1] = wire.Field13;
        result.Fields15And16[0] = wire.Field15;
        result.Fields15And16[1] = wire.Field16;

        CopyAssetLists(wire, result.AssetLists);
        CopyPairs(wire.PairList0, result.PairLists[0]);
        CopyPairs(wire.PairList1, result.PairLists[1]);
        return result;
    }

    internal static FhmTradeRecordData ToWire(FhmTradeRecord value)
    {
        value.ValidateForWrite();
        return new()
        {
            Field0 = value.Field0,
            Date = ToQDate(value.Date),
            Field1 = value.Fields1To3[0],
            Field2 = value.Fields1To3[1],
            Field3 = value.Fields1To3[2],
            Field4 = value.Field4,
            Field5 = value.Field5,
            AssetList0 = FhmTradeFile.ToWireList(value.AssetLists[0], "trade id list"),
            AssetList1 = FhmTradeFile.ToWireList(value.AssetLists[1], "trade id list"),
            AssetList2 = FhmTradeFile.ToWireList(value.AssetLists[2], "trade id list"),
            AssetList3 = FhmTradeFile.ToWireList(value.AssetLists[3], "trade id list"),
            AssetList4 = FhmTradeFile.ToWireList(value.AssetLists[4], "trade id list"),
            InterleavedUnsignedField0 = value.InterleavedUnsignedFields[0],
            AssetList5 = FhmTradeFile.ToWireList(value.AssetLists[5], "trade id list"),
            InterleavedUnsignedField1 = value.InterleavedUnsignedFields[1],
            AssetList6 = FhmTradeFile.ToWireList(value.AssetLists[6], "trade id list"),
            InterleavedUnsignedField2 = value.InterleavedUnsignedFields[2],
            AssetList7 = FhmTradeFile.ToWireList(value.AssetLists[7], "trade id list"),
            InterleavedUnsignedField3 = value.InterleavedUnsignedFields[3],
            Field10 = value.Fields10And11[0],
            Field11 = value.Fields10And11[1],
            Field12 = value.Fields12And13[0],
            Field13 = value.Fields12And13[1],
            Flag = value.Flag,
            Field14 = value.Field14,
            Field15 = value.Fields15And16[0],
            Field16 = value.Fields15And16[1],
            PairList0 = ToWirePairs(value.PairLists[0]),
            PairList1 = ToWirePairs(value.PairLists[1]),
        };
    }

    internal static void ValidateWire(FhmTradeRecordData wire)
    {
        ValidateAssetLists(GetAssetLists(wire));
        ValidatePairList(wire.PairList0);
        ValidatePairList(wire.PairList1);
    }

    internal void ValidateForWrite()
    {
        RequireCount(AssetLists, 8, "trade asset lists");
        RequireLength(Fields1To3, 3, nameof(Fields1To3));
        RequireLength(InterleavedUnsignedFields, 4, nameof(InterleavedUnsignedFields));
        RequireLength(Fields10And11, 2, nameof(Fields10And11));
        RequireLength(Fields12And13, 2, nameof(Fields12And13));
        RequireLength(Fields15And16, 2, nameof(Fields15And16));
        RequireCount(PairLists, 2, "trade pair lists");
        foreach (var list in AssetLists)
        {
            FhmTradeFile.ToWireList(list, "trade id list");
        }

        foreach (var list in PairLists)
        {
            FhmTradeFile.ValidateListCount(list.Count, "trade pair list");
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

    private static QDate ToQDate(FhmDate date) => new() { Year = date.Year, Month = date.Month, Day = date.Day };

    private static QList<FhmIntUInt16PairData> ToWirePairs(IList<FhmIntUInt16Pair> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        FhmTradeFile.ValidateListCount(values.Count, "trade pair list");
        return new()
        {
            Length = values.Count,
            Items = values.Select(value => new FhmIntUInt16PairData { Id = value.Id, Value = value.Value }).ToList(),
        };
    }

    private static void ValidatePairList(QList<FhmIntUInt16PairData> list) =>
        FhmTradeFile.ValidateWireList(list, "trade pair list");

    private static void ValidateAssetLists(IEnumerable<QList<int>> lists)
    {
        foreach (var list in lists)
        {
            FhmTradeFile.ValidateWireList(list, "trade id list");
        }
    }

    private static QList<int>[] GetAssetLists(FhmTradeRecordData wire) =>
    [
        wire.AssetList0, wire.AssetList1, wire.AssetList2, wire.AssetList3,
        wire.AssetList4, wire.AssetList5, wire.AssetList6, wire.AssetList7,
    ];

    private static void CopyAssetLists(FhmTradeRecordData source, IList<IList<int>> destination)
    {
        var lists = GetAssetLists(source);
        for (var index = 0; index < lists.Length; index++)
        {
            FhmTradeFile.CopyList(lists[index], destination[index]);
        }
    }

    private static void CopyPairs(QList<FhmIntUInt16PairData> source, IList<FhmIntUInt16Pair> destination)
    {
        foreach (var value in source.Items)
        {
            destination.Add(new(value.Id, value.Value));
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

    internal static FhmTradeHistoryRecord FromWire(FhmTradeHistoryRecordData wire)
    {
        var result = new FhmTradeHistoryRecord
        {
            Id = wire.Id,
            Date = new(wire.Date.Year, wire.Date.Month, wire.Date.Day),
            TeamA = wire.TeamA,
            TeamB = wire.TeamB,
            Flag = wire.Flag,
        };
        result.Fields2And3[0] = wire.Field2;
        result.Fields2And3[1] = wire.Field3;
        CopyAssetLists(wire, result.AssetLists);
        CopyDraftPickList(wire.DraftPickList0, result.DraftPickLists[0]);
        CopyDraftPickList(wire.DraftPickList1, result.DraftPickLists[1]);
        return result;
    }

    internal static FhmTradeHistoryRecordData ToWire(FhmTradeHistoryRecord value)
    {
        value.ValidateForWrite();
        return new()
        {
            Id = value.Id,
            Date = new() { Year = value.Date.Year, Month = value.Date.Month, Day = value.Date.Day },
            TeamA = value.TeamA,
            TeamB = value.TeamB,
            Field2 = value.Fields2And3[0],
            Field3 = value.Fields2And3[1],
            AssetList0 = FhmTradeFile.ToWireList(value.AssetLists[0], "trade-history id list"),
            AssetList1 = FhmTradeFile.ToWireList(value.AssetLists[1], "trade-history id list"),
            AssetList2 = FhmTradeFile.ToWireList(value.AssetLists[2], "trade-history id list"),
            AssetList3 = FhmTradeFile.ToWireList(value.AssetLists[3], "trade-history id list"),
            AssetList4 = FhmTradeFile.ToWireList(value.AssetLists[4], "trade-history id list"),
            AssetList5 = FhmTradeFile.ToWireList(value.AssetLists[5], "trade-history id list"),
            AssetList6 = FhmTradeFile.ToWireList(value.AssetLists[6], "trade-history id list"),
            AssetList7 = FhmTradeFile.ToWireList(value.AssetLists[7], "trade-history id list"),
            AssetList8 = FhmTradeFile.ToWireList(value.AssetLists[8], "trade-history id list"),
            AssetList9 = FhmTradeFile.ToWireList(value.AssetLists[9], "trade-history id list"),
            DraftPickList0 = ToWireDraftPickList(value.DraftPickLists[0]),
            DraftPickList1 = ToWireDraftPickList(value.DraftPickLists[1]),
            Flag = value.Flag,
        };
    }

    internal static void ValidateWire(FhmTradeHistoryRecordData wire)
    {
        foreach (var list in GetAssetLists(wire))
        {
            FhmTradeFile.ValidateWireList(list, "trade-history id list");
        }

        ValidateDraftPickList(wire.DraftPickList0);
        ValidateDraftPickList(wire.DraftPickList1);
    }

    internal void ValidateForWrite()
    {
        FhmTradeRecord.RequireLength(Fields2And3, 2, nameof(Fields2And3));
        FhmTradeRecord.RequireCount(AssetLists, 10, "trade-history asset lists");
        FhmTradeRecord.RequireCount(DraftPickLists, 2, "trade-history draft-pick lists");
        foreach (var list in AssetLists)
        {
            FhmTradeFile.ToWireList(list, "trade-history id list");
        }

        foreach (var list in DraftPickLists)
        {
            ArgumentNullException.ThrowIfNull(list);
            FhmTradeFile.ValidateListCount(list.Count, "historical draft picks");
        }
    }

    private static QList<int>[] GetAssetLists(FhmTradeHistoryRecordData wire) =>
    [
        wire.AssetList0, wire.AssetList1, wire.AssetList2, wire.AssetList3, wire.AssetList4,
        wire.AssetList5, wire.AssetList6, wire.AssetList7, wire.AssetList8, wire.AssetList9,
    ];

    private static void CopyAssetLists(FhmTradeHistoryRecordData source, IList<IList<int>> destination)
    {
        var lists = GetAssetLists(source);
        for (var index = 0; index < lists.Length; index++)
        {
            FhmTradeFile.CopyList(lists[index], destination[index]);
        }
    }

    private static QList<FhmDraftPickDescriptorData> ToWireDraftPickList(IList<FhmDraftPickDescriptor> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        FhmTradeFile.ValidateListCount(values.Count, "historical draft picks");
        return new()
        {
            Length = values.Count,
            Items = values.Select(value => new FhmDraftPickDescriptorData
            {
                FieldUInt16 = value.FieldUInt16,
                FieldInt0 = value.FieldInt0,
                FieldInt1 = value.FieldInt1,
                FieldInt2 = value.FieldInt2,
                FieldUInt160 = value.FieldUInt160,
                FieldUInt161 = value.FieldUInt161,
            }).ToList(),
        };
    }

    private static void ValidateDraftPickList(QList<FhmDraftPickDescriptorData> list) =>
        FhmTradeFile.ValidateWireList(list, "historical draft picks");

    private static void CopyDraftPickList(QList<FhmDraftPickDescriptorData> source, IList<FhmDraftPickDescriptor> destination)
    {
        foreach (var pick in source.Items)
        {
            destination.Add(new(
                pick.FieldUInt16,
                pick.FieldInt0,
                pick.FieldInt1,
                pick.FieldInt2,
                pick.FieldUInt160,
                pick.FieldUInt161));
        }
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
