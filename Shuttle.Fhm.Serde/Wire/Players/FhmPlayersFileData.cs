using System.Buffers.Binary;
using BinarySerialization;
using Shuttle.BinarySerde.Common.QFormat;

namespace Shuttle.Fhm.Serde.Wire.Players;

/// <summary>The declarative version-58 <c>players.dat</c> wire contract.</summary>
public sealed class FhmPlayersFileData
{
    [FieldOrder(0)] public int FormatVersion { get; set; }
    [FieldOrder(1)] public int PlayerCount { get; set; }
    [FieldOrder(2), FieldCount(nameof(PlayerCount))] public List<FhmPlayerRecordData> Players { get; set; } = [];
}

/// <summary>One complete player record.</summary>
public sealed class FhmPlayerRecordData
{
    [FieldOrder(0)] public int FirstNameId { get; set; }
    [FieldOrder(1)] public int SurnameId { get; set; }
    [FieldOrder(2)] public int CommonNameId { get; set; }
    [FieldOrder(3)] public QDate BirthDate { get; set; } = new();
    [FieldOrder(4), FieldCount(3)] public List<ushort> UnknownU2Values01 { get; set; } = [];
    [FieldOrder(5), FieldCount(6)] public List<int> UnknownS4Values01 { get; set; } = [];
    [FieldOrder(6)] public int UnknownS401 { get; set; }
    [FieldOrder(7)] public int InternalIdentity { get; set; }
    [FieldOrder(8)] public QString UnknownString01 { get; set; } = new();
    [FieldOrder(9)] public QString UnusedString01 { get; set; } = new();
    [FieldOrder(10)] public QString UnusedString02 { get; set; } = new();
    [FieldOrder(11), FieldCount(3)] public List<ushort> UnknownU2Values02 { get; set; } = [];
    [FieldOrder(12)] public FhmPlayerPositionRatingsData PositionRatings { get; set; } = new();
    [FieldOrder(13), FieldCount(2)] public List<ushort> UnknownU2Values03 { get; set; } = [];
    [FieldOrder(14)] public byte UnknownU101 { get; set; }
    [FieldOrder(15)] public int UnknownS402 { get; set; }
    [FieldOrder(16)] public byte UnknownU102 { get; set; }
    [FieldOrder(17)] public QList<FhmFixed24RecordData> UnknownRecordList01 { get; set; } = new();
    [FieldOrder(18), FieldCount(2)] public List<ushort> UnknownU2Values04 { get; set; } = [];
    [FieldOrder(19)] public int UnknownS403 { get; set; }
    [FieldOrder(20)] public ushort UnknownU201 { get; set; }
    [FieldOrder(21)] public double UnknownF801 { get; set; }
    [FieldOrder(22), FieldCount(2)] public List<byte> UnknownU1Values01 { get; set; } = [];
    [FieldOrder(23)] public QList<FhmPlayerContractData> Contracts { get; set; } = new();
    [FieldOrder(24)] public ushort UnknownU202 { get; set; }
    [FieldOrder(25), FieldCount(2)] public List<int> UnknownS4Values02 { get; set; } = [];
    [FieldOrder(26), FieldCount(2)] public List<byte> UnknownU1Values02 { get; set; } = [];
    [FieldOrder(27)] public ushort UnknownU203 { get; set; }
    [FieldOrder(28)] public FhmPlayerAttributesData RatingAttributes { get; set; } = new();
    [FieldOrder(29), FieldCount(3)] public List<int> UnknownS4Values03 { get; set; } = [];
    [FieldOrder(30), FieldCount(15)] public List<ushort> UnknownU2Values05 { get; set; } = [];
    [FieldOrder(31)] public QList<int> UnknownS4List01 { get; set; } = new();
    [FieldOrder(32), FieldCount(3)] public List<ushort> UnknownU2Values06 { get; set; } = [];
    [FieldOrder(33)] public QList<FhmFixed8RecordData> UnknownPairList01 { get; set; } = new();
    [FieldOrder(34)] public QDate UnknownDate01 { get; set; } = new();
    [FieldOrder(35)] public int UnknownS404 { get; set; }
    [FieldOrder(36)] public QList<FhmAggregateSkaterStatsData> AggregateSkaterStats01 { get; set; } = new();
    [FieldOrder(37)] public QList<FhmAggregateGoalieStatsData> AggregateGoalieStats01 { get; set; } = new();
    [FieldOrder(38)] public QList<FhmAggregateSkaterStatsData> AggregateSkaterStats02 { get; set; } = new();
    [FieldOrder(39)] public QList<FhmAggregateGoalieStatsData> AggregateGoalieStats02 { get; set; } = new();
    [FieldOrder(40)] public QList<FhmDetailedSkaterGameStatsData> DetailedSkaterGameStats { get; set; } = new();
    [FieldOrder(41)] public QList<FhmDetailedGoalieGameStatsData> DetailedGoalieGameStats { get; set; } = new();
    [FieldOrder(42)] public int UnknownS405 { get; set; }
    [FieldOrder(43)] public ushort UnknownU204 { get; set; }
    [FieldOrder(44), FieldCount(2)] public List<byte> UnknownU1Values03 { get; set; } = [];
    [FieldOrder(45)] public int UnknownS406 { get; set; }
    [FieldOrder(46), FieldCount(2)] public List<double> UnknownF8Values01 { get; set; } = [];
    [FieldOrder(47), FieldCount(3)] public List<int> UnknownS4Values04 { get; set; } = [];
    [FieldOrder(48), FieldCount(5)] public List<ushort> UnknownU2Values07 { get; set; } = [];
    [FieldOrder(49)] public QList<int> UnknownS4List02 { get; set; } = new();
    [FieldOrder(50), FieldCount(3)] public List<byte> UnknownU1Values04 { get; set; } = [];
    [FieldOrder(51)] public QList<int> UnknownS4List03 { get; set; } = new();
    [FieldOrder(52)] public int UnknownS407 { get; set; }
    [FieldOrder(53)] public double UnknownF802 { get; set; }
    [FieldOrder(54), FieldCount(3)] public List<double> UnknownF8Values02 { get; set; } = [];
    [FieldOrder(55), FieldCount(2)] public List<int> UnknownS4Values05 { get; set; } = [];
    [FieldOrder(56)] public double UnknownF803 { get; set; }
    [FieldOrder(57)] public byte UnknownU103 { get; set; }
    [FieldOrder(58)] public int UnknownS408 { get; set; }
    [FieldOrder(59)] public byte UnknownU104 { get; set; }
    [FieldOrder(60), FieldCount(3)] public List<ushort> UnknownU2Values08 { get; set; } = [];
    [FieldOrder(61)] public int UnknownS409 { get; set; }
    [FieldOrder(62), FieldCount(2)] public List<double> UnknownF8Values03 { get; set; } = [];
    [FieldOrder(63)] public int UnknownS410 { get; set; }
    [FieldOrder(64), FieldCount(4)] public List<byte> UnknownU1Values05 { get; set; } = [];
    [FieldOrder(65)] public ushort UnknownU205 { get; set; }
    [FieldOrder(66), FieldCount(2)] public List<int> UnknownS4Values06 { get; set; } = [];
    [FieldOrder(67), FieldCount(5)] public List<byte> UnknownU1Values06 { get; set; } = [];
    [FieldOrder(68), FieldCount(2)] public List<ushort> UnknownU2Values09 { get; set; } = [];
    [FieldOrder(69), FieldCount(5)] public List<byte> UnknownU1Values07 { get; set; } = [];
    [FieldOrder(70), FieldCount(6)] public List<ushort> UnknownU2Values10 { get; set; } = [];
    [FieldOrder(71), FieldCount(4)] public List<byte> UnknownU1Values08 { get; set; } = [];
    [FieldOrder(72), FieldCount(2)] public List<ushort> UnknownU2Values11 { get; set; } = [];
    [FieldOrder(73), FieldCount(5)] public List<double> UnknownF8Values04 { get; set; } = [];
    [FieldOrder(74)] public ushort UnknownU206 { get; set; }
    [FieldOrder(75)] public QList<ushort> UnknownU2List01 { get; set; } = new();
    [FieldOrder(76), FieldCount(5)] public List<double> UnknownF8Values05 { get; set; } = [];
    [FieldOrder(77), FieldCount(4)] public List<ushort> UnknownU2Values12 { get; set; } = [];
    [FieldOrder(78)] public QList<FhmDatedStringRecordData> DatedStringRecords { get; set; } = new();
    [FieldOrder(79), FieldCount(2)] public List<ushort> UnknownU2Values13 { get; set; } = [];
    [FieldOrder(80)] public byte UnknownU105 { get; set; }
    [FieldOrder(81), FieldCount(3)] public List<ushort> UnknownU2Values14 { get; set; } = [];
    [FieldOrder(82)] public byte UnknownU106 { get; set; }
    [FieldOrder(83)] public int UnknownS411 { get; set; }
    [FieldOrder(84), FieldCount(5)] public List<double> UnknownF8Values06 { get; set; } = [];
    [FieldOrder(85)] public ushort UnknownU207 { get; set; }
    [FieldOrder(86)] public double UnknownF804 { get; set; }
    [FieldOrder(87)] public QList<FhmFixed8RecordData> UnknownRecordList02 { get; set; } = new();
    [FieldOrder(88)] public int UnknownS412 { get; set; }
    [FieldOrder(89)] public QList<FhmFixed23RecordData> UnknownRecordList03 { get; set; } = new();
    [FieldOrder(90), FieldCount(2)] public List<ushort> UnknownU2Values15 { get; set; } = [];
    [FieldOrder(91)] public byte UnknownU107 { get; set; }
    [FieldOrder(92), FieldCount(2)] public List<ushort> UnknownU2Values16 { get; set; } = [];
    [FieldOrder(93)] public QString UnknownString04 { get; set; } = new();
    [FieldOrder(94), FieldCount(2)] public List<ushort> UnknownU2Values17 { get; set; } = [];
    [FieldOrder(95), FieldCount(4)] public List<int> UnknownS4Values07 { get; set; } = [];
    [FieldOrder(96)] public FhmOptionalPlayerRoleInstanceData PrimaryRole { get; set; } = new();
    [FieldOrder(97)] public FhmOptionalPlayerRoleInstanceData SupplementaryRole { get; set; } = new();
    [FieldOrder(98), FieldCount(2)] public List<ushort> UnknownU2Values18 { get; set; } = [];
    [FieldOrder(99), FieldCount(2)] public List<byte> UnknownU1Values09 { get; set; } = [];
    [FieldOrder(100)] public QList<FhmFixed2RecordData> UnknownU1PairList { get; set; } = new();
    [FieldOrder(101), FieldCount(2)] public List<int> UnknownS4Values08 { get; set; } = [];
    [FieldOrder(102)] public int ExportedPlayerId { get; set; }
    [FieldOrder(103)] public QList<FhmDatedUshortRecordData> UnknownDatedU2Records { get; set; } = new();
    [FieldOrder(104), FieldCount(3)] public List<double> UnknownF8Values07 { get; set; } = [];
    [FieldOrder(105)] public ushort UnknownU208 { get; set; }
    [FieldOrder(106), FieldCount(2)] public List<byte> UnknownU1Values10 { get; set; } = [];
    [FieldOrder(107)] public QList<int> UnknownS4List04 { get; set; } = new();
    [FieldOrder(108), FieldCount(3)] public List<byte> UnknownU1Values11 { get; set; } = [];
    [FieldOrder(109)] public QList<FhmFixed16RecordData> UnknownRecordList04 { get; set; } = new();
    [FieldOrder(110)] public byte UnknownU108 { get; set; }
    [FieldOrder(111)] public ushort UnknownU209 { get; set; }
    [FieldOrder(112)] public QList<byte> SpecialAbilities { get; set; } = new();
    [FieldOrder(113)] public byte UnknownU109 { get; set; }
    [FieldOrder(114)] public int UnknownS413 { get; set; }
    [FieldOrder(115)] public QList<int> UnknownS4List05 { get; set; } = new();
    [FieldOrder(116)] public int UnknownS414 { get; set; }
    [FieldOrder(117), FieldCount(5)] public List<byte> UnknownU1Values12 { get; set; } = [];
    [FieldOrder(118)] public QList<int> UnknownS4List06 { get; set; } = new();
    [FieldOrder(119)] public QList<FhmByteInt32PairData> UnknownU1S4PairList { get; set; } = new();
    [FieldOrder(120)] public byte UnknownU110 { get; set; }
}

