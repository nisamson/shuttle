using Shuttle.Fhm.SaveData.Binary;
using Shuttle.Fhm.SaveData.Model;

namespace Shuttle.Fhm.SaveData.Files;

/// <summary>A fully framed version-58 <c>players.dat</c> player record.</summary>
public sealed class FhmPlayerRecord
{
    /// <summary>Gets or sets the first-name id in <c>names.dat</c>.</summary>
    public int FirstNameId { get; set; } = FhmReferences.EmptyPlayer;

    /// <summary>Gets or sets the surname id in <c>names.dat</c>.</summary>
    public int SurnameId { get; set; } = FhmReferences.EmptyPlayer;

    /// <summary>Gets or sets the common-name id in <c>names.dat</c>.</summary>
    public int CommonNameId { get; set; } = FhmReferences.EmptyPlayer;

    /// <summary>Gets or sets the player's birth date.</summary>
    public FhmDate BirthDate { get; set; }

    /// <summary>Gets the first unknown three-value unsigned vector.</summary>
    public IList<ushort> UnknownU2Values01 { get; } = new ushort[3];

    /// <summary>Gets the first unknown six-value signed vector.</summary>
    public IList<int> UnknownS4Values01 { get; } = new int[6];

    /// <summary>Gets or sets an unknown signed value.</summary>
    public int UnknownS401 { get; set; }

    /// <summary>Gets or sets the cross-file identity used by lineup slots.</summary>
    public FhmPlayerInternalIdentity InternalIdentity { get; set; }

    /// <summary>Gets or sets the first unknown string.</summary>
    public string? UnknownString01 { get; set; }

    /// <summary>Gets or sets the second unknown string.</summary>
    public string? UnknownString02 { get; set; }

    /// <summary>Gets or sets the third unknown string.</summary>
    public string? UnknownString03 { get; set; }

    /// <summary>Gets the second unknown three-value unsigned vector.</summary>
    public IList<ushort> UnknownU2Values02 { get; } = new ushort[3];

    /// <summary>Gets the position-rating vector.</summary>
    public FhmPlayerPositionRatings PositionRatings { get; } = new();

    /// <summary>Gets the third unknown two-value unsigned vector.</summary>
    public IList<ushort> UnknownU2Values03 { get; } = new ushort[2];

    /// <summary>Gets or sets an unknown byte.</summary>
    public byte UnknownU101 { get; set; }

    /// <summary>Gets or sets an unknown signed value.</summary>
    public int UnknownS402 { get; set; }

    /// <summary>Gets or sets an unknown byte.</summary>
    public byte UnknownU102 { get; set; }

    /// <summary>Gets the unknown fixed-24-byte records.</summary>
    public IList<FhmFixed24Record> UnknownRecordList01 { get; } = [];

    /// <summary>Gets the fourth unknown two-value unsigned vector.</summary>
    public IList<ushort> UnknownU2Values04 { get; } = new ushort[2];

    /// <summary>Gets or sets an unknown signed value.</summary>
    public int UnknownS403 { get; set; }

    /// <summary>Gets or sets an unknown unsigned value.</summary>
    public ushort UnknownU201 { get; set; }

    /// <summary>Gets or sets an unknown floating-point value.</summary>
    public double UnknownF801 { get; set; }

    /// <summary>Gets the first unknown two-value byte vector.</summary>
    public IList<byte> UnknownU1Values01 { get; } = new byte[2];

    /// <summary>Gets player contracts in serialized order.</summary>
    public IList<FhmPlayerContract> Contracts { get; } = [];

    /// <summary>Gets or sets an unknown unsigned value.</summary>
    public ushort UnknownU202 { get; set; }

    /// <summary>Gets the second unknown two-value signed vector.</summary>
    public IList<int> UnknownS4Values02 { get; } = new int[2];

    /// <summary>Gets the second unknown two-value byte vector.</summary>
    public IList<byte> UnknownU1Values02 { get; } = new byte[2];

    /// <summary>Gets or sets an unknown unsigned value.</summary>
    public ushort UnknownU203 { get; set; }

    /// <summary>Gets the complete hidden and visible rating vector.</summary>
    public FhmPlayerAttributes RatingAttributes { get; } = new();

    /// <summary>Gets the third unknown three-value signed vector.</summary>
    public IList<int> UnknownS4Values03 { get; } = new int[3];

    /// <summary>Gets the fifth unknown 15-value unsigned vector.</summary>
    public IList<ushort> UnknownU2Values05 { get; } = new ushort[15];

    /// <summary>Gets the first unknown signed list.</summary>
    public IList<int> UnknownS4List01 { get; } = [];

    /// <summary>Gets the sixth unknown three-value unsigned vector.</summary>
    public IList<ushort> UnknownU2Values06 { get; } = new ushort[3];

