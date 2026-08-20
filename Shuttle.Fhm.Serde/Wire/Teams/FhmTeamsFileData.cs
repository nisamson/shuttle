using System.Buffers.Binary;
using BinarySerialization;
using Shuttle.BinarySerde.Common.QFormat;

namespace Shuttle.Fhm.Serde.Wire.Teams;

/// <summary>The declarative <c>teams.dat</c> wire contract.</summary>
public sealed class FhmTeamsFileData
{
    [FieldOrder(0)] public int VersionTag { get; set; }
    [FieldOrder(1)] public int TeamCount { get; set; }
    [FieldOrder(2), FieldCount(nameof(TeamCount))] public List<FhmTeamRecordData> Teams { get; set; } = [];
}

/// <summary>One complete team record.</summary>
public sealed class FhmTeamRecordData
{
    [FieldOrder(0)] public int RecordIndex { get; set; }
    [FieldOrder(1)] public int TeamId { get; set; }
    [FieldOrder(2)] public QString InternalCode { get; set; } = new();
    [FieldOrder(3)] public QString InternalCode2 { get; set; } = new();
    [FieldOrder(4)] public byte Flag1 { get; set; }
    [FieldOrder(5)] public QString City { get; set; } = new();
    [FieldOrder(6)] public QString Nickname { get; set; } = new();
    [FieldOrder(7)] public byte NicknamePlacement { get; set; }
    [FieldOrder(8)] public int AffiliateParentId { get; set; }
    [FieldOrder(9)] public int AffiliateParentId2 { get; set; }
    [FieldOrder(10)] public int LeagueId { get; set; }
    [FieldOrder(11)] public int ConferenceId { get; set; }
    [FieldOrder(12)] public int DivisionId { get; set; }
    [FieldOrder(13)] public int LocationId { get; set; }
    [FieldOrder(14)] public ushort MarketSize { get; set; }
    [FieldOrder(15)] public ushort FanLoyalty { get; set; }
    [FieldOrder(16)] public int Finance1 { get; set; }
    [FieldOrder(17)] public int Finance2 { get; set; }
    [FieldOrder(18)] public int Finance3 { get; set; }
    [FieldOrder(19)] public int UnknownInt32_13 { get; set; }
    [FieldOrder(20)] public int UnknownInt32_14 { get; set; }
    [FieldOrder(21)] public int Finance4 { get; set; }
    [FieldOrder(22)] public int SeasonHistoryCount { get; set; }
    [FieldOrder(23), FieldCount(nameof(SeasonHistoryCount))] public List<FhmTeamSeasonRecordData> SeasonHistory { get; set; } = [];
    [FieldOrder(24)] public FhmFranchiseHistoryData FranchiseHistory { get; set; } = new();
    [FieldOrder(25)] public FhmActiveLineUnitData ActiveLines { get; set; } = new();
    [FieldOrder(26)] public FhmLeadershipReserveData LeadershipReserve { get; set; } = new();
    [FieldOrder(27)] public FhmSeasonParticipationChainData SeasonParticipation { get; set; } = new();
    [FieldOrder(28)] public FhmRosterChainData Roster { get; set; } = new();
    [FieldOrder(29)] public FhmTeamPostHeadData PostHead { get; set; } = new();
    [FieldOrder(30)] public FhmTeamPostBodyData PostBody { get; set; } = new();
    [FieldOrder(31)] public FhmTeamTailData Tail { get; set; } = new();
}

public sealed class FhmTeamSeasonRecordData
{
    [FieldOrder(0)] public int Year { get; set; }
    [FieldOrder(1)] public QString City { get; set; } = new();
    [FieldOrder(2)] public QString Nickname { get; set; } = new();
    [FieldOrder(3)] public QString Abbreviation { get; set; } = new();
    [FieldOrder(4), FieldLength(134)] public byte[] Stats { get; set; } = [];
}