/// <summary>Count-prefixed position ratings.</summary>
public sealed class FhmPlayerPositionRatingsData
{
    [FieldOrder(0)] public QList<ushort> RawValues { get; set; } = new();
}

/// <summary>The complete 58-byte player-attribute vector.</summary>
public sealed class FhmPlayerAttributesData
{
    [FieldOrder(0)] public byte BigGames { get; set; }
    [FieldOrder(1)] public byte Consistency { get; set; }
    [FieldOrder(2)] public byte Greed { get; set; }
    [FieldOrder(3)] public byte Adaptability { get; set; }
    [FieldOrder(4)] public byte Loyalty { get; set; }
    [FieldOrder(5)] public byte Coachability { get; set; }
    [FieldOrder(6)] public byte Aging { get; set; }
    [FieldOrder(7)] public byte Sportsmanship { get; set; }
    [FieldOrder(8)] public byte PassShootTendency { get; set; }
    [FieldOrder(9)] public byte Controversy { get; set; }
    [FieldOrder(10)] public byte HandleCritics { get; set; }
    [FieldOrder(11)] public byte HandleFailure { get; set; }
    [FieldOrder(12)] public byte HandleSuccess { get; set; }
    [FieldOrder(13)] public byte Intelligence { get; set; }
    [FieldOrder(14)] public byte Mood { get; set; }
    [FieldOrder(15)] public byte DevRate { get; set; }
    [FieldOrder(16)] public byte Aggression { get; set; }
    [FieldOrder(17)] public byte Bravery { get; set; }
    [FieldOrder(18)] public byte Determination { get; set; }
    [FieldOrder(19)] public byte Teamplayer { get; set; }
    [FieldOrder(20)] public byte Leadership { get; set; }
    [FieldOrder(21)] public byte Temperament { get; set; }
    [FieldOrder(22)] public byte Professionalism { get; set; }
    [FieldOrder(23)] public byte Ambition { get; set; }
    [FieldOrder(24)] public byte Acceleration { get; set; }
    [FieldOrder(25)] public byte Agility { get; set; }
    [FieldOrder(26)] public byte Balance { get; set; }
    [FieldOrder(27)] public byte Speed { get; set; }
    [FieldOrder(28)] public byte Stamina { get; set; }
    [FieldOrder(29)] public byte Strength { get; set; }
    [FieldOrder(30)] public byte Fighting { get; set; }
    [FieldOrder(31)] public byte GoalieReflexes { get; set; }
    [FieldOrder(32)] public byte GoalieStamina { get; set; }
    [FieldOrder(33)] public byte Screening { get; set; }
    [FieldOrder(34)] public byte GettingOpen { get; set; }
    [FieldOrder(35)] public byte Passing { get; set; }
    [FieldOrder(36)] public byte PuckHandling { get; set; }
    [FieldOrder(37)] public byte ShootingAccuracy { get; set; }
    [FieldOrder(38)] public byte ShootingRange { get; set; }
    [FieldOrder(39)] public byte OffensiveRead { get; set; }
    [FieldOrder(40)] public byte Checking { get; set; }
    [FieldOrder(41)] public byte Faceoffs { get; set; }
    [FieldOrder(42)] public byte Hitting { get; set; }
    [FieldOrder(43)] public byte Positioning { get; set; }
    [FieldOrder(44)] public byte ShotBlocking { get; set; }
    [FieldOrder(45)] public byte Stickchecking { get; set; }
    [FieldOrder(46)] public byte DefensiveRead { get; set; }
    [FieldOrder(47)] public byte GoaliePositioning { get; set; }
    [FieldOrder(48)] public byte GoaliePassing { get; set; }
    [FieldOrder(49)] public byte GoaliePokecheck { get; set; }
    [FieldOrder(50)] public byte GoalieBlocker { get; set; }
    [FieldOrder(51)] public byte GoalieGlove { get; set; }
    [FieldOrder(52)] public byte GoalieRebound { get; set; }
    [FieldOrder(53)] public byte GoalieRecovery { get; set; }
    [FieldOrder(54)] public byte GoaliePuckhandling { get; set; }
    [FieldOrder(55)] public byte GoalieLowShots { get; set; }
    [FieldOrder(56)] public byte MentalToughness { get; set; }
    [FieldOrder(57)] public byte GoalieSkating { get; set; }
}