    /// <summary>Gets the unknown fixed-8-byte records.</summary>
    public IList<FhmFixed8Record> UnknownPairList01 { get; } = [];

    /// <summary>Gets or sets an unknown date.</summary>
    public FhmDate UnknownDate01 { get; set; }

    /// <summary>Gets or sets an unknown signed value.</summary>
    public int UnknownS404 { get; set; }

    /// <summary>Gets the first aggregate-skater statistics family.</summary>
    public IList<FhmAggregateSkaterStats> AggregateSkaterStats01 { get; } = [];

    /// <summary>Gets the first aggregate-goalie statistics family.</summary>
    public IList<FhmAggregateGoalieStats> AggregateGoalieStats01 { get; } = [];

    /// <summary>Gets the second aggregate-skater statistics family.</summary>
    public IList<FhmAggregateSkaterStats> AggregateSkaterStats02 { get; } = [];

    /// <summary>Gets the second aggregate-goalie statistics family.</summary>
    public IList<FhmAggregateGoalieStats> AggregateGoalieStats02 { get; } = [];

    /// <summary>Gets detailed skater game statistics.</summary>
    public IList<FhmDetailedSkaterGameStats> DetailedSkaterGameStats { get; } = [];

    /// <summary>Gets detailed goalie game statistics.</summary>
    public IList<FhmDetailedGoalieGameStats> DetailedGoalieGameStats { get; } = [];

    /// <summary>Gets or sets an unknown signed value.</summary>
    public int UnknownS405 { get; set; }

    /// <summary>Gets or sets an unknown unsigned value.</summary>
    public ushort UnknownU204 { get; set; }

    /// <summary>Gets the third unknown two-value byte vector.</summary>
    public IList<byte> UnknownU1Values03 { get; } = new byte[2];

    /// <summary>Gets or sets an unknown signed value.</summary>
    public int UnknownS406 { get; set; }

    /// <summary>Gets the first unknown two-value floating-point vector.</summary>
    public IList<double> UnknownF8Values01 { get; } = new double[2];

    /// <summary>Gets the fourth unknown three-value signed vector.</summary>
    public IList<int> UnknownS4Values04 { get; } = new int[3];

    /// <summary>Gets the seventh unknown five-value unsigned vector.</summary>
    public IList<ushort> UnknownU2Values07 { get; } = new ushort[5];

    /// <summary>Gets the second unknown signed list.</summary>
    public IList<int> UnknownS4List02 { get; } = [];

    /// <summary>Gets the fourth unknown three-value byte vector.</summary>
    public IList<byte> UnknownU1Values04 { get; } = new byte[3];

    /// <summary>Gets the third unknown signed list.</summary>
    public IList<int> UnknownS4List03 { get; } = [];

    /// <summary>Gets or sets an unknown signed value.</summary>
    public int UnknownS407 { get; set; }

    /// <summary>Gets or sets an unknown floating-point value.</summary>
    public double UnknownF802 { get; set; }

    /// <summary>Gets the second unknown three-value floating-point vector.</summary>
    public IList<double> UnknownF8Values02 { get; } = new double[3];

    /// <summary>Gets the fifth unknown two-value signed vector.</summary>
    public IList<int> UnknownS4Values05 { get; } = new int[2];

    /// <summary>Gets or sets an unknown floating-point value.</summary>
    public double UnknownF803 { get; set; }

    /// <summary>Gets or sets an unknown byte.</summary>
    public byte UnknownU103 { get; set; }

    /// <summary>Gets or sets an unknown signed value.</summary>
    public int UnknownS408 { get; set; }

    /// <summary>Gets or sets an unknown byte.</summary>
    public byte UnknownU104 { get; set; }

    /// <summary>Gets the eighth unknown three-value unsigned vector.</summary>
    public IList<ushort> UnknownU2Values08 { get; } = new ushort[3];

    /// <summary>Gets or sets an unknown signed value.</summary>
    public int UnknownS409 { get; set; }

    /// <summary>Gets the third unknown two-value floating-point vector.</summary>
    public IList<double> UnknownF8Values03 { get; } = new double[2];

    /// <summary>Gets or sets an unknown signed value.</summary>
    public int UnknownS410 { get; set; }

    /// <summary>Gets the fifth unknown four-value byte vector.</summary>
    public IList<byte> UnknownU1Values05 { get; } = new byte[4];

    /// <summary>Gets or sets an unknown unsigned value.</summary>
    public ushort UnknownU205 { get; set; }

    /// <summary>Gets the sixth unknown two-value signed vector.</summary>
    public IList<int> UnknownS4Values06 { get; } = new int[2];

    /// <summary>Gets the sixth unknown five-value byte vector.</summary>
    public IList<byte> UnknownU1Values06 { get; } = new byte[5];