public sealed class FhmFranchiseHistoryData
{
    [FieldOrder(0), FieldLength(115)] public byte[] Head { get; set; } = [];
    [FieldOrder(1)] public int SeasonStatisticCount { get; set; }
    [FieldOrder(2), FieldLength(8)] public byte[] HeadTail { get; set; } = [];
    [FieldOrder(3), FieldCount(nameof(SeasonStatisticCount))] public List<FhmFranchiseSeasonStatisticData> SeasonStatistics { get; set; } = [];
    [FieldOrder(4)] public int NameHistoryCount { get; set; }
    [FieldOrder(5)] public int NameHistoryStringCount { get; set; }
    [FieldOrder(6), FieldCount(nameof(NameHistoryStringCount))] public List<QString> NameHistory { get; set; } = [];
    [FieldOrder(7)] public int AbbreviationHistoryCount { get; set; }
    [FieldOrder(8), FieldCount(nameof(AbbreviationHistoryCount))] public List<QString> AbbreviationHistory { get; set; } = [];
}

public sealed class FhmFranchiseSeasonStatisticData
{
    [FieldOrder(0), FieldLength(108)] public byte[] Body { get; set; } = [];
    [FieldOrder(1)] public byte Flag { get; set; }
    [FieldOrder(2)] public int Value { get; set; }
    [FieldOrder(3)] public ushort Year { get; set; }
}

public sealed class FhmActiveLineUnitData
{
    [FieldOrder(0), FieldCount(13)] public List<QList<int>> Lists { get; set; } = [];
}

public sealed class FhmLeadershipReserveData
{
    [FieldOrder(0)] public int Captain { get; set; }
    [FieldOrder(1)] public int AlternateCaptain1 { get; set; }
    [FieldOrder(2)] public int AlternateCaptain2 { get; set; }
    [FieldOrder(3)] public QList<int> ReserveSlots { get; set; } = new();
}

/// <summary>The count-delimited participation blocks before the roster delimiter.</summary>
public sealed class FhmSeasonParticipationChainData : IBinarySerializable
{
    public List<FhmSeasonParticipationBlockData> Blocks { get; set; } = [];

    public void Deserialize(Stream stream, Endianness endianness, BinarySerializationContext context)
    {
        FhmTeamsWirePrimitives.RequireBigEndian(endianness);
        Blocks = [];
        do
        {
            Blocks.Add(QSerializerFactory.DeserializeOne<FhmSeasonParticipationBlockData>(stream));
        }
        while (FhmTeamsWirePrimitives.PeekInt32(stream) != 0);
    }

    public void Serialize(Stream stream, Endianness endianness, BinarySerializationContext context)
    {
        FhmTeamsWirePrimitives.RequireBigEndian(endianness);
        foreach (var block in Blocks)
        {
            QSerializerFactory.Serialize(stream, block);
        }
    }
}

public sealed class FhmSeasonParticipationBlockData
{
    [FieldOrder(0)] public QList<FhmSeasonParticipationRecordData> Records { get; set; } = new();
}

public sealed class FhmSeasonParticipationRecordData
{
    [FieldOrder(0)] public ushort SequenceNumber { get; set; }
    [FieldOrder(1)] public ushort Year { get; set; }
    [FieldOrder(2)] public int ParticipationId { get; set; }
    [FieldOrder(3)] public byte Flag { get; set; }
}

/// <summary>The self-delimiting roster lists preceding the post-head marker.</summary>
public sealed class FhmRosterChainData : IBinarySerializable
{
    private static ReadOnlySpan<byte> PostHeadSignature => [0, 100, 1];

    public List<QList<int>> Lists { get; set; } = [];

    public void Deserialize(Stream stream, Endianness endianness, BinarySerializationContext context)
    {
        FhmTeamsWirePrimitives.RequireBigEndian(endianness);
        Lists = [];
        while (!FhmTeamsWirePrimitives.PeekBytes(stream, 3, 4).SequenceEqual(PostHeadSignature))
        {
            Lists.Add(QSerializerFactory.DeserializeOne<QList<int>>(stream));
        }
    }

    public void Serialize(Stream stream, Endianness endianness, BinarySerializationContext context)
    {
        FhmTeamsWirePrimitives.RequireBigEndian(endianness);
        foreach (var list in Lists)
        {
            QSerializerFactory.Serialize(stream, list);
        }
    }
}

