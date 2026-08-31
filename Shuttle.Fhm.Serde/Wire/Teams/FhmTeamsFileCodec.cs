using Shuttle.BinarySerde.Common.QFormat;
using Shuttle.BinarySerde.Dsl;
using Shuttle.Fhm.Serde.Wire;
using static Shuttle.BinarySerde.Dsl.BinaryCodecs;

namespace Shuttle.Fhm.Serde.Wire.Teams;

/// <summary>Reflection-free schemas for the complete <c>teams.dat</c> wire format.</summary>
internal static class FhmTeamsFileCodec
{
    private static readonly byte[] PostHeadSignature = [0, 100, 1];
    private static readonly ValueCodec<FhmTeamRecordData> TeamRecord = new(ReadTeam, WriteTeam);
    private static readonly ValueCodec<QList<int>> RosterList =
        FhmWireCodecs.QList(FhmWireCodecs.Int32, "roster players");

    private static readonly RecordCodec<FhmTeamSeasonRecordData> TeamSeason = Record<FhmTeamSeasonRecordData>()
        .Field(static value => value.Year, static (value, field) => value.Year = field, FhmWireCodecs.Int32)
        .Field(static value => value.City, static (value, field) => value.City = field, FhmWireCodecs.QString)
        .Field(static value => value.Nickname, static (value, field) => value.Nickname = field, FhmWireCodecs.QString)
        .Field(static value => value.Abbreviation, static (value, field) => value.Abbreviation = field, FhmWireCodecs.QString)
        .Field(static value => value.Stats, static (value, field) => value.Stats = field, FixedBytes(134))
        .Build();

    private static readonly RecordCodec<FhmFranchiseSeasonStatisticData> FranchiseSeasonStatistic =
        Record<FhmFranchiseSeasonStatisticData>()
            .Field(static value => value.Body, static (value, field) => value.Body = field, FixedBytes(108))
            .Field(static value => value.Flag, static (value, field) => value.Flag = field, FhmWireCodecs.Byte)
            .Field(static value => value.Value, static (value, field) => value.Value = field, FhmWireCodecs.Int32)
            .Field(static value => value.Year, static (value, field) => value.Year = field, FhmWireCodecs.UInt16)
            .Build();

    private static readonly ValueCodec<FhmFranchiseHistoryData> FranchiseHistory = new(
        static reader => ReadFranchiseHistory(reader),
        static (writer, value) => WriteFranchiseHistory(writer, value));

    private static readonly ValueCodec<FhmActiveLineUnitData> ActiveLines = new(
        static reader => new FhmActiveLineUnitData
        {
            Lists = ReadFixedQLists(reader, 13, FhmWireCodecs.QList(FhmWireCodecs.Int32, "active line players")),
        },
        static (writer, value) => WriteQLists(writer, value.Lists, FhmWireCodecs.QList(FhmWireCodecs.Int32, "active line players")));

    private static readonly RecordCodec<FhmLeadershipReserveData> LeadershipReserve =
        Record<FhmLeadershipReserveData>()
            .Field(static value => value.Captain, static (value, field) => value.Captain = field, FhmWireCodecs.Int32)
            .Field(static value => value.AlternateCaptain1, static (value, field) => value.AlternateCaptain1 = field, FhmWireCodecs.Int32)
            .Field(static value => value.AlternateCaptain2, static (value, field) => value.AlternateCaptain2 = field, FhmWireCodecs.Int32)
            .Field(static value => value.ReserveSlots, static (value, field) => value.ReserveSlots = field, FhmWireCodecs.QList(FhmWireCodecs.Int32, nameof(FhmLeadershipReserveData.ReserveSlots)))
            .Build();

    private static readonly RecordCodec<FhmSeasonParticipationRecordData> ParticipationRecord =
        Record<FhmSeasonParticipationRecordData>()
            .Field(static value => value.SequenceNumber, static (value, field) => value.SequenceNumber = field, FhmWireCodecs.UInt16)
            .Field(static value => value.Year, static (value, field) => value.Year = field, FhmWireCodecs.UInt16)
            .Field(static value => value.ParticipationId, static (value, field) => value.ParticipationId = field, FhmWireCodecs.Int32)
            .Field(static value => value.Flag, static (value, field) => value.Flag = field, FhmWireCodecs.Byte)
            .Build();

    private static readonly RecordCodec<FhmSeasonParticipationBlockData> ParticipationBlock =
        Record<FhmSeasonParticipationBlockData>()
            .Field(
                static value => value.Records,
                static (value, field) => value.Records = field,
                FhmWireCodecs.QList(Object(ParticipationRecord), nameof(FhmSeasonParticipationBlockData.Records)))
            .Build();

    private static readonly ValueCodec<FhmSeasonParticipationChainData> SeasonParticipation = new(
        static reader => ReadSeasonParticipation(reader),
        static (writer, value) => WriteSeasonParticipation(writer, value));

    private static readonly ValueCodec<FhmRosterChainData> Roster = new(
        static reader => ReadRoster(reader),
        static (writer, value) => WriteRoster(writer, value));