    /// <summary>Gets the ninth unknown two-value unsigned vector.</summary>
    public IList<ushort> UnknownU2Values09 { get; } = new ushort[2];

    /// <summary>Gets the seventh unknown five-value byte vector.</summary>
    public IList<byte> UnknownU1Values07 { get; } = new byte[5];

    /// <summary>Gets the tenth unknown six-value unsigned vector.</summary>
    public IList<ushort> UnknownU2Values10 { get; } = new ushort[6];

    /// <summary>Gets the eighth unknown four-value byte vector.</summary>
    public IList<byte> UnknownU1Values08 { get; } = new byte[4];

    /// <summary>Gets the eleventh unknown two-value unsigned vector.</summary>
    public IList<ushort> UnknownU2Values11 { get; } = new ushort[2];

    /// <summary>Gets the fourth unknown five-value floating-point vector.</summary>
    public IList<double> UnknownF8Values04 { get; } = new double[5];

    /// <summary>Gets or sets an unknown unsigned value.</summary>
    public ushort UnknownU206 { get; set; }

    /// <summary>Gets the first unknown unsigned list.</summary>
    public IList<ushort> UnknownU2List01 { get; } = [];

    /// <summary>Gets the fifth unknown five-value floating-point vector.</summary>
    public IList<double> UnknownF8Values05 { get; } = new double[5];

    /// <summary>Gets the twelfth unknown four-value unsigned vector.</summary>
    public IList<ushort> UnknownU2Values12 { get; } = new ushort[4];

    /// <summary>Gets dated string records.</summary>
    public IList<FhmDatedStringRecord> DatedStringRecords { get; } = [];

    /// <summary>Gets the thirteenth unknown two-value unsigned vector.</summary>
    public IList<ushort> UnknownU2Values13 { get; } = new ushort[2];

    /// <summary>Gets or sets an unknown byte.</summary>
    public byte UnknownU105 { get; set; }

    /// <summary>Gets the fourteenth unknown three-value unsigned vector.</summary>
    public IList<ushort> UnknownU2Values14 { get; } = new ushort[3];

    /// <summary>Gets or sets an unknown byte.</summary>
    public byte UnknownU106 { get; set; }

    /// <summary>Gets or sets an unknown signed value.</summary>
    public int UnknownS411 { get; set; }

    /// <summary>Gets the sixth unknown five-value floating-point vector.</summary>
    public IList<double> UnknownF8Values06 { get; } = new double[5];

    /// <summary>Gets or sets an unknown unsigned value.</summary>
    public ushort UnknownU207 { get; set; }

    /// <summary>Gets or sets an unknown floating-point value.</summary>
    public double UnknownF804 { get; set; }

    /// <summary>Gets the second unknown fixed-8-byte records.</summary>
    public IList<FhmFixed8Record> UnknownRecordList02 { get; } = [];

    /// <summary>Gets or sets an unknown signed value.</summary>
    public int UnknownS412 { get; set; }

    /// <summary>Gets the unknown fixed-23-byte records.</summary>
    public IList<FhmFixed23Record> UnknownRecordList03 { get; } = [];

    /// <summary>Gets the fifteenth unknown two-value unsigned vector.</summary>
    public IList<ushort> UnknownU2Values15 { get; } = new ushort[2];

    /// <summary>Gets or sets an unknown byte.</summary>
    public byte UnknownU107 { get; set; }

    /// <summary>Gets the sixteenth unknown two-value unsigned vector.</summary>
    public IList<ushort> UnknownU2Values16 { get; } = new ushort[2];

    /// <summary>Gets or sets the fourth unknown string.</summary>
    public string? UnknownString04 { get; set; }

    /// <summary>Gets the seventeenth unknown two-value unsigned vector.</summary>
    public IList<ushort> UnknownU2Values17 { get; } = new ushort[2];

    /// <summary>Gets the seventh unknown four-value signed vector.</summary>
    public IList<int> UnknownS4Values07 { get; } = new int[4];

    /// <summary>Gets or sets the optional selected primary tactical role.</summary>
    public FhmPlayerRoleInstance? PrimaryRole { get; set; }

    /// <summary>Gets or sets the optional selected supplementary tactical role.</summary>
    public FhmPlayerRoleInstance? SupplementaryRole { get; set; }

    /// <summary>Gets the eighteenth unknown two-value unsigned vector.</summary>
    public IList<ushort> UnknownU2Values18 { get; } = new ushort[2];

    /// <summary>Gets the ninth unknown two-value byte vector.</summary>
    public IList<byte> UnknownU1Values09 { get; } = new byte[2];

    /// <summary>Gets unknown fixed two-byte records.</summary>
    public IList<FhmFixed2Record> UnknownU1PairList { get; } = [];

    /// <summary>Gets the eighth unknown three-value signed vector.</summary>
    public IList<int> UnknownS4Values08 { get; } = new int[3];

