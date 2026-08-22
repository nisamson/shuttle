using Shuttle.Fhm.Serde.Domain.Binary;
using Shuttle.Fhm.Serde.Domain.Model;

namespace Shuttle.Fhm.Serde.Domain.Files;

/// <summary>A complete self-delimiting <c>teams.dat</c> team record.</summary>
public sealed class FhmTeamRecord
{
    /// <summary>Gets or sets this team's serialized record ordinal.</summary>
    public int RecordIndex { get; set; }
    /// <summary>Gets or sets the team's stable identity.</summary>
    public int TeamId { get; set; }
    public string? InternalCode { get; set; }
    public string? InternalCode2 { get; set; }
    public byte Flag1 { get; set; }
    public string? City { get; set; }
    public string? Nickname { get; set; }
    public byte NicknamePlacement { get; set; }
    public int AffiliateParentId { get; set; }
    public int AffiliateParentId2 { get; set; }
    public int LeagueId { get; set; }
    public int ConferenceId { get; set; }
    public int DivisionId { get; set; }
    public int LocationId { get; set; }
    public ushort MarketSize { get; set; }
    public ushort FanLoyalty { get; set; }
    public int Finance1 { get; set; }
    public int Finance2 { get; set; }
    public int Finance3 { get; set; }
    public int UnknownInt32_13 { get; set; }
    public int UnknownInt32_14 { get; set; }
    public int Finance4 { get; set; }
    public IList<FhmTeamSeasonRecord> SeasonHistory { get; } = [];
    public FhmFranchiseHistory FranchiseHistory { get; set; } = new();
    public FhmActiveLineUnit ActiveLines { get; set; } = new();
    public FhmLeadershipReserve LeadershipReserve { get; set; } = new();
    public FhmSeasonParticipationChain SeasonParticipation { get; set; } = new();
    public FhmRosterChain Roster { get; set; } = new();
    public FhmTeamPostHead PostHead { get; set; } = new();
    public FhmTeamPostBody PostBody { get; set; } = new();
    public FhmTeamTail Tail { get; set; } = new();
}

/// <summary>A season-specific name and statistical snapshot.</summary>
public sealed class FhmTeamSeasonRecord
{
    public int Year { get; set; }
    public string? City { get; set; }
    public string? Nickname { get; set; }
    public string? Abbreviation { get; set; }
    public FhmOpaqueBytes Stats { get; set; } = new(new byte[134]);
}

/// <summary>The fixed and list-valued franchise history block.</summary>
public sealed class FhmFranchiseHistory
{
    public FhmOpaqueBytes Head { get; set; } = new(new byte[115]);
    public FhmOpaqueBytes HeadTail { get; set; } = new(new byte[8]);
    public IList<FhmFranchiseSeasonStatistic> SeasonStatistics { get; } = [];
    /// <summary>Gets or sets the independent name-history scalar retained by the game.</summary>
    public int NameHistoryCount { get; set; }
    public IList<string?> NameHistory { get; } = [];
    public IList<string?> AbbreviationHistory { get; } = [];
}

/// <summary>A fixed franchise season-statistics payload with its documented scalars.</summary>
public sealed class FhmFranchiseSeasonStatistic
{
    public FhmOpaqueBytes Body { get; set; } = new(new byte[108]);
    public byte Flag { get; set; }
    public int Value { get; set; }
    public ushort Year { get; set; }
}

/// <summary>The thirteen active-line player source lists.</summary>
public sealed class FhmActiveLineUnit
{
    public IList<FhmActiveLineSlotList> Lists { get; } =
        Enumerable.Range(0, 13).Select(index => new FhmActiveLineSlotList((FhmLineGroup)index)).ToList();
}

/// <summary>One active-line group and its player source identities.</summary>
public sealed class FhmActiveLineSlotList
{
    public FhmActiveLineSlotList(FhmLineGroup group) => Group = group;
    public FhmLineGroup Group { get; }
    public IList<int> PlayerReferences { get; } = [];
}

/// <summary>Team leadership and reserve-player identities.</summary>
public sealed class FhmLeadershipReserve
{
    public int Captain { get; set; } = FhmNullConstants.Null;
    public int AlternateCaptain1 { get; set; } = FhmNullConstants.Null;
    public int AlternateCaptain2 { get; set; } = FhmNullConstants.Null;
    public IList<int> ReserveSlots { get; } = [];
}