public sealed class FhmTeamPostHeadData
{
    [FieldOrder(0), FieldLength(7)] public byte[] Pre { get; set; } = [];
    [FieldOrder(1)] public QList<int> UnknownListA { get; set; } = new();
    [FieldOrder(2), FieldLength(196)] public byte[] Gap { get; set; } = [];
    [FieldOrder(3)] public QList<int> Goalies { get; set; } = new();
    [FieldOrder(4)] public QList<int> Defensemen { get; set; } = new();
    [FieldOrder(5)] public QList<int> Forwards { get; set; } = new();
    [FieldOrder(6)] public int RegionId { get; set; }
    [FieldOrder(7)] public ushort NationIndex { get; set; }
    [FieldOrder(8)] public FhmPositionRequirementWordsData PositionRequirements { get; set; } = new();
}

public sealed class FhmPositionRequirementWordsData
{
    [FieldOrder(0)] public ushort LogicalEntryCount { get; set; }
    [FieldOrder(1)] public QList<int> Words { get; set; } = new();
}

public sealed class FhmTeamPostBodyData
{
    [FieldOrder(0)] public int OpenRosterSlots { get; set; }
    [FieldOrder(1)] public QList<int> AllTimePlayers { get; set; } = new();
    [FieldOrder(2)] public ushort PreColourValue { get; set; }
    [FieldOrder(3)] public QList<int> PreColourList { get; set; } = new();
    [FieldOrder(4)] public byte PreColourFlag1 { get; set; }
    [FieldOrder(5)] public int PreColourValue1 { get; set; }
    [FieldOrder(6)] public int PreColourValue2 { get; set; }
    [FieldOrder(7)] public int PreColourValue3 { get; set; }
    [FieldOrder(8)] public ushort PreColourValue4 { get; set; }
    [FieldOrder(9)] public ushort PreColourValue5 { get; set; }
    [FieldOrder(10)] public byte PreColourFlag2 { get; set; }
    [FieldOrder(11)] public byte PreColourFlag3 { get; set; }
    [FieldOrder(12), FieldCount(13)] public List<FhmQColorData> Colours { get; set; } = [];
    [FieldOrder(13), FieldLength(41)] public byte[] Gap2 { get; set; } = [];
    [FieldOrder(14)] public QString Abbreviation { get; set; } = new();
    [FieldOrder(15), FieldLength(1)] public byte[] Pad { get; set; } = [];
    [FieldOrder(16), FieldCount(4)] public List<FhmTeamPostUnitData> Units { get; set; } = [];
}

public sealed class FhmQColorData
{
    [FieldOrder(0)] public byte Spec { get; set; }
    [FieldOrder(1)] public ushort Alpha { get; set; }
    [FieldOrder(2)] public ushort Red { get; set; }
    [FieldOrder(3)] public ushort Green { get; set; }
    [FieldOrder(4)] public ushort Blue { get; set; }
    [FieldOrder(5), FieldCount(2)] public List<byte> Reserved { get; set; } = [];
}

public sealed class FhmTeamPostUnitData
{
    [FieldOrder(0)] public byte Marker1 { get; set; }
    [FieldOrder(1)] public byte Marker2 { get; set; }
    [FieldOrder(2)] public QList<int> Values { get; set; } = new();
}

public sealed class FhmTeamTailData
{
    [FieldOrder(0), FieldLength(32)] public byte[] Pre { get; set; } = [];
    [FieldOrder(1)] public ushort UnknownMRecordCount { get; set; }
    [FieldOrder(2)] public int UnknownMWordCount { get; set; }
    [FieldOrder(3), FieldLength(139)] public byte[] Pre2Prefix { get; set; } = [];
    [FieldOrder(4)] public ushort FanHappiness { get; set; }
    [FieldOrder(5), FieldLength(21)] public byte[] Pre2Suffix { get; set; } = [];
    [FieldOrder(6), FieldCount(nameof(UnknownMRecordCount))] public List<FhmFixed12BytesData> UnknownMRecords { get; set; } = [];
    [FieldOrder(7)] public int RetiredNumberCount { get; set; }
    [FieldOrder(8), FieldCount(nameof(RetiredNumberCount))] public List<FhmRetiredNumberData> RetiredNumbers { get; set; } = [];
    [FieldOrder(9)] public QString WikiUrl { get; set; } = new();
    [FieldOrder(10)] public QString WebsiteUrl { get; set; } = new();
    [FieldOrder(11)] public FhmTeamTacticsSettingsData Tactics { get; set; } = new();
    [FieldOrder(12)] public FhmTeamTailRestData Rest { get; set; } = new();
}