    /// <summary>Gets unknown dated unsigned records.</summary>
    public IList<FhmDatedUshortRecord> UnknownDatedU2Records { get; } = [];

    /// <summary>Gets the seventh unknown three-value floating-point vector.</summary>
    public IList<double> UnknownF8Values07 { get; } = new double[3];

    /// <summary>Gets or sets an unknown unsigned value.</summary>
    public ushort UnknownU208 { get; set; }

    /// <summary>Gets the tenth unknown two-value byte vector.</summary>
    public IList<byte> UnknownU1Values10 { get; } = new byte[2];

    /// <summary>Gets the fourth unknown signed list.</summary>
    public IList<int> UnknownS4List04 { get; } = [];

    /// <summary>Gets the eleventh unknown three-value byte vector.</summary>
    public IList<byte> UnknownU1Values11 { get; } = new byte[3];

    /// <summary>Gets unknown fixed-16-byte records.</summary>
    public IList<FhmFixed16Record> UnknownRecordList04 { get; } = [];

    /// <summary>Gets or sets an unknown byte.</summary>
    public byte UnknownU108 { get; set; }

    /// <summary>Gets or sets an unknown unsigned value.</summary>
    public ushort UnknownU209 { get; set; }

    /// <summary>Gets special-ability ids.</summary>
    public IList<byte> SpecialAbilities { get; } = [];

    /// <summary>Gets or sets an unknown byte.</summary>
    public byte UnknownU109 { get; set; }

    /// <summary>Gets or sets an unknown signed value.</summary>
    public int UnknownS413 { get; set; }

    /// <summary>Gets the fifth unknown signed list.</summary>
    public IList<int> UnknownS4List05 { get; } = [];

    /// <summary>Gets or sets an unknown signed value.</summary>
    public int UnknownS414 { get; set; }

    /// <summary>Gets the twelfth unknown five-value byte vector.</summary>
    public IList<byte> UnknownU1Values12 { get; } = new byte[5];

    /// <summary>Gets the sixth unknown signed list.</summary>
    public IList<int> UnknownS4List06 { get; } = [];

    /// <summary>Gets unknown byte/signed pairs.</summary>
    public IList<FhmByteInt32Pair> UnknownU1S4PairList { get; } = [];

    /// <summary>Gets or sets an unknown byte.</summary>
    public byte UnknownU110 { get; set; }

    internal static FhmPlayerRecord Read(FhmBinaryReader reader)
    {
        var result = new FhmPlayerRecord
        {
            FirstNameId = reader.ReadInt32(),
            SurnameId = reader.ReadInt32(),
            CommonNameId = reader.ReadInt32(),
            BirthDate = reader.ReadDate(),
        };
        FhmPlayerSerialization.ReadFixed(reader, result.UnknownU2Values01);
        FhmPlayerSerialization.ReadFixed(reader, result.UnknownS4Values01);
        result.UnknownS401 = reader.ReadInt32();
        result.InternalIdentity = new FhmPlayerInternalIdentity(reader.ReadInt32());
        result.UnknownString01 = reader.ReadQString();
        result.UnknownString02 = reader.ReadQString();
        result.UnknownString03 = reader.ReadQString();
        FhmPlayerSerialization.ReadFixed(reader, result.UnknownU2Values02);
        result.PositionRatings.ReadFrom(reader);
        FhmPlayerSerialization.ReadFixed(reader, result.UnknownU2Values03);
        result.UnknownU101 = reader.ReadByte();
        result.UnknownS402 = reader.ReadInt32();
        result.UnknownU102 = reader.ReadByte();
        FhmPlayerSerialization.ReadList(reader, result.UnknownRecordList01, "player fixed-24 records", FhmFixed24Record.Read);
        FhmPlayerSerialization.ReadFixed(reader, result.UnknownU2Values04);
        result.UnknownS403 = reader.ReadInt32();
        result.UnknownU201 = reader.ReadUInt16();
        result.UnknownF801 = reader.ReadDouble();
        FhmPlayerSerialization.ReadFixed(reader, result.UnknownU1Values01);
        FhmPlayerSerialization.ReadList(reader, result.Contracts, "player contracts", FhmPlayerContract.Read);
        result.UnknownU202 = reader.ReadUInt16();
        FhmPlayerSerialization.ReadFixed(reader, result.UnknownS4Values02);
        FhmPlayerSerialization.ReadFixed(reader, result.UnknownU1Values02);
        result.UnknownU203 = reader.ReadUInt16();
        result.RatingAttributes.ReadFrom(reader);
        FhmPlayerSerialization.ReadFixed(reader, result.UnknownS4Values03);
        FhmPlayerSerialization.ReadFixed(reader, result.UnknownU2Values05);
        FhmPlayerSerialization.ReadInt32List(reader, result.UnknownS4List01, "player unknown signed list 01");
        FhmPlayerSerialization.ReadFixed(reader, result.UnknownU2Values06);
        FhmPlayerSerialization.ReadList(reader, result.UnknownPairList01, "player fixed-8 records", FhmFixed8Record.Read);
        result.UnknownDate01 = reader.ReadDate();
        result.UnknownS404 = reader.ReadInt32();
        FhmPlayerSerialization.ReadList(reader, result.AggregateSkaterStats01, "aggregate skater statistics 01", FhmAggregateSkaterStats.Read);
        FhmPlayerSerialization.ReadList(reader, result.AggregateGoalieStats01, "aggregate goalie statistics 01", FhmAggregateGoalieStats.Read);
        FhmPlayerSerialization.ReadList(reader, result.AggregateSkaterStats02, "aggregate skater statistics 02", FhmAggregateSkaterStats.Read);
        FhmPlayerSerialization.ReadList(reader, result.AggregateGoalieStats02, "aggregate goalie statistics 02", FhmAggregateGoalieStats.Read);
        FhmPlayerSerialization.ReadList(reader, result.DetailedSkaterGameStats, "detailed skater game statistics", FhmDetailedSkaterGameStats.Read);
        FhmPlayerSerialization.ReadList(reader, result.DetailedGoalieGameStats, "detailed goalie game statistics", FhmDetailedGoalieGameStats.Read);
        result.ReadTrailing(reader);
        return result;
    }