/// <summary>A selected player tactical role.</summary>
public sealed class FhmPlayerRoleInstanceData
{
    [FieldOrder(0)] public int RoleId { get; set; }
    [FieldOrder(1), FieldCount(9)] public List<byte> UseOverride { get; set; } = [];
    [FieldOrder(2), FieldCount(9)] public List<ushort> TendencyValue { get; set; } = [];
}

/// <summary>FHM's -1-or-role optional tactical-role representation.</summary>
public sealed class FhmOptionalPlayerRoleInstanceData : IBinarySerializable
{
    public FhmPlayerRoleInstanceData? Value { get; set; }

    public void Serialize(Stream stream, Endianness endianness, BinarySerializationContext context)
    {
        ArgumentNullException.ThrowIfNull(stream);
        if (endianness != Endianness.Big)
        {
            throw new NotSupportedException("FHM player roles require big-endian serialization.");
        }

        Span<byte> presence = stackalloc byte[sizeof(int)];
        BinaryPrimitives.WriteInt32BigEndian(presence, Value is null ? -1 : 0);
        stream.Write(presence);
        if (Value is not null)
        {
            QSerializerFactory.Serialize(stream, Value);
        }
    }

    public void Deserialize(Stream stream, Endianness endianness, BinarySerializationContext context)
    {
        ArgumentNullException.ThrowIfNull(stream);
        if (endianness != Endianness.Big)
        {
            throw new NotSupportedException("FHM player roles require big-endian serialization.");
        }

        Span<byte> presence = stackalloc byte[sizeof(int)];
        stream.ReadExactly(presence);
        Value = BinaryPrimitives.ReadInt32BigEndian(presence) switch
        {
            -1 => null,
            0 => QSerializerFactory.DeserializeOne<FhmPlayerRoleInstanceData>(stream),
            var value => throw new InvalidDataException($"Invalid player role presence {value}."),
        };
    }
}