public sealed class FhmFixed12BytesData
{
    [FieldOrder(0), FieldLength(12)] public byte[] Data { get; set; } = [];
}

public sealed class FhmRetiredNumberData
{
    [FieldOrder(0)] public ushort Year { get; set; }
    [FieldOrder(1)] public ushort Number { get; set; }
    [FieldOrder(2)] public ushort Flag { get; set; }
    [FieldOrder(3)] public int PlayerReference { get; set; }
}

/// <summary>The 1,303-byte team-owned tactics region.</summary>
public sealed class FhmTeamTacticsSettingsData : IBinarySerializable
{
    [Ignore] public ushort TeamValue1 { get; set; }
    [Ignore] public byte TeamFlag { get; set; }
    [Ignore] public byte TeamRating { get; set; }
    [Ignore] public List<int> TeamValues2To4 { get; set; } = [];
    [Ignore] public int TacticsObjectVersion { get; set; }
    [Ignore] public List<ushort> BaseSettings { get; set; } = [];
    [Ignore] public List<FhmZoneSelectorBlockData> Selectors { get; set; } = [];
    [Ignore] public ushort FinalOffensiveOrientation { get; set; }
    [Ignore] public ushort FinalPhysicalOrientation { get; set; }
    [Ignore] public List<byte> FinalUseOwnSettingsFlags { get; set; } = [];
    [Ignore] public List<FhmTendencyBlockData> Tendencies { get; set; } = [];

    public void Deserialize(Stream stream, Endianness endianness, BinarySerializationContext context)
    {
        FhmTeamsWirePrimitives.RequireBigEndian(endianness);
        var header = QSerializerFactory.DeserializeOne<FhmTeamTacticsHeaderData>(stream);
        TeamValue1 = header.TeamValue1;
        TeamFlag = header.TeamFlag;
        TeamRating = header.TeamRating;
        TeamValues2To4 = header.TeamValues2To4;
        TacticsObjectVersion = header.TacticsObjectVersion;
        BaseSettings = header.BaseSettings;

        Selectors = [];
        for (var index = 0; index < 22; index++)
        {
            Selectors.Add(FhmZoneSelectorBlockData.Deserialize(stream, index));
        }

        var footer = QSerializerFactory.DeserializeOne<FhmTeamTacticsFooterData>(stream);
        FinalOffensiveOrientation = footer.FinalOffensiveOrientation;
        FinalPhysicalOrientation = footer.FinalPhysicalOrientation;
        FinalUseOwnSettingsFlags = footer.FinalUseOwnSettingsFlags;
        Tendencies = [];
        for (var index = 0; index < 22; index++)
        {
            Tendencies.Add(QSerializerFactory.DeserializeOne<FhmTendencyBlockData>(stream));
        }
    }

    public void Serialize(Stream stream, Endianness endianness, BinarySerializationContext context)
    {
        FhmTeamsWirePrimitives.RequireBigEndian(endianness);
        QSerializerFactory.Serialize(stream, new FhmTeamTacticsHeaderData
        {
            TeamValue1 = TeamValue1,
            TeamFlag = TeamFlag,
            TeamRating = TeamRating,
            TeamValues2To4 = TeamValues2To4,
            TacticsObjectVersion = TacticsObjectVersion,
            BaseSettings = BaseSettings,
        });
        foreach (var selector in Selectors)
        {
            selector.Serialize(stream);
        }

        QSerializerFactory.Serialize(stream, new FhmTeamTacticsFooterData
        {
            FinalOffensiveOrientation = FinalOffensiveOrientation,
            FinalPhysicalOrientation = FinalPhysicalOrientation,
            FinalUseOwnSettingsFlags = FinalUseOwnSettingsFlags,
        });
        foreach (var tendency in Tendencies)
        {
            QSerializerFactory.Serialize(stream, tendency);
        }
    }
}