    internal void WriteTo(FhmBinaryWriter writer)
    {
        writer.WriteInt32(FirstNameId);
        writer.WriteInt32(SurnameId);
        writer.WriteInt32(CommonNameId);
        writer.WriteDate(BirthDate);
        FhmPlayerSerialization.WriteFixed(writer, UnknownU2Values01, 3, "UnknownU2Values01");
        FhmPlayerSerialization.WriteFixed(writer, UnknownS4Values01, 6, "UnknownS4Values01");
        writer.WriteInt32(UnknownS401);
        writer.WriteInt32(InternalIdentity.Value);
        writer.WriteQString(UnknownString01);
        writer.WriteQString(UnknownString02);
        writer.WriteQString(UnknownString03);
        FhmPlayerSerialization.WriteFixed(writer, UnknownU2Values02, 3, "UnknownU2Values02");
        PositionRatings.WriteTo(writer);
        FhmPlayerSerialization.WriteFixed(writer, UnknownU2Values03, 2, "UnknownU2Values03");
        writer.WriteByte(UnknownU101);
        writer.WriteInt32(UnknownS402);
        writer.WriteByte(UnknownU102);
        FhmPlayerSerialization.WriteList(writer, UnknownRecordList01, "player fixed-24 records", static (target, value) => value.WriteTo(target));
        FhmPlayerSerialization.WriteFixed(writer, UnknownU2Values04, 2, "UnknownU2Values04");
        writer.WriteInt32(UnknownS403);
        writer.WriteUInt16(UnknownU201);
        writer.WriteDouble(UnknownF801);
        FhmPlayerSerialization.WriteFixed(writer, UnknownU1Values01, 2, "UnknownU1Values01");
        FhmPlayerSerialization.WriteList(writer, Contracts, "player contracts", static (target, value) => value.WriteTo(target));
        writer.WriteUInt16(UnknownU202);
        FhmPlayerSerialization.WriteFixed(writer, UnknownS4Values02, 2, "UnknownS4Values02");
        FhmPlayerSerialization.WriteFixed(writer, UnknownU1Values02, 2, "UnknownU1Values02");
        writer.WriteUInt16(UnknownU203);
        RatingAttributes.WriteTo(writer);
        FhmPlayerSerialization.WriteFixed(writer, UnknownS4Values03, 3, "UnknownS4Values03");
        FhmPlayerSerialization.WriteFixed(writer, UnknownU2Values05, 15, "UnknownU2Values05");
        FhmPlayerSerialization.WriteInt32List(writer, UnknownS4List01, "player unknown signed list 01");
        FhmPlayerSerialization.WriteFixed(writer, UnknownU2Values06, 3, "UnknownU2Values06");
        FhmPlayerSerialization.WriteList(writer, UnknownPairList01, "player fixed-8 records", static (target, value) => value.WriteTo(target));
        writer.WriteDate(UnknownDate01);
        writer.WriteInt32(UnknownS404);
        FhmPlayerSerialization.WriteList(writer, AggregateSkaterStats01, "aggregate skater statistics 01", static (target, value) => value.WriteTo(target));
        FhmPlayerSerialization.WriteList(writer, AggregateGoalieStats01, "aggregate goalie statistics 01", static (target, value) => value.WriteTo(target));
        FhmPlayerSerialization.WriteList(writer, AggregateSkaterStats02, "aggregate skater statistics 02", static (target, value) => value.WriteTo(target));
        FhmPlayerSerialization.WriteList(writer, AggregateGoalieStats02, "aggregate goalie statistics 02", static (target, value) => value.WriteTo(target));
        FhmPlayerSerialization.WriteList(writer, DetailedSkaterGameStats, "detailed skater game statistics", static (target, value) => value.WriteTo(target));
        FhmPlayerSerialization.WriteList(writer, DetailedGoalieGameStats, "detailed goalie game statistics", static (target, value) => value.WriteTo(target));
        WriteTrailing(writer);
    }