/// <summary>A player contract.</summary>
public sealed class FhmPlayerContractData
{
    [FieldOrder(0), FieldCount(2)] public List<int> UnknownS4Values01 { get; set; } = [];
    [FieldOrder(1), FieldCount(2)] public List<ushort> UnknownU2Values01 { get; set; } = [];
    [FieldOrder(2)] public int UnknownS401 { get; set; }
    [FieldOrder(3)] public QDate UnknownDate01 { get; set; } = new();
    [FieldOrder(4)] public double UnknownF801 { get; set; }
    [FieldOrder(5)] public QList<int> UnknownS4List { get; set; } = new();
    [FieldOrder(6), FieldCount(3)] public List<ushort> UnknownU2Values02 { get; set; } = [];
    [FieldOrder(7)] public int UnknownS402 { get; set; }
    [FieldOrder(8)] public ushort UnknownU201 { get; set; }
    [FieldOrder(9)] public byte UnknownU101 { get; set; }
    [FieldOrder(10), FieldCount(2)] public List<int> UnknownS4Values02 { get; set; } = [];
    [FieldOrder(11)] public ushort UnknownU202 { get; set; }
    [FieldOrder(12), FieldCount(2)] public List<byte> UnknownU1Values01 { get; set; } = [];
    [FieldOrder(13), FieldCount(4)] public List<int> UnknownS4Values03 { get; set; } = [];
    [FieldOrder(14), FieldCount(4)] public List<byte> UnknownU1Values02 { get; set; } = [];
    [FieldOrder(15)] public int UnknownS403 { get; set; }
    [FieldOrder(16), FieldCount(2)] public List<byte> UnknownU1Values03 { get; set; } = [];
    [FieldOrder(17)] public int UnknownS404 { get; set; }
    [FieldOrder(18), FieldCount(3)] public List<byte> UnknownU1Values04 { get; set; } = [];
    [FieldOrder(19)] public int UnknownS405 { get; set; }
    [FieldOrder(20)] public ushort UnknownU203 { get; set; }
    [FieldOrder(21)] public int UnknownS406 { get; set; }
    [FieldOrder(22), FieldCount(3)] public List<byte> UnknownU1Values05 { get; set; } = [];
    [FieldOrder(23), FieldCount(3)] public List<ushort> UnknownU2Values03 { get; set; } = [];
    [FieldOrder(24), FieldCount(4)] public List<byte> UnknownU1Values06 { get; set; } = [];
    [FieldOrder(25)] public QList<byte> UnknownU1List01 { get; set; } = new();
    [FieldOrder(26)] public QList<byte> UnknownU1List02 { get; set; } = new();
    [FieldOrder(27), FieldCount(3)] public List<byte> UnknownU1Values07 { get; set; } = [];
}