public sealed class FhmTeamTacticsHeaderData
{
    [FieldOrder(0)] public ushort TeamValue1 { get; set; }
    [FieldOrder(1)] public byte TeamFlag { get; set; }
    [FieldOrder(2)] public byte TeamRating { get; set; }
    [FieldOrder(3), FieldCount(3)] public List<int> TeamValues2To4 { get; set; } = [];
    [FieldOrder(4)] public int TacticsObjectVersion { get; set; }
    [FieldOrder(5), FieldCount(59)] public List<ushort> BaseSettings { get; set; } = [];
}

public sealed class FhmZoneSelectorBlockData
{
    [Ignore] public int BlockIndex { get; set; }
    public List<ushort> SystemIds { get; set; } = [];
    public ushort OffensiveOrientation { get; set; }
    public ushort PhysicalOrientation { get; set; }
    public List<byte> DelayedUseOwnSettingsFlags { get; set; } = [];

    internal static FhmZoneSelectorBlockData Deserialize(Stream stream, int blockIndex)
    {
        var result = QSerializerFactory.DeserializeOne<FhmZoneSelectorSystemsData>(stream);
        var selector = new FhmZoneSelectorBlockData { BlockIndex = blockIndex, SystemIds = result.SystemIds };
        if (blockIndex != 21)
        {
            var orientation = QSerializerFactory.DeserializeOne<FhmZoneSelectorOrientationData>(stream);
            selector.OffensiveOrientation = orientation.OffensiveOrientation;
            selector.PhysicalOrientation = orientation.PhysicalOrientation;
        }

        selector.DelayedUseOwnSettingsFlags = FhmTeamsWirePrimitives.ReadBytes(stream, GetDelayedFlagCount(blockIndex)).ToList();
        return selector;
    }

    internal void Serialize(Stream stream)
    {
        QSerializerFactory.Serialize(stream, new FhmZoneSelectorSystemsData { SystemIds = SystemIds });
        if (BlockIndex != 21)
        {
            QSerializerFactory.Serialize(stream, new FhmZoneSelectorOrientationData
            {
                OffensiveOrientation = OffensiveOrientation,
                PhysicalOrientation = PhysicalOrientation,
            });
        }

        FhmTeamsWirePrimitives.WriteBytes(stream, DelayedUseOwnSettingsFlags);
    }

    internal static int GetDelayedFlagCount(int blockIndex) => blockIndex switch
    {
        4 => 4,
        13 => 3,
        6 or 8 or 10 or 15 or 17 or 19 => 2,
        _ => 0,
    };
}

public sealed class FhmZoneSelectorSystemsData
{
    [FieldOrder(0), FieldCount(12)] public List<ushort> SystemIds { get; set; } = [];
}

public sealed class FhmZoneSelectorOrientationData
{
    [FieldOrder(0)] public ushort OffensiveOrientation { get; set; }
    [FieldOrder(1)] public ushort PhysicalOrientation { get; set; }
}

public sealed class FhmTeamTacticsFooterData
{
    [FieldOrder(0)] public ushort FinalOffensiveOrientation { get; set; }
    [FieldOrder(1)] public ushort FinalPhysicalOrientation { get; set; }
    [FieldOrder(2), FieldCount(2)] public List<byte> FinalUseOwnSettingsFlags { get; set; } = [];
}

public sealed class FhmTendencyBlockData
{
    [FieldOrder(0), FieldCount(8)] public List<ushort> Values { get; set; } = [];
    [FieldOrder(1), FieldCount(8)] public List<byte> Overrides { get; set; } = [];
}