    private void ReadTrailing(FhmBinaryReader reader)
    {
        UnknownS405 = reader.ReadInt32();
        UnknownU204 = reader.ReadUInt16();
        FhmPlayerSerialization.ReadFixed(reader, UnknownU1Values03);
        UnknownS406 = reader.ReadInt32();
        FhmPlayerSerialization.ReadFixed(reader, UnknownF8Values01);
        FhmPlayerSerialization.ReadFixed(reader, UnknownS4Values04);
        FhmPlayerSerialization.ReadFixed(reader, UnknownU2Values07);
        FhmPlayerSerialization.ReadInt32List(reader, UnknownS4List02, "player unknown signed list 02");
        FhmPlayerSerialization.ReadFixed(reader, UnknownU1Values04);
        FhmPlayerSerialization.ReadInt32List(reader, UnknownS4List03, "player unknown signed list 03");
        UnknownS407 = reader.ReadInt32();
        UnknownF802 = reader.ReadDouble();
        FhmPlayerSerialization.ReadFixed(reader, UnknownF8Values02);
        FhmPlayerSerialization.ReadFixed(reader, UnknownS4Values05);
        UnknownF803 = reader.ReadDouble();
        UnknownU103 = reader.ReadByte();
        UnknownS408 = reader.ReadInt32();
        UnknownU104 = reader.ReadByte();
        FhmPlayerSerialization.ReadFixed(reader, UnknownU2Values08);
        UnknownS409 = reader.ReadInt32();
        FhmPlayerSerialization.ReadFixed(reader, UnknownF8Values03);
        UnknownS410 = reader.ReadInt32();
        FhmPlayerSerialization.ReadFixed(reader, UnknownU1Values05);
        UnknownU205 = reader.ReadUInt16();
        FhmPlayerSerialization.ReadFixed(reader, UnknownS4Values06);
        FhmPlayerSerialization.ReadFixed(reader, UnknownU1Values06);
        FhmPlayerSerialization.ReadFixed(reader, UnknownU2Values09);
        FhmPlayerSerialization.ReadFixed(reader, UnknownU1Values07);
        FhmPlayerSerialization.ReadFixed(reader, UnknownU2Values10);
        FhmPlayerSerialization.ReadFixed(reader, UnknownU1Values08);
        FhmPlayerSerialization.ReadFixed(reader, UnknownU2Values11);
        FhmPlayerSerialization.ReadFixed(reader, UnknownF8Values04);
        UnknownU206 = reader.ReadUInt16();
        FhmPlayerSerialization.ReadUInt16List(reader, UnknownU2List01, "player unknown unsigned list");
        FhmPlayerSerialization.ReadFixed(reader, UnknownF8Values05);
        FhmPlayerSerialization.ReadFixed(reader, UnknownU2Values12);
        FhmPlayerSerialization.ReadList(reader, DatedStringRecords, "dated player strings", FhmDatedStringRecord.Read);
        FhmPlayerSerialization.ReadFixed(reader, UnknownU2Values13);
        UnknownU105 = reader.ReadByte();
        FhmPlayerSerialization.ReadFixed(reader, UnknownU2Values14);
        UnknownU106 = reader.ReadByte();
        UnknownS411 = reader.ReadInt32();
        FhmPlayerSerialization.ReadFixed(reader, UnknownF8Values06);
        UnknownU207 = reader.ReadUInt16();
        UnknownF804 = reader.ReadDouble();
        FhmPlayerSerialization.ReadList(reader, UnknownRecordList02, "player fixed-8 records", FhmFixed8Record.Read);
        UnknownS412 = reader.ReadInt32();
        FhmPlayerSerialization.ReadList(reader, UnknownRecordList03, "player fixed-23 records", FhmFixed23Record.Read);
        FhmPlayerSerialization.ReadFixed(reader, UnknownU2Values15);
        UnknownU107 = reader.ReadByte();
        FhmPlayerSerialization.ReadFixed(reader, UnknownU2Values16);
        UnknownString04 = reader.ReadQString();
        FhmPlayerSerialization.ReadFixed(reader, UnknownU2Values17);
        FhmPlayerSerialization.ReadFixed(reader, UnknownS4Values07);
        PrimaryRole = FhmPlayerRoleInstance.ReadOptional(reader);
        SupplementaryRole = FhmPlayerRoleInstance.ReadOptional(reader);
        FhmPlayerSerialization.ReadFixed(reader, UnknownU2Values18);
        FhmPlayerSerialization.ReadFixed(reader, UnknownU1Values09);
        FhmPlayerSerialization.ReadList(reader, UnknownU1PairList, "player fixed-2 records", FhmFixed2Record.Read);
        FhmPlayerSerialization.ReadFixed(reader, UnknownS4Values08);
        FhmPlayerSerialization.ReadList(reader, UnknownDatedU2Records, "dated player unsigned records", FhmDatedUshortRecord.Read);
        FhmPlayerSerialization.ReadFixed(reader, UnknownF8Values07);
        UnknownU208 = reader.ReadUInt16();
        FhmPlayerSerialization.ReadFixed(reader, UnknownU1Values10);
        FhmPlayerSerialization.ReadInt32List(reader, UnknownS4List04, "player unknown signed list 04");
        FhmPlayerSerialization.ReadFixed(reader, UnknownU1Values11);
        FhmPlayerSerialization.ReadList(reader, UnknownRecordList04, "player fixed-16 records", FhmFixed16Record.Read);
        UnknownU108 = reader.ReadByte();
        UnknownU209 = reader.ReadUInt16();
        FhmPlayerSerialization.ReadByteList(reader, SpecialAbilities, "player special abilities");
        UnknownU109 = reader.ReadByte();
        UnknownS413 = reader.ReadInt32();
        FhmPlayerSerialization.ReadInt32List(reader, UnknownS4List05, "player unknown signed list 05");
        UnknownS414 = reader.ReadInt32();
        FhmPlayerSerialization.ReadFixed(reader, UnknownU1Values12);
        FhmPlayerSerialization.ReadInt32List(reader, UnknownS4List06, "player unknown signed list 06");
        FhmPlayerSerialization.ReadList(reader, UnknownU1S4PairList, "player byte/signed pairs", FhmByteInt32Pair.Read);
        UnknownU110 = reader.ReadByte();
    }