    private static readonly RecordCodec<FhmPositionRequirementWordsData> PositionRequirements =
        Record<FhmPositionRequirementWordsData>()
            .Field(static value => value.LogicalEntryCount, static (value, field) => value.LogicalEntryCount = field, FhmWireCodecs.UInt16)
            .Field(static value => value.Words, static (value, field) => value.Words = field, FhmWireCodecs.QList(FhmWireCodecs.Int32, nameof(FhmPositionRequirementWordsData.Words)))
            .Build();

    private static readonly RecordCodec<FhmTeamPostHeadData> PostHead = Record<FhmTeamPostHeadData>()
        .Field(static value => value.Pre, static (value, field) => value.Pre = field, FixedBytes(7))
        .Field(static value => value.UnknownListA, static (value, field) => value.UnknownListA = field, FhmWireCodecs.QList(FhmWireCodecs.Int32, nameof(FhmTeamPostHeadData.UnknownListA)))
        .Field(static value => value.Gap, static (value, field) => value.Gap = field, FixedBytes(196))
        .Field(static value => value.Goalies, static (value, field) => value.Goalies = field, FhmWireCodecs.QList(FhmWireCodecs.Int32, nameof(FhmTeamPostHeadData.Goalies)))
        .Field(static value => value.Defensemen, static (value, field) => value.Defensemen = field, FhmWireCodecs.QList(FhmWireCodecs.Int32, nameof(FhmTeamPostHeadData.Defensemen)))
        .Field(static value => value.Forwards, static (value, field) => value.Forwards = field, FhmWireCodecs.QList(FhmWireCodecs.Int32, nameof(FhmTeamPostHeadData.Forwards)))
        .Field(static value => value.RegionId, static (value, field) => value.RegionId = field, FhmWireCodecs.Int32)
        .Field(static value => value.NationIndex, static (value, field) => value.NationIndex = field, FhmWireCodecs.UInt16)
        .Field(static value => value.PositionRequirements, static (value, field) => value.PositionRequirements = field, Object(PositionRequirements))
        .Build();

    private static readonly RecordCodec<FhmQColorData> Colour = Record<FhmQColorData>()
        .Field(static value => value.Spec, static (value, field) => value.Spec = field, FhmWireCodecs.Byte)
        .Field(static value => value.Alpha, static (value, field) => value.Alpha = field, FhmWireCodecs.UInt16)
        .Field(static value => value.Red, static (value, field) => value.Red = field, FhmWireCodecs.UInt16)
        .Field(static value => value.Green, static (value, field) => value.Green = field, FhmWireCodecs.UInt16)
        .Field(static value => value.Blue, static (value, field) => value.Blue = field, FhmWireCodecs.UInt16)
        .Field(static value => value.Reserved, static (value, field) => value.Reserved = field, FixedList(2, FhmWireCodecs.Byte))
        .Build();

    private static readonly RecordCodec<FhmTeamPostUnitData> PostUnit = Record<FhmTeamPostUnitData>()
        .Field(static value => value.Marker1, static (value, field) => value.Marker1 = field, FhmWireCodecs.Byte)
        .Field(static value => value.Marker2, static (value, field) => value.Marker2 = field, FhmWireCodecs.Byte)
        .Field(static value => value.Values, static (value, field) => value.Values = field, FhmWireCodecs.QList(FhmWireCodecs.Int32, nameof(FhmTeamPostUnitData.Values)))
        .Build();

    private static readonly RecordCodec<FhmTeamPostBodyData> PostBody = Record<FhmTeamPostBodyData>()
        .Field(static value => value.OpenRosterSlots, static (value, field) => value.OpenRosterSlots = field, FhmWireCodecs.Int32)
        .Field(static value => value.AllTimePlayers, static (value, field) => value.AllTimePlayers = field, FhmWireCodecs.QList(FhmWireCodecs.Int32, nameof(FhmTeamPostBodyData.AllTimePlayers)))
        .Field(static value => value.PreColourValue, static (value, field) => value.PreColourValue = field, FhmWireCodecs.UInt16)
        .Field(static value => value.PreColourList, static (value, field) => value.PreColourList = field, FhmWireCodecs.QList(FhmWireCodecs.Int32, nameof(FhmTeamPostBodyData.PreColourList)))
        .Field(static value => value.PreColourFlag1, static (value, field) => value.PreColourFlag1 = field, FhmWireCodecs.Byte)
        .Field(static value => value.PreColourValue1, static (value, field) => value.PreColourValue1 = field, FhmWireCodecs.Int32)
        .Field(static value => value.PreColourValue2, static (value, field) => value.PreColourValue2 = field, FhmWireCodecs.Int32)
        .Field(static value => value.PreColourValue3, static (value, field) => value.PreColourValue3 = field, FhmWireCodecs.Int32)
        .Field(static value => value.PreColourValue4, static (value, field) => value.PreColourValue4 = field, FhmWireCodecs.UInt16)
        .Field(static value => value.PreColourValue5, static (value, field) => value.PreColourValue5 = field, FhmWireCodecs.UInt16)
        .Field(static value => value.PreColourFlag2, static (value, field) => value.PreColourFlag2 = field, FhmWireCodecs.Byte)
        .Field(static value => value.PreColourFlag3, static (value, field) => value.PreColourFlag3 = field, FhmWireCodecs.Byte)
        .Field(static value => value.Colours, static (value, field) => value.Colours = field, FixedList(13, Object(Colour)))
        .Field(static value => value.Gap2, static (value, field) => value.Gap2 = field, FixedBytes(41))
        .Field(static value => value.Abbreviation, static (value, field) => value.Abbreviation = field, FhmWireCodecs.QString)
        .Field(static value => value.Pad, static (value, field) => value.Pad = field, FixedBytes(1))
        .Field(static value => value.Units, static (value, field) => value.Units = field, FixedList(4, Object(PostUnit)))
        .Build();

