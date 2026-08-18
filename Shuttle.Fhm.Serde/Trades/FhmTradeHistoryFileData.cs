using BinarySerialization;
using Shuttle.BinarySerde.Common.QFormat;

namespace Shuttle.Fhm.Serde.Trades;

/// <summary>The declarative <c>trade_history.dat</c> wire contract.</summary>
public sealed class FhmTradeHistoryFileData
{
    [FieldOrder(0)]
    public int VersionTag { get; set; }

    [FieldOrder(1)]
    public int TradeCount { get; set; }

    [FieldOrder(2)]
    [FieldCount(nameof(TradeCount))]
    public List<FhmTradeHistoryRecordData> Records { get; set; } = [];
}

/// <summary>One completed trade record.</summary>
public sealed class FhmTradeHistoryRecordData
{
    [FieldOrder(0)]
    public int Id { get; set; }

    [FieldOrder(1)]
    public QDate Date { get; set; } = new();

    [FieldOrder(2)]
    public int TeamA { get; set; }

    [FieldOrder(3)]
    public int TeamB { get; set; }

    [FieldOrder(4)]
    public int Field2 { get; set; }

    [FieldOrder(5)]
    public int Field3 { get; set; }

    [FieldOrder(6)]
    public QList<int> AssetList0 { get; set; } = new();

    [FieldOrder(7)]
    public QList<int> AssetList1 { get; set; } = new();

    [FieldOrder(8)]
    public QList<int> AssetList2 { get; set; } = new();

    [FieldOrder(9)]
    public QList<int> AssetList3 { get; set; } = new();

    [FieldOrder(10)]
    public QList<int> AssetList4 { get; set; } = new();

    [FieldOrder(11)]
    public QList<int> AssetList5 { get; set; } = new();

    [FieldOrder(12)]
    public QList<int> AssetList6 { get; set; } = new();

    [FieldOrder(13)]
    public QList<int> AssetList7 { get; set; } = new();

    [FieldOrder(14)]
    public QList<int> AssetList8 { get; set; } = new();

    [FieldOrder(15)]
    public QList<int> AssetList9 { get; set; } = new();

    [FieldOrder(16)]
    public QList<FhmDraftPickDescriptorData> DraftPickList0 { get; set; } = new();

    [FieldOrder(17)]
    public QList<FhmDraftPickDescriptorData> DraftPickList1 { get; set; } = new();

    [FieldOrder(18)]
    public byte Flag { get; set; }
}

/// <summary>One historical draft-pick descriptor.</summary>
public sealed class FhmDraftPickDescriptorData
{
    [FieldOrder(0)]
    public ushort FieldUInt16 { get; set; }

    [FieldOrder(1)]
    public int FieldInt0 { get; set; }

    [FieldOrder(2)]
    public int FieldInt1 { get; set; }

    [FieldOrder(3)]
    public int FieldInt2 { get; set; }

    [FieldOrder(4)]
    public ushort FieldUInt160 { get; set; }

    [FieldOrder(5)]
    public ushort FieldUInt161 { get; set; }
}

/// <summary>Serializes complete <c>trade_history.dat</c> streams.</summary>
public static class FhmTradeHistoryFileSerializer
{
    /// <summary>Deserializes a complete <c>trade_history.dat</c> stream.</summary>
    public static FhmTradeHistoryFileData Deserialize(Stream stream) =>
        QSerializerFactory.Deserialize<FhmTradeHistoryFileData>(stream, "trade_history.dat");

    /// <summary>Serializes a <c>trade_history.dat</c> stream.</summary>
    public static void Serialize(Stream stream, FhmTradeHistoryFileData value) =>
        QSerializerFactory.Serialize(stream, value);
}