/// <summary>Aggregate skater statistics.</summary>
public sealed class FhmAggregateSkaterStatsData
{
    [FieldOrder(0), FieldCount(5)] public List<ushort> HeaderU2 { get; set; } = [];
    [FieldOrder(1)] public int ValueS401 { get; set; }
    [FieldOrder(2), FieldCount(18)] public List<ushort> CountersU201 { get; set; } = [];
    [FieldOrder(3), FieldCount(3)] public List<int> ValuesS401 { get; set; } = [];
    [FieldOrder(4), FieldCount(7)] public List<ushort> CountersU202 { get; set; } = [];
    [FieldOrder(5), FieldCount(3)] public List<double> ValuesF801 { get; set; } = [];
    [FieldOrder(6)] public ushort ValueU201 { get; set; }
    [FieldOrder(7), FieldCount(3)] public List<double> ValuesF802 { get; set; } = [];
    [FieldOrder(8), FieldCount(2)] public List<ushort> ValuesU201 { get; set; } = [];
    [FieldOrder(9), FieldCount(3)] public List<byte> FlagsU1 { get; set; } = [];
}

/// <summary>Aggregate goalie statistics.</summary>
public sealed class FhmAggregateGoalieStatsData
{
    [FieldOrder(0), FieldCount(5)] public List<ushort> HeaderU2 { get; set; } = [];
    [FieldOrder(1)] public int ValueS401 { get; set; }
    [FieldOrder(2), FieldCount(2)] public List<ushort> ValuesU201 { get; set; } = [];
    [FieldOrder(3)] public int ValueS402 { get; set; }
    [FieldOrder(4), FieldCount(10)] public List<ushort> CountersU2 { get; set; } = [];
    [FieldOrder(5)] public double ValueF8 { get; set; }
    [FieldOrder(6)] public ushort ValueU201 { get; set; }
    [FieldOrder(7)] public byte FlagU1 { get; set; }
}