/// <summary>The chained season-participation lists preceding roster lists.</summary>
public sealed class FhmSeasonParticipationChain
{
    public FhmSeasonParticipationChain() => Blocks.Add(new FhmSeasonParticipationBlock());
    public IList<FhmSeasonParticipationBlock> Blocks { get; } = [];
}

/// <summary>A count-prefixed set of season participation records.</summary>
public sealed class FhmSeasonParticipationBlock
{
    public IList<FhmSeasonParticipationRecord> Records { get; } = [];
}

/// <summary>A single competition participation entry.</summary>
public sealed class FhmSeasonParticipationRecord
{
    public ushort SequenceNumber { get; set; }
    public ushort Year { get; set; }
    public int ParticipationId { get; set; }
    public byte Flag { get; set; }
}

/// <summary>The self-delimiting roster ID-list chain.</summary>
public sealed class FhmRosterChain
{
    public IList<IList<int>> Lists { get; } = [[]];
}

/// <summary>The POST head with position split and position-requirements word list.</summary>
public sealed class FhmTeamPostHead
{
    public FhmOpaqueBytes Pre { get; set; } = new([0, 0, 0, 0, 0, 100, 1]);
    public IList<int> UnknownListA { get; } = [];
    public FhmOpaqueBytes Gap { get; set; } = new(new byte[196]);
    public IList<int> Goalies { get; } = [];
    public IList<int> Defensemen { get; } = [];
    public IList<int> Forwards { get; } = [];
    public int RegionId { get; set; } = -1;
    public ushort NationIndex { get; set; } = FhmNullConstants.NullUnsignedShort;
    public FhmPositionRequirementWords PositionRequirements { get; set; } = new();
}

/// <summary>The independent logical and physical position-requirements list counts.</summary>
public sealed class FhmPositionRequirementWords
{
    public ushort LogicalEntryCount { get; set; }
    /// <summary>Gets serialized requirement words; this count is authoritative even when stale.</summary>
    public IList<int> Words { get; } = [];
    /// <summary>Gets whether the stored words can be decoded as the normal position/state pairs.</summary>
    public bool HasConsistentPairs => Words.Count == LogicalEntryCount * 2;
    /// <summary>Returns decoded position/state pairs when the two stored counts are consistent.</summary>
    public IEnumerable<FhmPositionRequirement> GetPairs()
    {
        if (!HasConsistentPairs)
        {
            return [];
        }

        return Enumerable.Range(0, LogicalEntryCount)
            .Select(index => new FhmPositionRequirement(Words[index * 2], Words[(index * 2) + 1]));
    }
}

/// <summary>One normally paired position-requirement word pair.</summary>
public readonly record struct FhmPositionRequirement(int PositionCode, int StateCode);

/// <summary>The POST body with roster state, colours, abbreviation, and unit lists.</summary>
public sealed class FhmTeamPostBody
{
    public int OpenRosterSlots { get; set; } = FhmNullConstants.UnlimitedOpenRosterSlots;
    public IList<int> AllTimePlayers { get; } = [];
    public ushort PreColourValue { get; set; }
    public IList<int> PreColourList { get; } = [];
    public byte PreColourFlag1 { get; set; }
    public int PreColourValue1 { get; set; }
    public int PreColourValue2 { get; set; }
    public int PreColourValue3 { get; set; }
    public ushort PreColourValue4 { get; set; }
    public ushort PreColourValue5 { get; set; }
    public byte PreColourFlag2 { get; set; }
    public byte PreColourFlag3 { get; set; }
    public IList<FhmQColor> Colours { get; } = Enumerable.Range(0, 13).Select(_ => new FhmQColor()).ToList();
    public FhmOpaqueBytes Gap2 { get; set; } = new(new byte[41]);
    public string? Abbreviation { get; set; }
    public FhmOpaqueBytes Pad { get; set; } = new(new byte[1]);
    public IList<FhmTeamPostUnit> Units { get; } = Enumerable.Range(0, 4).Select(_ => new FhmTeamPostUnit()).ToList();
}

/// <summary>A QColor in the team colour palette.</summary>
public sealed class FhmQColor
{
    public byte Spec { get; set; }
    public ushort Alpha { get; set; }
    public ushort Red { get; set; }
    public ushort Green { get; set; }
    public ushort Blue { get; set; }
}