    private static readonly RecordCodec<FhmFixed12BytesData> Fixed12 = Record<FhmFixed12BytesData>()
        .Field(static value => value.Data, static (value, field) => value.Data = field, FixedBytes(12))
        .Build();

    private static readonly RecordCodec<FhmRetiredNumberData> RetiredNumber = Record<FhmRetiredNumberData>()
        .Field(static value => value.Year, static (value, field) => value.Year = field, FhmWireCodecs.UInt16)
        .Field(static value => value.Number, static (value, field) => value.Number = field, FhmWireCodecs.UInt16)
        .Field(static value => value.Flag, static (value, field) => value.Flag = field, FhmWireCodecs.UInt16)
        .Field(static value => value.PlayerReference, static (value, field) => value.PlayerReference = field, FhmWireCodecs.Int32)
        .Build();

    private static readonly RecordCodec<FhmTeamTacticsHeaderData> TacticsHeader = Record<FhmTeamTacticsHeaderData>()
        .Field(static value => value.TeamValue1, static (value, field) => value.TeamValue1 = field, FhmWireCodecs.UInt16)
        .Field(static value => value.TeamFlag, static (value, field) => value.TeamFlag = field, FhmWireCodecs.Byte)
        .Field(static value => value.TeamRating, static (value, field) => value.TeamRating = field, FhmWireCodecs.Byte)
        .Field(static value => value.TeamValues2To4, static (value, field) => value.TeamValues2To4 = field, FixedList(3, FhmWireCodecs.Int32))
        .Field(static value => value.TacticsObjectVersion, static (value, field) => value.TacticsObjectVersion = field, FhmWireCodecs.Int32)
        .Field(static value => value.BaseSettings, static (value, field) => value.BaseSettings = field, FixedList(59, FhmWireCodecs.UInt16))
        .Build();

    private static readonly RecordCodec<FhmZoneSelectorSystemsData> SelectorSystems = Record<FhmZoneSelectorSystemsData>()
        .Field(static value => value.SystemIds, static (value, field) => value.SystemIds = field, FixedList(12, FhmWireCodecs.UInt16))
        .Build();

    private static readonly RecordCodec<FhmZoneSelectorOrientationData> SelectorOrientation =
        Record<FhmZoneSelectorOrientationData>()
            .Field(static value => value.OffensiveOrientation, static (value, field) => value.OffensiveOrientation = field, FhmWireCodecs.UInt16)
            .Field(static value => value.PhysicalOrientation, static (value, field) => value.PhysicalOrientation = field, FhmWireCodecs.UInt16)
            .Build();

    private static readonly RecordCodec<FhmTeamTacticsFooterData> TacticsFooter = Record<FhmTeamTacticsFooterData>()
        .Field(static value => value.FinalOffensiveOrientation, static (value, field) => value.FinalOffensiveOrientation = field, FhmWireCodecs.UInt16)
        .Field(static value => value.FinalPhysicalOrientation, static (value, field) => value.FinalPhysicalOrientation = field, FhmWireCodecs.UInt16)
        .Field(static value => value.FinalUseOwnSettingsFlags, static (value, field) => value.FinalUseOwnSettingsFlags = field, FixedList(2, FhmWireCodecs.Byte))
        .Build();

    private static readonly RecordCodec<FhmTendencyBlockData> Tendency = Record<FhmTendencyBlockData>()
        .Field(static value => value.Values, static (value, field) => value.Values = field, FixedList(8, FhmWireCodecs.UInt16))
        .Field(static value => value.Overrides, static (value, field) => value.Overrides = field, FixedList(8, FhmWireCodecs.Byte))
        .Build();

    private static readonly ValueCodec<FhmTeamTacticsSettingsData> Tactics = new(
        static reader => ReadTactics(reader),
        static (writer, value) => WriteTactics(writer, value));

    private static readonly RecordCodec<FhmJuniorHistoryRecordData> JuniorHistory =
        Record<FhmJuniorHistoryRecordData>()
            .Field(static value => value.Flag, static (value, field) => value.Flag = field, FhmWireCodecs.UInt16)
            .Field(static value => value.Year, static (value, field) => value.Year = field, FhmWireCodecs.UInt16)
            .Field(static value => value.TeamId, static (value, field) => value.TeamId = field, FhmWireCodecs.Int32)
            .Field(static value => value.Pad, static (value, field) => value.Pad = field, FhmWireCodecs.Byte)
            .Build();