    private void WriteTrailing(FhmBinaryWriter writer)
    {
        writer.WriteInt32(UnknownS405);
        writer.WriteUInt16(UnknownU204);
        FhmPlayerSerialization.WriteFixed(writer, UnknownU1Values03, 2, "UnknownU1Values03");
        writer.WriteInt32(UnknownS406);
        FhmPlayerSerialization.WriteFixed(writer, UnknownF8Values01, 2, "UnknownF8Values01");
        FhmPlayerSerialization.WriteFixed(writer, UnknownS4Values04, 3, "UnknownS4Values04");
        FhmPlayerSerialization.WriteFixed(writer, UnknownU2Values07, 5, "UnknownU2Values07");
        FhmPlayerSerialization.WriteInt32List(writer, UnknownS4List02, "player unknown signed list 02");
        FhmPlayerSerialization.WriteFixed(writer, UnknownU1Values04, 3, "UnknownU1Values04");
        FhmPlayerSerialization.WriteInt32List(writer, UnknownS4List03, "player unknown signed list 03");
        writer.WriteInt32(UnknownS407);
        writer.WriteDouble(UnknownF802);
        FhmPlayerSerialization.WriteFixed(writer, UnknownF8Values02, 3, "UnknownF8Values02");
        FhmPlayerSerialization.WriteFixed(writer, UnknownS4Values05, 2, "UnknownS4Values05");
        writer.WriteDouble(UnknownF803);
        writer.WriteByte(UnknownU103);
        writer.WriteInt32(UnknownS408);
        writer.WriteByte(UnknownU104);
        FhmPlayerSerialization.WriteFixed(writer, UnknownU2Values08, 3, "UnknownU2Values08");
        writer.WriteInt32(UnknownS409);
        FhmPlayerSerialization.WriteFixed(writer, UnknownF8Values03, 2, "UnknownF8Values03");
        writer.WriteInt32(UnknownS410);
        FhmPlayerSerialization.WriteFixed(writer, UnknownU1Values05, 4, "UnknownU1Values05");
        writer.WriteUInt16(UnknownU205);
        FhmPlayerSerialization.WriteFixed(writer, UnknownS4Values06, 2, "UnknownS4Values06");
        FhmPlayerSerialization.WriteFixed(writer, UnknownU1Values06, 5, "UnknownU1Values06");
        FhmPlayerSerialization.WriteFixed(writer, UnknownU2Values09, 2, "UnknownU2Values09");
        FhmPlayerSerialization.WriteFixed(writer, UnknownU1Values07, 5, "UnknownU1Values07");
        FhmPlayerSerialization.WriteFixed(writer, UnknownU2Values10, 6, "UnknownU2Values10");
        FhmPlayerSerialization.WriteFixed(writer, UnknownU1Values08, 4, "UnknownU1Values08");
        FhmPlayerSerialization.WriteFixed(writer, UnknownU2Values11, 2, "UnknownU2Values11");
        FhmPlayerSerialization.WriteFixed(writer, UnknownF8Values04, 5, "UnknownF8Values04");
        writer.WriteUInt16(UnknownU206);
        FhmPlayerSerialization.WriteUInt16List(writer, UnknownU2List01, "player unknown unsigned list");
        FhmPlayerSerialization.WriteFixed(writer, UnknownF8Values05, 5, "UnknownF8Values05");
        FhmPlayerSerialization.WriteFixed(writer, UnknownU2Values12, 4, "UnknownU2Values12");
        FhmPlayerSerialization.WriteList(writer, DatedStringRecords, "dated player strings", static (target, value) => value.WriteTo(target));
        FhmPlayerSerialization.WriteFixed(writer, UnknownU2Values13, 2, "UnknownU2Values13");
        writer.WriteByte(UnknownU105);
        FhmPlayerSerialization.WriteFixed(writer, UnknownU2Values14, 3, "UnknownU2Values14");
        writer.WriteByte(UnknownU106);
        writer.WriteInt32(UnknownS411);
        FhmPlayerSerialization.WriteFixed(writer, UnknownF8Values06, 5, "UnknownF8Values06");
        writer.WriteUInt16(UnknownU207);
        writer.WriteDouble(UnknownF804);
        FhmPlayerSerialization.WriteList(writer, UnknownRecordList02, "player fixed-8 records", static (target, value) => value.WriteTo(target));
        writer.WriteInt32(UnknownS412);
        FhmPlayerSerialization.WriteList(writer, UnknownRecordList03, "player fixed-23 records", static (target, value) => value.WriteTo(target));
        FhmPlayerSerialization.WriteFixed(writer, UnknownU2Values15, 2, "UnknownU2Values15");
        writer.WriteByte(UnknownU107);
        FhmPlayerSerialization.WriteFixed(writer, UnknownU2Values16, 2, "UnknownU2Values16");
        writer.WriteQString(UnknownString04);
        FhmPlayerSerialization.WriteFixed(writer, UnknownU2Values17, 2, "UnknownU2Values17");
        FhmPlayerSerialization.WriteFixed(writer, UnknownS4Values07, 4, "UnknownS4Values07");
        FhmPlayerRoleInstance.WriteOptional(writer, PrimaryRole);
        FhmPlayerRoleInstance.WriteOptional(writer, SupplementaryRole);
        FhmPlayerSerialization.WriteFixed(writer, UnknownU2Values18, 2, "UnknownU2Values18");
        FhmPlayerSerialization.WriteFixed(writer, UnknownU1Values09, 2, "UnknownU1Values09");
        FhmPlayerSerialization.WriteList(writer, UnknownU1PairList, "player fixed-2 records", static (target, value) => value.WriteTo(target));
        FhmPlayerSerialization.WriteFixed(writer, UnknownS4Values08, 3, "UnknownS4Values08");
        FhmPlayerSerialization.WriteList(writer, UnknownDatedU2Records, "dated player unsigned records", static (target, value) => value.WriteTo(target));
        FhmPlayerSerialization.WriteFixed(writer, UnknownF8Values07, 3, "UnknownF8Values07");
        writer.WriteUInt16(UnknownU208);
        FhmPlayerSerialization.WriteFixed(writer, UnknownU1Values10, 2, "UnknownU1Values10");
        FhmPlayerSerialization.WriteInt32List(writer, UnknownS4List04, "player unknown signed list 04");
        FhmPlayerSerialization.WriteFixed(writer, UnknownU1Values11, 3, "UnknownU1Values11");
        FhmPlayerSerialization.WriteList(writer, UnknownRecordList04, "player fixed-16 records", static (target, value) => value.WriteTo(target));
        writer.WriteByte(UnknownU108);
        writer.WriteUInt16(UnknownU209);
        FhmPlayerSerialization.WriteByteList(writer, SpecialAbilities, "player special abilities");
        writer.WriteByte(UnknownU109);
        writer.WriteInt32(UnknownS413);
        FhmPlayerSerialization.WriteInt32List(writer, UnknownS4List05, "player unknown signed list 05");
        writer.WriteInt32(UnknownS414);
        FhmPlayerSerialization.WriteFixed(writer, UnknownU1Values12, 5, "UnknownU1Values12");
        FhmPlayerSerialization.WriteInt32List(writer, UnknownS4List06, "player unknown signed list 06");
        FhmPlayerSerialization.WriteList(writer, UnknownU1S4PairList, "player byte/signed pairs", static (target, value) => value.WriteTo(target));
        writer.WriteByte(UnknownU110);
    }
}