/// <summary>One fixed-marker post-body unit list.</summary>
public sealed class FhmTeamPostUnit
{
    public IList<int> Values { get; } = [];
}

/// <summary>The team tail preceding and following tactical settings.</summary>
public sealed class FhmTeamTail
{
    private const int HeadCoachPersonnelIdOffset = 0x0B;
    private const int GeneralManagerPersonnelIdOffset = 0x0F;

    public FhmOpaqueBytes Pre { get; set; } = new(new byte[32]);
    /// <summary>Gets opaque 12-byte records whose validated word count is three times this count.</summary>
    public IList<FhmOpaqueBytes> UnknownMRecords { get; } = [];
    public FhmOpaqueBytes Pre2Prefix { get; set; } = new(new byte[139]);
    public int HeadCoachPersonnelId
    {
        get => ReadPre2PrefixInt32(HeadCoachPersonnelIdOffset);
        set => WritePre2PrefixInt32(HeadCoachPersonnelIdOffset, value);
    }
    public int GeneralManagerPersonnelId
    {
        get => ReadPre2PrefixInt32(GeneralManagerPersonnelIdOffset);
        set => WritePre2PrefixInt32(GeneralManagerPersonnelIdOffset, value);
    }
    /// <summary>Gets or sets current fan happiness on the 1..100 scale when <see cref="UnknownMRecords"/> is empty.</summary>
    public ushort FanHappiness { get; set; }
    public FhmOpaqueBytes Pre2Suffix { get; set; } = new(new byte[21]);
    public IList<FhmRetiredNumber> RetiredNumbers { get; } = [];
    public string? WikiUrl { get; set; }
    public string? WebsiteUrl { get; set; }
    public FhmTeamTacticsSettings Tactics { get; set; } = new();
    public FhmTeamTailRest Rest { get; set; } = new();

    private int ReadPre2PrefixInt32(int offset) =>
        System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(Pre2Prefix.Value.AsSpan(offset, sizeof(int)));

    private void WritePre2PrefixInt32(int offset, int value) =>
        System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(Pre2Prefix.Value.AsSpan(offset, sizeof(int)), value);
}

/// <summary>A retired-number record.</summary>
public sealed class FhmRetiredNumber
{
    public ushort Year { get; set; }
    public ushort Number { get; set; }
    public ushort Flag { get; set; }
    public int PlayerReference { get; set; } = FhmNullConstants.Null;
}