    private static readonly RecordCodec<FhmFanHappinessHistoryRecordData> FanHappinessHistory =
        Record<FhmFanHappinessHistoryRecordData>()
            .Field(static value => value.EventType, static (value, field) => value.EventType = field, FhmWireCodecs.UInt16)
            .Field(static value => value.ResultingHappiness, static (value, field) => value.ResultingHappiness = field, FhmWireCodecs.UInt16)
            .Field(static value => value.PlayerReference, static (value, field) => value.PlayerReference = field, FhmWireCodecs.Int32)
            .Field(static value => value.StaffId, static (value, field) => value.StaffId = field, FhmWireCodecs.Int32)
            .Field(static value => value.RelatedTeamRecordIndex, static (value, field) => value.RelatedTeamRecordIndex = field, FhmWireCodecs.Int32)
            .Field(static value => value.CompetitionId, static (value, field) => value.CompetitionId = field, FhmWireCodecs.Int32)
            .Field(static value => value.LeagueId, static (value, field) => value.LeagueId = field, FhmWireCodecs.Int32)
            .Build();

    private static readonly RecordCodec<FhmFixed89BytesData> Fixed89 = Record<FhmFixed89BytesData>()
        .Field(static value => value.Data, static (value, field) => value.Data = field, FixedBytes(89))
        .Build();

    private static readonly RecordCodec<FhmTaggedPlayerIdData> TaggedPlayerId = Record<FhmTaggedPlayerIdData>()
        .Field(static value => value.PlayerReference, static (value, field) => value.PlayerReference = field, FhmWireCodecs.Int32)
        .Field(static value => value.Tag, static (value, field) => value.Tag = field, FhmWireCodecs.Byte)
        .Build();