/// <summary>Detailed skater game statistics.</summary>
public sealed class FhmDetailedSkaterGameStatsData
{
    [FieldOrder(0), FieldCount(5)] public List<ushort> HeaderU2 { get; set; } = [];
    [FieldOrder(1)] public int ValueS401 { get; set; }
    [FieldOrder(2), FieldCount(18)] public List<ushort> CountersU201 { get; set; } = [];
    [FieldOrder(3), FieldCount(3)] public List<int> ValuesS401 { get; set; } = [];
    [FieldOrder(4), FieldCount(26)] public List<ushort> CountersU202 { get; set; } = [];
    [FieldOrder(5)] public int ValueS402 { get; set; }
    [FieldOrder(6), FieldCount(3)] public List<double> ValuesF8 { get; set; } = [];
    [FieldOrder(7), FieldCount(6)] public List<ushort> ValuesU201 { get; set; } = [];
    [FieldOrder(8), FieldCount(10)] public List<byte> TrailingU1 { get; set; } = [];
}

/// <summary>Detailed goalie game statistics.</summary>
public sealed class FhmDetailedGoalieGameStatsData
{
    [FieldOrder(0), FieldCount(5)] public List<ushort> HeaderU2 { get; set; } = [];
    [FieldOrder(1)] public int ValueS401 { get; set; }
    [FieldOrder(2), FieldCount(2)] public List<ushort> ValuesU201 { get; set; } = [];
    [FieldOrder(3)] public int ValueS402 { get; set; }
    [FieldOrder(4), FieldCount(10)] public List<ushort> CountersU201 { get; set; } = [];
    [FieldOrder(5), FieldCount(2)] public List<ushort> CountersU202 { get; set; } = [];
    [FieldOrder(6)] public double ValueF8 { get; set; }
    [FieldOrder(7), FieldCount(6)] public List<ushort> ValuesU202 { get; set; } = [];
    [FieldOrder(8)] public byte TrailingU1 { get; set; }
}