public sealed class FhmTeamTailRestData
{
    [FieldOrder(0)] public QList<FhmJuniorHistoryRecordData> MajorJuniorHistory { get; set; } = new();
    [FieldOrder(1)] public int MainRivalRecordIndex { get; set; }
    [FieldOrder(2)] public int PotentialRivalRecordIndex { get; set; }
    [FieldOrder(3)] public ushort PotentialRivalProgress { get; set; }
    [FieldOrder(4)] public QList<FhmFanHappinessHistoryRecordData> FanHappinessHistory { get; set; } = new();
    [FieldOrder(5), FieldCount(13)] public List<QList<byte>> ActiveLineSlotLocks { get; set; } = [];
    [FieldOrder(6), FieldCount(17)] public List<QList<int>> ManagedDepthCharts { get; set; } = [];
    [FieldOrder(7)] public int UnknownInt32_1 { get; set; }
    [FieldOrder(8)] public int UnknownInt32_2 { get; set; }
    [FieldOrder(9), FieldCount(7)] public List<QList<ushort>> UnsignedShortLists { get; set; } = [];
    [FieldOrder(10)] public QList<int> AdditionalPlayerIds { get; set; } = new();
    [FieldOrder(11)] public byte Flag1 { get; set; }
    [FieldOrder(12)] public byte Flag2 { get; set; }
    [FieldOrder(13)] public int UnknownInt32_3 { get; set; }
    [FieldOrder(14)] public byte Flag3 { get; set; }
    [FieldOrder(15)] public double UnknownDouble_1 { get; set; }
    [FieldOrder(16)] public int UnknownInt32_4 { get; set; }
    [FieldOrder(17)] public byte UnknownByte_1 { get; set; }
    [FieldOrder(18)] public byte UnknownByte_2 { get; set; }
    [FieldOrder(19)] public byte UnknownByte_3 { get; set; }
    [FieldOrder(20)] public ushort UnknownUInt16_1 { get; set; }
    [FieldOrder(21)] public ushort UnknownUInt16_2 { get; set; }
    [FieldOrder(22)] public byte UnknownByte_4 { get; set; }
    [FieldOrder(23)] public byte UnknownByte_5 { get; set; }
    [FieldOrder(24), FieldCount(6)] public List<byte> Flags4To9 { get; set; } = [];
    [FieldOrder(25), FieldCount(4)] public List<byte> UnknownBytes6To9 { get; set; } = [];
    [FieldOrder(26)] public ushort UnknownUInt16_3 { get; set; }
    [FieldOrder(27)] public double UnknownDouble_2 { get; set; }
    [FieldOrder(28)] public int UnknownInt32_5 { get; set; }
    [FieldOrder(29)] public byte Flag10 { get; set; }
    [FieldOrder(30)] public ushort UnknownUInt16_4 { get; set; }
    [FieldOrder(31)] public byte UnknownByte_10 { get; set; }
    [FieldOrder(32)] public QList<FhmFixed89BytesData> FinanceCurveRecords { get; set; } = new();
    [FieldOrder(33)] public int UnknownInt32_6 { get; set; }
    [FieldOrder(34)] public double UnknownDouble_3 { get; set; }
    [FieldOrder(35)] public byte Flag11 { get; set; }
    [FieldOrder(36), FieldCount(3)] public List<byte> UnknownBytes11To13 { get; set; } = [];
    [FieldOrder(37)] public byte Flag12 { get; set; }
    [FieldOrder(38)] public QList<QList<int>> NestedPlayerIdLists { get; set; } = new();
    [FieldOrder(39)] public byte Flag13 { get; set; }
    [FieldOrder(40)] public byte Flag14 { get; set; }
    [FieldOrder(41), FieldCount(3)] public List<byte> UnknownBytes14To16 { get; set; } = [];
    [FieldOrder(42)] public QList<FhmTaggedPlayerIdData> TaggedPlayerIds { get; set; } = new();
    [FieldOrder(43)] public ushort ClosingUInt16_1 { get; set; }
    [FieldOrder(44)] public ushort ClosingUInt16_2 { get; set; }
    [FieldOrder(45)] public byte ClosingFlag1 { get; set; }
    [FieldOrder(46)] public double ClosingDouble1 { get; set; }
    [FieldOrder(47)] public double ClosingDouble2 { get; set; }
    [FieldOrder(48)] public byte ClosingFlag2 { get; set; }
    [FieldOrder(49)] public double ClosingDouble3 { get; set; }
    [FieldOrder(50)] public double ClosingDouble4 { get; set; }
    [FieldOrder(51)] public byte ClosingFlag3 { get; set; }
    [FieldOrder(52)] public ushort ClosingUInt16_3 { get; set; }
    [FieldOrder(53)] public ushort ClosingUInt16_4 { get; set; }
    [FieldOrder(54)] public byte ClosingFlag4 { get; set; }
    [FieldOrder(55)] public int ClosingInt32_1 { get; set; }
    [FieldOrder(56)] public int ClosingInt32_2 { get; set; }
    [FieldOrder(57), FieldCount(9)] public List<byte> ClosingFlags { get; set; } = [];
    [FieldOrder(58)] public int ClosingInt32_3 { get; set; }
}