    private static readonly RecordCodec<FhmTeamTailRestData> TailRest = Record<FhmTeamTailRestData>()
        .Field(static value => value.MajorJuniorHistory, static (value, field) => value.MajorJuniorHistory = field, FhmWireCodecs.QList(Object(JuniorHistory), nameof(FhmTeamTailRestData.MajorJuniorHistory)))
        .Field(static value => value.MainRivalRecordIndex, static (value, field) => value.MainRivalRecordIndex = field, FhmWireCodecs.Int32)
        .Field(static value => value.PotentialRivalRecordIndex, static (value, field) => value.PotentialRivalRecordIndex = field, FhmWireCodecs.Int32)
        .Field(static value => value.PotentialRivalProgress, static (value, field) => value.PotentialRivalProgress = field, FhmWireCodecs.UInt16)
        .Field(static value => value.FanHappinessHistory, static (value, field) => value.FanHappinessHistory = field, FhmWireCodecs.QList(Object(FanHappinessHistory), nameof(FhmTeamTailRestData.FanHappinessHistory)))
        .Field(static value => value.ActiveLineSlotLocks, static (value, field) => value.ActiveLineSlotLocks = field, FixedList(13, FhmWireCodecs.QList(FhmWireCodecs.Byte, "active line slot locks")))
        .Field(static value => value.ManagedDepthCharts, static (value, field) => value.ManagedDepthCharts = field, FixedList(17, FhmWireCodecs.QList(FhmWireCodecs.Int32, "managed depth charts")))
        .Field(static value => value.UnknownInt32_1, static (value, field) => value.UnknownInt32_1 = field, FhmWireCodecs.Int32)
        .Field(static value => value.UnknownInt32_2, static (value, field) => value.UnknownInt32_2 = field, FhmWireCodecs.Int32)
        .Field(static value => value.UnsignedShortLists, static (value, field) => value.UnsignedShortLists = field, FixedList(7, FhmWireCodecs.QList(FhmWireCodecs.UInt16, "unsigned short list")))
        .Field(static value => value.AdditionalPlayerIds, static (value, field) => value.AdditionalPlayerIds = field, FhmWireCodecs.QList(FhmWireCodecs.Int32, nameof(FhmTeamTailRestData.AdditionalPlayerIds)))
        .Field(static value => value.Flag1, static (value, field) => value.Flag1 = field, FhmWireCodecs.Byte)
        .Field(static value => value.Flag2, static (value, field) => value.Flag2 = field, FhmWireCodecs.Byte)
        .Field(static value => value.UnknownInt32_3, static (value, field) => value.UnknownInt32_3 = field, FhmWireCodecs.Int32)
        .Field(static value => value.Flag3, static (value, field) => value.Flag3 = field, FhmWireCodecs.Byte)
        .Field(static value => value.UnknownDouble_1, static (value, field) => value.UnknownDouble_1 = field, FhmWireCodecs.Double)
        .Field(static value => value.UnknownInt32_4, static (value, field) => value.UnknownInt32_4 = field, FhmWireCodecs.Int32)
        .Field(static value => value.UnknownByte_1, static (value, field) => value.UnknownByte_1 = field, FhmWireCodecs.Byte)
        .Field(static value => value.UnknownByte_2, static (value, field) => value.UnknownByte_2 = field, FhmWireCodecs.Byte)
        .Field(static value => value.UnknownByte_3, static (value, field) => value.UnknownByte_3 = field, FhmWireCodecs.Byte)
        .Field(static value => value.UnknownUInt16_1, static (value, field) => value.UnknownUInt16_1 = field, FhmWireCodecs.UInt16)
        .Field(static value => value.UnknownUInt16_2, static (value, field) => value.UnknownUInt16_2 = field, FhmWireCodecs.UInt16)
        .Field(static value => value.UnknownByte_4, static (value, field) => value.UnknownByte_4 = field, FhmWireCodecs.Byte)
        .Field(static value => value.UnknownByte_5, static (value, field) => value.UnknownByte_5 = field, FhmWireCodecs.Byte)
        .Field(static value => value.Flags4To9, static (value, field) => value.Flags4To9 = field, FixedList(6, FhmWireCodecs.Byte))
        .Field(static value => value.UnknownBytes6To9, static (value, field) => value.UnknownBytes6To9 = field, FixedList(4, FhmWireCodecs.Byte))
        .Field(static value => value.UnknownUInt16_3, static (value, field) => value.UnknownUInt16_3 = field, FhmWireCodecs.UInt16)
        .Field(static value => value.UnknownDouble_2, static (value, field) => value.UnknownDouble_2 = field, FhmWireCodecs.Double)
        .Field(static value => value.UnknownInt32_5, static (value, field) => value.UnknownInt32_5 = field, FhmWireCodecs.Int32)
        .Field(static value => value.Flag10, static (value, field) => value.Flag10 = field, FhmWireCodecs.Byte)
        .Field(static value => value.UnknownUInt16_4, static (value, field) => value.UnknownUInt16_4 = field, FhmWireCodecs.UInt16)
        .Field(static value => value.UnknownByte_10, static (value, field) => value.UnknownByte_10 = field, FhmWireCodecs.Byte)
        .Field(static value => value.FinanceCurveRecords, static (value, field) => value.FinanceCurveRecords = field, FhmWireCodecs.QList(Object(Fixed89), nameof(FhmTeamTailRestData.FinanceCurveRecords)))
        .Field(static value => value.UnknownInt32_6, static (value, field) => value.UnknownInt32_6 = field, FhmWireCodecs.Int32)
        .Field(static value => value.UnknownDouble_3, static (value, field) => value.UnknownDouble_3 = field, FhmWireCodecs.Double)
        .Field(static value => value.Flag11, static (value, field) => value.Flag11 = field, FhmWireCodecs.Byte)
        .Field(static value => value.UnknownBytes11To13, static (value, field) => value.UnknownBytes11To13 = field, FixedList(3, FhmWireCodecs.Byte))
        .Field(static value => value.Flag12, static (value, field) => value.Flag12 = field, FhmWireCodecs.Byte)
        .Field(static value => value.NestedPlayerIdLists, static (value, field) => value.NestedPlayerIdLists = field, FhmWireCodecs.QList(FhmWireCodecs.QList(FhmWireCodecs.Int32, "nested player ids"), nameof(FhmTeamTailRestData.NestedPlayerIdLists)))
        .Field(static value => value.Flag13, static (value, field) => value.Flag13 = field, FhmWireCodecs.Byte)
        .Field(static value => value.Flag14, static (value, field) => value.Flag14 = field, FhmWireCodecs.Byte)
        .Field(static value => value.UnknownBytes14To16, static (value, field) => value.UnknownBytes14To16 = field, FixedList(3, FhmWireCodecs.Byte))
        .Field(static value => value.TaggedPlayerIds, static (value, field) => value.TaggedPlayerIds = field, FhmWireCodecs.QList(Object(TaggedPlayerId), nameof(FhmTeamTailRestData.TaggedPlayerIds)))
        .Field(static value => value.ClosingUInt16_1, static (value, field) => value.ClosingUInt16_1 = field, FhmWireCodecs.UInt16)
        .Field(static value => value.ClosingUInt16_2, static (value, field) => value.ClosingUInt16_2 = field, FhmWireCodecs.UInt16)
        .Field(static value => value.ClosingFlag1, static (value, field) => value.ClosingFlag1 = field, FhmWireCodecs.Byte)
        .Field(static value => value.ClosingDouble1, static (value, field) => value.ClosingDouble1 = field, FhmWireCodecs.Double)
        .Field(static value => value.ClosingDouble2, static (value, field) => value.ClosingDouble2 = field, FhmWireCodecs.Double)
        .Field(static value => value.ClosingFlag2, static (value, field) => value.ClosingFlag2 = field, FhmWireCodecs.Byte)
        .Field(static value => value.ClosingDouble3, static (value, field) => value.ClosingDouble3 = field, FhmWireCodecs.Double)
        .Field(static value => value.ClosingDouble4, static (value, field) => value.ClosingDouble4 = field, FhmWireCodecs.Double)
        .Field(static value => value.ClosingFlag3, static (value, field) => value.ClosingFlag3 = field, FhmWireCodecs.Byte)
        .Field(static value => value.ClosingUInt16_3, static (value, field) => value.ClosingUInt16_3 = field, FhmWireCodecs.UInt16)
        .Field(static value => value.ClosingUInt16_4, static (value, field) => value.ClosingUInt16_4 = field, FhmWireCodecs.UInt16)
        .Field(static value => value.ClosingFlag4, static (value, field) => value.ClosingFlag4 = field, FhmWireCodecs.Byte)
        .Field(static value => value.ClosingInt32_1, static (value, field) => value.ClosingInt32_1 = field, FhmWireCodecs.Int32)
        .Field(static value => value.ClosingInt32_2, static (value, field) => value.ClosingInt32_2 = field, FhmWireCodecs.Int32)
        .Field(static value => value.ClosingFlags, static (value, field) => value.ClosingFlags = field, FixedList(9, FhmWireCodecs.Byte))
        .Field(static value => value.ClosingInt32_3, static (value, field) => value.ClosingInt32_3 = field, FhmWireCodecs.Int32)
        .Build();