/// <summary>A dated string record.</summary>
public sealed class FhmDatedStringRecordData
{
    [FieldOrder(0)] public long JulianDay { get; set; }
    [FieldOrder(1)] public byte UnknownU101 { get; set; }
    [FieldOrder(2), FieldCount(3)] public List<int> UnknownS4Values01 { get; set; } = [];
    [FieldOrder(3)] public QString Text { get; set; } = new();
    [FieldOrder(4), FieldCount(2)] public List<int> UnknownS4Values02 { get; set; } = [];
}

/// <summary>A dated unsigned-value record.</summary>
public sealed class FhmDatedUshortRecordData
{
    [FieldOrder(0)] public QDate Date { get; set; } = new();
    [FieldOrder(1), FieldCount(2)] public List<ushort> UnknownU2Values { get; set; } = [];
}

/// <summary>A two-byte opaque record.</summary>
public sealed class FhmFixed2RecordData
{
    [FieldOrder(0)] public byte Value01 { get; set; }
    [FieldOrder(1)] public byte Value02 { get; set; }
}

/// <summary>An opaque fixed eight-byte record.</summary>
public sealed class FhmFixed8RecordData
{
    [FieldOrder(0), FieldLength(8)] public byte[] Data { get; set; } = [];
}

/// <summary>An opaque fixed sixteen-byte record.</summary>
public sealed class FhmFixed16RecordData
{
    [FieldOrder(0), FieldLength(16)] public byte[] Data { get; set; } = [];
}

/// <summary>An opaque fixed twenty-three-byte record.</summary>
public sealed class FhmFixed23RecordData
{
    [FieldOrder(0), FieldLength(23)] public byte[] Data { get; set; } = [];
}

/// <summary>An opaque fixed twenty-four-byte record.</summary>
public sealed class FhmFixed24RecordData
{
    [FieldOrder(0), FieldLength(24)] public byte[] Data { get; set; } = [];
}

/// <summary>A byte and signed-value pair.</summary>
public sealed class FhmByteInt32PairData
{
    [FieldOrder(0)] public byte UnknownU1 { get; set; }
    [FieldOrder(1)] public int UnknownS4 { get; set; }
}

/// <summary>Serializes complete and standalone <c>players.dat</c> records.</summary>
public static class FhmPlayersFileSerializer
{
    /// <summary>Deserializes a complete <c>players.dat</c> stream.</summary>
    public static FhmPlayersFileData Deserialize(Stream stream) =>
        QSerializerFactory.Deserialize<FhmPlayersFileData>(stream, "players.dat");

    /// <summary>Serializes a complete <c>players.dat</c> stream.</summary>
    public static void Serialize(Stream stream, FhmPlayersFileData value) =>
        QSerializerFactory.Serialize(stream, value);

    /// <summary>Deserializes one self-delimiting player record.</summary>
    public static FhmPlayerRecordData DeserializeRecord(Stream stream) =>
        QSerializerFactory.DeserializeOne<FhmPlayerRecordData>(stream);

    /// <summary>Serializes one player record without a surrounding file container.</summary>
    public static void SerializeRecord(Stream stream, FhmPlayerRecordData value) =>
        QSerializerFactory.Serialize(stream, value);
}
