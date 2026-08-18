using BinarySerialization;
using Shuttle.BinarySerde.Common.QFormat;

namespace Shuttle.Fhm.Serde.Trades;

/// <summary>The declarative <c>trade.dat</c> wire contract.</summary>
public sealed class FhmTradeFileData
{
    [FieldOrder(0)]
    public int VersionTag { get; set; }

    [FieldOrder(1)]
    public int TradeCount { get; set; }

    [FieldOrder(2)]
    [FieldCount(nameof(TradeCount))]
    public List<FhmTradeRecordData> Records { get; set; } = [];
}

/// <summary>One active trade record.</summary>
public sealed class FhmTradeRecordData
{
    [FieldOrder(0)]
    public int Field0 { get; set; }

    [FieldOrder(1)]
    public QDate Date { get; set; } = new();

    [FieldOrder(2)]
    public int Field1 { get; set; }

    [FieldOrder(3)]
    public int Field2 { get; set; }

    [FieldOrder(4)]
    public int Field3 { get; set; }

    [FieldOrder(5)]
    public ushort Field4 { get; set; }

    [FieldOrder(6)]
    public int Field5 { get; set; }

    [FieldOrder(7)]
    public QList<int> AssetList0 { get; set; } = new();

    [FieldOrder(8)]
    public QList<int> AssetList1 { get; set; } = new();

    [FieldOrder(9)]
    public QList<int> AssetList2 { get; set; } = new();

    [FieldOrder(10)]
    public QList<int> AssetList3 { get; set; } = new();

    [FieldOrder(11)]
    public QList<int> AssetList4 { get; set; } = new();

    [FieldOrder(12)]
    public ushort InterleavedUnsignedField0 { get; set; }

    [FieldOrder(13)]
    public QList<int> AssetList5 { get; set; } = new();

    [FieldOrder(14)]
    public ushort InterleavedUnsignedField1 { get; set; }

    [FieldOrder(15)]
    public QList<int> AssetList6 { get; set; } = new();

    [FieldOrder(16)]
    public ushort InterleavedUnsignedField2 { get; set; }

    [FieldOrder(17)]
    public QList<int> AssetList7 { get; set; } = new();

    [FieldOrder(18)]
    public ushort InterleavedUnsignedField3 { get; set; }

    [FieldOrder(19)]
    public ushort Field10 { get; set; }

    [FieldOrder(20)]
    public ushort Field11 { get; set; }

    [FieldOrder(21)]
    public int Field12 { get; set; }

    [FieldOrder(22)]
    public int Field13 { get; set; }

    [FieldOrder(23)]
    public byte Flag { get; set; }

    [FieldOrder(24)]
    public ushort Field14 { get; set; }

    [FieldOrder(25)]
    public int Field15 { get; set; }

    [FieldOrder(26)]
    public int Field16 { get; set; }

    [FieldOrder(27)]
    public QList<FhmIntUInt16PairData> PairList0 { get; set; } = new();

    [FieldOrder(28)]
    public QList<FhmIntUInt16PairData> PairList1 { get; set; } = new();
}

/// <summary>A signed-id/unsigned-value pair in an active trade.</summary>
public sealed class FhmIntUInt16PairData
{
    [FieldOrder(0)]
    public int Id { get; set; }

    [FieldOrder(1)]
    public ushort Value { get; set; }
}

/// <summary>Serializes complete <c>trade.dat</c> streams.</summary>
public static class FhmTradeFileSerializer
{
    /// <summary>Deserializes a complete <c>trade.dat</c> stream.</summary>
    public static FhmTradeFileData Deserialize(Stream stream) =>
        QSerializerFactory.Deserialize<FhmTradeFileData>(stream, "trade.dat");

    /// <summary>Serializes a <c>trade.dat</c> stream.</summary>
    public static void Serialize(Stream stream, FhmTradeFileData value) =>
        QSerializerFactory.Serialize(stream, value);
}