public sealed class FhmJuniorHistoryRecordData
{
    [FieldOrder(0)] public ushort Flag { get; set; }
    [FieldOrder(1)] public ushort Year { get; set; }
    [FieldOrder(2)] public int TeamId { get; set; }
    [FieldOrder(3)] public byte Pad { get; set; }
}

public sealed class FhmFanHappinessHistoryRecordData
{
    [FieldOrder(0)] public ushort EventType { get; set; }
    [FieldOrder(1)] public ushort ResultingHappiness { get; set; }
    [FieldOrder(2)] public int PlayerReference { get; set; }
    [FieldOrder(3)] public int StaffId { get; set; }
    [FieldOrder(4)] public int RelatedTeamRecordIndex { get; set; }
    [FieldOrder(5)] public int CompetitionId { get; set; }
    [FieldOrder(6)] public int LeagueId { get; set; }
}

public sealed class FhmFixed89BytesData
{
    [FieldOrder(0), FieldLength(89)] public byte[] Data { get; set; } = [];
}

public sealed class FhmTaggedPlayerIdData
{
    [FieldOrder(0)] public int PlayerReference { get; set; }
    [FieldOrder(1)] public byte Tag { get; set; }
}

/// <summary>Serializes complete <c>teams.dat</c> streams.</summary>
public static class FhmTeamsFileSerializer
{
    /// <summary>Deserializes only the fixed-size tactics region within a team record.</summary>
    public static FhmTeamTacticsSettingsData DeserializeSettings(Stream stream) =>
        QSerializerFactory.Deserialize<FhmTeamTacticsSettingsData>(stream, "team tactics settings");

    /// <summary>Deserializes a complete <c>teams.dat</c> stream.</summary>
    public static FhmTeamsFileData Deserialize(Stream stream) =>
        QSerializerFactory.Deserialize<FhmTeamsFileData>(stream, "teams.dat");

    /// <summary>Serializes a <c>teams.dat</c> stream.</summary>
    public static void Serialize(Stream stream, FhmTeamsFileData value) =>
        QSerializerFactory.Serialize(stream, value);

    /// <summary>Serializes only the fixed-size tactics region within a team record.</summary>
    public static void SerializeSettings(Stream stream, FhmTeamTacticsSettingsData value) =>
        QSerializerFactory.Serialize(stream, value);
}

internal static class FhmTeamsWirePrimitives
{
    internal static void RequireBigEndian(Endianness endianness)
    {
        if (endianness != Endianness.Big)
        {
            throw new NotSupportedException("FHM teams data requires big-endian serialization.");
        }
    }

    internal static int PeekInt32(Stream stream)
    {
        Span<byte> bytes = stackalloc byte[sizeof(int)];
        ReadAt(stream, stream.Position, bytes);
        return BinaryPrimitives.ReadInt32BigEndian(bytes);
    }

    internal static byte[] PeekBytes(Stream stream, int length, int relativeOffset)
    {
        var bytes = new byte[length];
        ReadAt(stream, checked(stream.Position + relativeOffset), bytes);
        return bytes;
    }

    internal static byte[] ReadBytes(Stream stream, int length)
    {
        var bytes = new byte[length];
        stream.ReadExactly(bytes);
        return bytes;
    }

    internal static void WriteBytes(Stream stream, IEnumerable<byte> values)
    {
        foreach (var value in values)
        {
            stream.WriteByte(value);
        }
    }

    private static void ReadAt(Stream stream, long position, Span<byte> destination)
    {
        if (!stream.CanSeek)
        {
            throw new NotSupportedException("FHM teams data requires a seekable stream.");
        }

        var originalPosition = stream.Position;
        try
        {
            stream.Position = position;
            stream.ReadExactly(destination);
        }
        finally
        {
            stream.Position = originalPosition;
        }
    }
}