    private static readonly ValueCodec<FhmTeamTailData> Tail = new(
        static reader => ReadTail(reader),
        static (writer, value) => WriteTail(writer, value));

    internal static FhmTeamsFileData Read(BigEndianBinaryReader reader)
    {
        var result = new FhmTeamsFileData
        {
            VersionTag = reader.ReadInt32(),
            TeamCount = reader.ReadInt32(),
        };
        result.Teams = FhmWireCodecs.ReadCountedList(
            reader,
            result.TeamCount,
            TeamRecord,
            "teams");
        return result;
    }

    internal static void Write(BigEndianBinaryWriter writer, FhmTeamsFileData value)
    {
        writer.WriteInt32(value.VersionTag);
        writer.WriteInt32(value.TeamCount);
        FhmWireCodecs.WriteCountedList(
            writer,
            value.Teams,
            TeamRecord);
    }

    internal static FhmTeamTacticsSettingsData ReadTacticsSettings(BigEndianBinaryReader reader) => ReadTactics(reader);

    internal static void WriteTacticsSettings(BigEndianBinaryWriter writer, FhmTeamTacticsSettingsData value) =>
        WriteTactics(writer, value);

    private static FhmTeamRecordData ReadTeam(BigEndianBinaryReader reader)
    {
        var result = new FhmTeamRecordData
        {
            RecordIndex = reader.ReadInt32(),
            TeamId = reader.ReadInt32(),
            InternalCode = FhmWireCodecs.QString.Read(reader),
            InternalCode2 = FhmWireCodecs.QString.Read(reader),
            Flag1 = reader.ReadByte(),
            City = FhmWireCodecs.QString.Read(reader),
            Nickname = FhmWireCodecs.QString.Read(reader),
            NicknamePlacement = reader.ReadByte(),
            AffiliateParentId = reader.ReadInt32(),
            AffiliateParentId2 = reader.ReadInt32(),
            LeagueId = reader.ReadInt32(),
            ConferenceId = reader.ReadInt32(),
            DivisionId = reader.ReadInt32(),
            LocationId = reader.ReadInt32(),
            MarketSize = reader.ReadUInt16(),
            FanLoyalty = reader.ReadUInt16(),
            Finance1 = reader.ReadInt32(),
            Finance2 = reader.ReadInt32(),
            Finance3 = reader.ReadInt32(),
            UnknownInt32_13 = reader.ReadInt32(),
            UnknownInt32_14 = reader.ReadInt32(),
            Finance4 = reader.ReadInt32(),
            SeasonHistoryCount = reader.ReadInt32(),
        };
        result.SeasonHistory = FhmWireCodecs.ReadCountedList(reader, result.SeasonHistoryCount, Object(TeamSeason), "team season history");
        result.FranchiseHistory = FranchiseHistory.Read(reader);
        result.ActiveLines = ActiveLines.Read(reader);
        result.LeadershipReserve = LeadershipReserve.Read(reader);
        result.SeasonParticipation = SeasonParticipation.Read(reader);
        result.Roster = Roster.Read(reader);
        result.PostHead = PostHead.Read(reader);
        result.PostBody = PostBody.Read(reader);
        result.Tail = Tail.Read(reader);
        return result;
    }

    private static void WriteTeam(BigEndianBinaryWriter writer, FhmTeamRecordData value)
    {
        writer.WriteInt32(value.RecordIndex);
        writer.WriteInt32(value.TeamId);
        FhmWireCodecs.QString.Write(writer, value.InternalCode);
        FhmWireCodecs.QString.Write(writer, value.InternalCode2);
        writer.WriteByte(value.Flag1);
        FhmWireCodecs.QString.Write(writer, value.City);
        FhmWireCodecs.QString.Write(writer, value.Nickname);
        writer.WriteByte(value.NicknamePlacement);
        writer.WriteInt32(value.AffiliateParentId);
        writer.WriteInt32(value.AffiliateParentId2);
        writer.WriteInt32(value.LeagueId);
        writer.WriteInt32(value.ConferenceId);
        writer.WriteInt32(value.DivisionId);
        writer.WriteInt32(value.LocationId);
        writer.WriteUInt16(value.MarketSize);
        writer.WriteUInt16(value.FanLoyalty);
        writer.WriteInt32(value.Finance1);
        writer.WriteInt32(value.Finance2);
        writer.WriteInt32(value.Finance3);
        writer.WriteInt32(value.UnknownInt32_13);
        writer.WriteInt32(value.UnknownInt32_14);
        writer.WriteInt32(value.Finance4);
        writer.WriteInt32(value.SeasonHistoryCount);
        FhmWireCodecs.WriteCountedList(writer, value.SeasonHistory, Object(TeamSeason));
        FranchiseHistory.Write(writer, value.FranchiseHistory);
        ActiveLines.Write(writer, value.ActiveLines);
        LeadershipReserve.Write(writer, value.LeadershipReserve);
        SeasonParticipation.Write(writer, value.SeasonParticipation);
        Roster.Write(writer, value.Roster);
        PostHead.Write(writer, value.PostHead);
        PostBody.Write(writer, value.PostBody);
        Tail.Write(writer, value.Tail);
    }