/// <summary>The structurally complete team tail after tactical settings.</summary>
public sealed class FhmTeamTailRest
{
    public IList<FhmJuniorHistoryRecord> MajorJuniorHistory { get; } = [];
    public int MainRivalRecordIndex { get; set; }
    public int PotentialRivalRecordIndex { get; set; }
    public ushort PotentialRivalProgress { get; set; }
    public IList<FhmFanHappinessHistoryRecord> FanHappinessHistory { get; } = [];
    public IList<FhmActiveLineSlotLockList> ActiveLineSlotLocks { get; } =
        Enumerable.Range(0, 13).Select(index => new FhmActiveLineSlotLockList((FhmLineGroup)index)).ToList();
    public IList<FhmManagedDepthChart> ManagedDepthCharts { get; } =
        Enum.GetValues<FhmManagedDepthChartRole>().Select(role => new FhmManagedDepthChart(role)).ToList();
    public int UnknownInt32_1 { get; set; }
    public int UnknownInt32_2 { get; set; }
    public IList<IList<ushort>> UnsignedShortLists { get; } = Enumerable.Range(0, 7).Select(_ => (IList<ushort>)new List<ushort>()).ToList();
    public IList<int> AdditionalPlayerIds { get; } = [];
    public byte Flag1 { get; set; }
    public byte Flag2 { get; set; }
    public int UnknownInt32_3 { get; set; }
    public byte Flag3 { get; set; }
    public double UnknownDouble_1 { get; set; }
    public int UnknownInt32_4 { get; set; }
    public byte UnknownByte_1 { get; set; }
    public byte UnknownByte_2 { get; set; }
    public byte UnknownByte_3 { get; set; }
    public ushort UnknownUInt16_1 { get; set; }
    public ushort UnknownUInt16_2 { get; set; }
    public byte UnknownByte_4 { get; set; }
    public byte UnknownByte_5 { get; set; }
    public byte[] Flags4To9 { get; } = new byte[6];
    public byte[] UnknownBytes6To9 { get; } = new byte[4];
    public ushort UnknownUInt16_3 { get; set; }
    public double UnknownDouble_2 { get; set; }
    public int UnknownInt32_5 { get; set; }
    public byte Flag10 { get; set; }
    public ushort UnknownUInt16_4 { get; set; }
    public byte UnknownByte_10 { get; set; }
    public IList<FhmOpaqueBytes> FinanceCurveRecords { get; } = [];
    public int UnknownInt32_6 { get; set; }
    public double UnknownDouble_3 { get; set; }
    public byte Flag11 { get; set; }
    public byte[] UnknownBytes11To13 { get; } = new byte[3];
    public byte Flag12 { get; set; }
    public IList<IList<int>> NestedPlayerIdLists { get; } = [];
    public byte Flag13 { get; set; }
    public byte Flag14 { get; set; }
    public byte[] UnknownBytes14To16 { get; } = new byte[3];
    public IList<FhmTaggedPlayerId> TaggedPlayerIds { get; } = [];
    public ushort ClosingUInt16_1 { get; set; }
    public ushort ClosingUInt16_2 { get; set; }
    public byte ClosingFlag1 { get; set; }
    public double ClosingDouble1 { get; set; }
    public double ClosingDouble2 { get; set; }
    public byte ClosingFlag2 { get; set; }
    public double ClosingDouble3 { get; set; }
    public double ClosingDouble4 { get; set; }
    public byte ClosingFlag3 { get; set; }
    public ushort ClosingUInt16_3 { get; set; }
    public ushort ClosingUInt16_4 { get; set; }
    public byte ClosingFlag4 { get; set; }
    public int ClosingInt32_1 { get; set; }
    public int ClosingInt32_2 { get; set; }
    public byte[] ClosingFlags { get; } = new byte[9];
    public int ClosingInt32_3 { get; set; }
}

/// <summary>A major-junior team history entry.</summary>
public sealed class FhmJuniorHistoryRecord
{
    public ushort Flag { get; set; }
    public ushort Year { get; set; }
    public int TeamId { get; set; }
    public byte Pad { get; set; }
}

/// <summary>A fan-happiness adjustment and optional related references.</summary>
public sealed class FhmFanHappinessHistoryRecord
{
    public FhmEnumValue<FhmFanHappinessEvent> EventType { get; set; }
    public ushort ResultingHappiness { get; set; }
    public int PlayerReference { get; set; } = FhmNullConstants.Null;
    public int StaffId { get; set; }
    public int RelatedTeamRecordIndex { get; set; }
    public int CompetitionId { get; set; }
    public int LeagueId { get; set; }
}

/// <summary>Slot-lock values for one active-line group, preserving all byte values.</summary>
public sealed class FhmActiveLineSlotLockList
{
    public FhmActiveLineSlotLockList(FhmLineGroup group) => Group = group;
    public FhmLineGroup Group { get; }
    public IList<byte> Values { get; } = [];
}

/// <summary>A managed Game Lineup positional depth chart.</summary>
public sealed class FhmManagedDepthChart
{
    public FhmManagedDepthChart(FhmManagedDepthChartRole role) => Role = role;
    public FhmManagedDepthChartRole Role { get; }
    public IList<int> PlayerReferences { get; } = [];
}

/// <summary>Serialized order and role of the persistent managed-team depth charts.</summary>
public enum FhmManagedDepthChartRole
{
    EvenStrengthLeftWings,
    EvenStrengthCentres,
    EvenStrengthRightWings,
    EvenStrengthLeftDefence,
    EvenStrengthRightDefence,
    Goalies,
    ExtraAttackers,
    ShootoutOrder,
    PowerPlayLeftWings,
    PowerPlayCentres,
    PowerPlayRightWings,
    PowerPlayLeftDefence,
    PowerPlayRightDefence,
    PenaltyKillForwardOne,
    PenaltyKillForwardTwo,
    PenaltyKillLeftDefence,
    PenaltyKillRightDefence,
}

/// <summary>A five-byte player reference with an unclassified tag.</summary>
public readonly record struct FhmTaggedPlayerId(int PlayerReference, byte Tag);