    private static FhmFranchiseHistoryData ReadFranchiseHistory(BigEndianBinaryReader reader)
    {
        var result = new FhmFranchiseHistoryData
        {
            Head = reader.ReadBytes(115),
            SeasonStatisticCount = reader.ReadInt32(),
            HeadTail = reader.ReadBytes(8),
        };
        result.SeasonStatistics = FhmWireCodecs.ReadCountedList(
            reader,
            result.SeasonStatisticCount,
            Object(FranchiseSeasonStatistic),
            "season statistics");
        result.NameHistoryCount = reader.ReadInt32();
        result.NameHistoryStringCount = reader.ReadInt32();
        result.NameHistory = FhmWireCodecs.ReadCountedList(
            reader,
            result.NameHistoryStringCount,
            FhmWireCodecs.QString,
            "name history");
        result.AbbreviationHistoryCount = reader.ReadInt32();
        result.AbbreviationHistory = FhmWireCodecs.ReadCountedList(
            reader,
            result.AbbreviationHistoryCount,
            FhmWireCodecs.QString,
            "abbreviation history");
        return result;
    }

    private static void WriteFranchiseHistory(BigEndianBinaryWriter writer, FhmFranchiseHistoryData value)
    {
        writer.WriteBytes(value.Head);
        writer.WriteInt32(value.SeasonStatisticCount);
        writer.WriteBytes(value.HeadTail);
        FhmWireCodecs.WriteCountedList(writer, value.SeasonStatistics, Object(FranchiseSeasonStatistic));
        writer.WriteInt32(value.NameHistoryCount);
        writer.WriteInt32(value.NameHistoryStringCount);
        FhmWireCodecs.WriteCountedList(writer, value.NameHistory, FhmWireCodecs.QString);
        writer.WriteInt32(value.AbbreviationHistoryCount);
        FhmWireCodecs.WriteCountedList(writer, value.AbbreviationHistory, FhmWireCodecs.QString);
    }

    private static FhmSeasonParticipationChainData ReadSeasonParticipation(BigEndianBinaryReader reader)
    {
        var result = new FhmSeasonParticipationChainData();
        do
        {
            result.Blocks.Add(ParticipationBlock.Read(reader));
        }
        while (ReadPeekedInt32(reader) != 0);

        return result;
    }

    private static void WriteSeasonParticipation(BigEndianBinaryWriter writer, FhmSeasonParticipationChainData value)
    {
        foreach (var block in value.Blocks)
        {
            ParticipationBlock.Write(writer, block);
        }
    }

    private static FhmRosterChainData ReadRoster(BigEndianBinaryReader reader)
    {
        var result = new FhmRosterChainData();
        while (!System.MemoryExtensions.SequenceEqual<byte>(
                   reader.PeekBytes(3, relativeOffset: 4),
                   PostHeadSignature))
        {
            result.Lists.Add(RosterList.Read(reader));
        }

        return result;
    }

    private static void WriteRoster(BigEndianBinaryWriter writer, FhmRosterChainData value)
    {
        foreach (var item in value.Lists)
        {
            RosterList.Write(writer, item);
        }
    }

    private static FhmTeamTacticsSettingsData ReadTactics(BigEndianBinaryReader reader)
    {
        var header = TacticsHeader.Read(reader);
        var result = new FhmTeamTacticsSettingsData
        {
            TeamValue1 = header.TeamValue1,
            TeamFlag = header.TeamFlag,
            TeamRating = header.TeamRating,
            TeamValues2To4 = header.TeamValues2To4,
            TacticsObjectVersion = header.TacticsObjectVersion,
            BaseSettings = header.BaseSettings,
        };
        for (var index = 0; index < 22; index++)
        {
            result.Selectors.Add(ReadSelector(reader, index));
        }

        var footer = TacticsFooter.Read(reader);
        result.FinalOffensiveOrientation = footer.FinalOffensiveOrientation;
        result.FinalPhysicalOrientation = footer.FinalPhysicalOrientation;
        result.FinalUseOwnSettingsFlags = footer.FinalUseOwnSettingsFlags;
        for (var index = 0; index < 22; index++)
        {
            result.Tendencies.Add(Tendency.Read(reader));
        }

        return result;
    }

    private static void WriteTactics(BigEndianBinaryWriter writer, FhmTeamTacticsSettingsData value)
    {
        TacticsHeader.Write(writer, new FhmTeamTacticsHeaderData
        {
            TeamValue1 = value.TeamValue1,
            TeamFlag = value.TeamFlag,
            TeamRating = value.TeamRating,
            TeamValues2To4 = value.TeamValues2To4,
            TacticsObjectVersion = value.TacticsObjectVersion,
            BaseSettings = value.BaseSettings,
        });
        foreach (var selector in value.Selectors)
        {
            WriteSelector(writer, selector);
        }

        TacticsFooter.Write(writer, new FhmTeamTacticsFooterData
        {
            FinalOffensiveOrientation = value.FinalOffensiveOrientation,
            FinalPhysicalOrientation = value.FinalPhysicalOrientation,
            FinalUseOwnSettingsFlags = value.FinalUseOwnSettingsFlags,
        });
        foreach (var tendency in value.Tendencies)
        {
            Tendency.Write(writer, tendency);
        }
    }

    private static FhmZoneSelectorBlockData ReadSelector(BigEndianBinaryReader reader, int blockIndex)
    {
        var systems = SelectorSystems.Read(reader);
        var result = new FhmZoneSelectorBlockData
        {
            BlockIndex = blockIndex,
            SystemIds = systems.SystemIds,
        };
        if (blockIndex != 21)
        {
            var orientation = SelectorOrientation.Read(reader);
            result.OffensiveOrientation = orientation.OffensiveOrientation;
            result.PhysicalOrientation = orientation.PhysicalOrientation;
        }

        result.DelayedUseOwnSettingsFlags = reader.ReadBytes(GetDelayedFlagCount(blockIndex)).ToList();
        return result;
    }

    private static void WriteSelector(BigEndianBinaryWriter writer, FhmZoneSelectorBlockData value)
    {
        SelectorSystems.Write(writer, new FhmZoneSelectorSystemsData { SystemIds = value.SystemIds });
        if (value.BlockIndex != 21)
        {
            SelectorOrientation.Write(writer, new FhmZoneSelectorOrientationData
            {
                OffensiveOrientation = value.OffensiveOrientation,
                PhysicalOrientation = value.PhysicalOrientation,
            });
        }

        writer.WriteBytes([.. value.DelayedUseOwnSettingsFlags]);
    }

    private static FhmTeamTailData ReadTail(BigEndianBinaryReader reader)
    {
        var result = new FhmTeamTailData
        {
            Pre = reader.ReadBytes(32),
            UnknownMRecordCount = reader.ReadUInt16(),
            UnknownMWordCount = reader.ReadInt32(),
            Pre2Prefix = reader.ReadBytes(139),
            FanHappiness = reader.ReadUInt16(),
            Pre2Suffix = reader.ReadBytes(21),
        };
        result.UnknownMRecords = FhmWireCodecs.ReadCountedList(
            reader,
            result.UnknownMRecordCount,
            Object(Fixed12),
            "unknown M records");
        result.RetiredNumberCount = reader.ReadInt32();
        result.RetiredNumbers = FhmWireCodecs.ReadCountedList(
            reader,
            result.RetiredNumberCount,
            Object(RetiredNumber),
            "retired numbers");
        result.WikiUrl = FhmWireCodecs.QString.Read(reader);
        result.WebsiteUrl = FhmWireCodecs.QString.Read(reader);
        result.Tactics = Tactics.Read(reader);
        result.Rest = TailRest.Read(reader);
        return result;
    }

    private static void WriteTail(BigEndianBinaryWriter writer, FhmTeamTailData value)
    {
        writer.WriteBytes(value.Pre);
        writer.WriteUInt16(value.UnknownMRecordCount);
        writer.WriteInt32(value.UnknownMWordCount);
        writer.WriteBytes(value.Pre2Prefix);
        writer.WriteUInt16(value.FanHappiness);
        writer.WriteBytes(value.Pre2Suffix);
        FhmWireCodecs.WriteCountedList(writer, value.UnknownMRecords, Object(Fixed12));
        writer.WriteInt32(value.RetiredNumberCount);
        FhmWireCodecs.WriteCountedList(writer, value.RetiredNumbers, Object(RetiredNumber));
        FhmWireCodecs.QString.Write(writer, value.WikiUrl);
        FhmWireCodecs.QString.Write(writer, value.WebsiteUrl);
        Tactics.Write(writer, value.Tactics);
        TailRest.Write(writer, value.Rest);
    }

    private static List<QList<T>> ReadFixedQLists<T>(
        BigEndianBinaryReader reader,
        int count,
        ValueCodec<QList<T>> codec)
    {
        var result = new List<QList<T>>(count);
        for (var index = 0; index < count; index++)
        {
            result.Add(codec.Read(reader));
        }

        return result;
    }

    private static void WriteQLists<T>(
        BigEndianBinaryWriter writer,
        IEnumerable<QList<T>> values,
        ValueCodec<QList<T>> codec)
    {
        foreach (var value in values)
        {
            codec.Write(writer, value);
        }
    }

    private static int ReadPeekedInt32(BigEndianBinaryReader reader) =>
        System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(reader.PeekBytes(sizeof(int)));

    private static int GetDelayedFlagCount(int blockIndex) => blockIndex switch
    {
        4 => 4,
        13 => 3,
        6 or 8 or 10 or 15 or 17 or 19 => 2,
        _ => 0,
    };
}
