using Shuttle.Fhm.SaveData.Binary;
using Shuttle.Fhm.SaveData.Model;

namespace Shuttle.Fhm.SaveData.Files;

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

    internal static FhmTeamRecord Read(FhmBinaryReader reader)
    {
        var result = new FhmTeamRecord
        {
            RecordIndex = reader.ReadInt32(),
            TeamId = reader.ReadInt32(),
            InternalCode = reader.ReadQString(),
            InternalCode2 = reader.ReadQString(),
            Flag1 = reader.ReadByte(),
            City = reader.ReadQString(),
            Nickname = reader.ReadQString(),
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
        };

        var seasonCount = reader.ReadCount("team season history");
        for (var index = 0; index < seasonCount; index++)
        {
            result.SeasonHistory.Add(FhmTeamSeasonRecord.Read(reader));
        }

        result.FranchiseHistory = FhmFranchiseHistory.Read(reader);
        result.ActiveLines = FhmActiveLineUnit.Read(reader);
        result.LeadershipReserve = FhmLeadershipReserve.Read(reader);
        result.SeasonParticipation = FhmSeasonParticipationChain.Read(reader);
        result.Roster = FhmRosterChain.Read(reader);
        result.PostHead = FhmTeamPostHead.Read(reader);
        result.PostBody = FhmTeamPostBody.Read(reader);
        result.Tail = FhmTeamTail.Read(reader);
        return result;
    }

    internal void WriteTo(FhmBinaryWriter writer)
    {
        writer.WriteInt32(RecordIndex);
        writer.WriteInt32(TeamId);
        writer.WriteQString(InternalCode);
        writer.WriteQString(InternalCode2);
        writer.WriteByte(Flag1);
        writer.WriteQString(City);
        writer.WriteQString(Nickname);
        writer.WriteByte(NicknamePlacement);
        writer.WriteInt32(AffiliateParentId);
        writer.WriteInt32(AffiliateParentId2);
        writer.WriteInt32(LeagueId);
        writer.WriteInt32(ConferenceId);
        writer.WriteInt32(DivisionId);
        writer.WriteInt32(LocationId);
        writer.WriteUInt16(MarketSize);
        writer.WriteUInt16(FanLoyalty);
        writer.WriteInt32(Finance1);
        writer.WriteInt32(Finance2);
        writer.WriteInt32(Finance3);
        writer.WriteInt32(UnknownInt32_13);
        writer.WriteInt32(UnknownInt32_14);
        writer.WriteInt32(Finance4);
        writer.WriteCount(SeasonHistory.Count, "team season history");
        foreach (var season in SeasonHistory)
        {
            season.WriteTo(writer);
        }

        FranchiseHistory.WriteTo(writer);
        ActiveLines.WriteTo(writer);
        LeadershipReserve.WriteTo(writer);
        SeasonParticipation.WriteTo(writer);
        Roster.WriteTo(writer, PostHead);
        PostHead.WriteTo(writer);
        PostBody.WriteTo(writer);
        Tail.WriteTo(writer);
    }
}

/// <summary>A season-specific name and statistical snapshot.</summary>
public sealed class FhmTeamSeasonRecord
{
    public int Year { get; set; }

    public string? City { get; set; }

    public string? Nickname { get; set; }

    public string? Abbreviation { get; set; }

    public FhmOpaqueBytes Stats { get; set; } = FhmTeamBinary.Opaque(134);

    internal static FhmTeamSeasonRecord Read(FhmBinaryReader reader) => new()
    {
        Year = reader.ReadInt32(),
        City = reader.ReadQString(),
        Nickname = reader.ReadQString(),
        Abbreviation = reader.ReadQString(),
        Stats = reader.ReadOpaqueBytes(134),
    };

    internal void WriteTo(FhmBinaryWriter writer)
    {
        writer.WriteInt32(Year);
        writer.WriteQString(City);
        writer.WriteQString(Nickname);
        writer.WriteQString(Abbreviation);
        FhmTeamBinary.WriteOpaque(writer, Stats, 134, nameof(Stats));
    }
}

/// <summary>The fixed and list-valued franchise history block.</summary>
public sealed class FhmFranchiseHistory
{
    public FhmOpaqueBytes Head { get; set; } = FhmTeamBinary.Opaque(115);

    public FhmOpaqueBytes HeadTail { get; set; } = FhmTeamBinary.Opaque(8);

    public IList<FhmFranchiseSeasonStatistic> SeasonStatistics { get; } = [];

    /// <summary>Gets or sets the independent name-history scalar retained by the game.</summary>
    public int NameHistoryCount { get; set; }

    public IList<string?> NameHistory { get; } = [];

    public IList<string?> AbbreviationHistory { get; } = [];

    internal static FhmFranchiseHistory Read(FhmBinaryReader reader)
    {
        var result = new FhmFranchiseHistory
        {
            Head = reader.ReadOpaqueBytes(115),
        };
        var seasonStatisticCount = reader.ReadCount("franchise season statistics");
        result.HeadTail = reader.ReadOpaqueBytes(8);
        for (var index = 0; index < seasonStatisticCount; index++)
        {
            result.SeasonStatistics.Add(FhmFranchiseSeasonStatistic.Read(reader));
        }

        result.NameHistoryCount = reader.ReadInt32();
        var nameHistoryStringCount = reader.ReadCount("franchise name history strings");
        for (var index = 0; index < nameHistoryStringCount; index++)
        {
            result.NameHistory.Add(reader.ReadQString());
        }

        var abbreviationHistoryCount = reader.ReadCount("franchise abbreviation history");
        for (var index = 0; index < abbreviationHistoryCount; index++)
        {
            result.AbbreviationHistory.Add(reader.ReadQString());
        }

        return result;
    }

    internal void WriteTo(FhmBinaryWriter writer)
    {
        FhmTeamBinary.WriteOpaque(writer, Head, 115, nameof(Head));
        writer.WriteCount(SeasonStatistics.Count, "franchise season statistics");
        FhmTeamBinary.WriteOpaque(writer, HeadTail, 8, nameof(HeadTail));
        foreach (var statistic in SeasonStatistics)
        {
            statistic.WriteTo(writer);
        }

        writer.WriteInt32(NameHistoryCount);
        writer.WriteCount(NameHistory.Count, "franchise name history strings");
        foreach (var name in NameHistory)
        {
            writer.WriteQString(name);
        }

        writer.WriteCount(AbbreviationHistory.Count, "franchise abbreviation history");
        foreach (var abbreviation in AbbreviationHistory)
        {
            writer.WriteQString(abbreviation);
        }
    }
}

/// <summary>A fixed franchise season-statistics payload with its documented scalars.</summary>
public sealed class FhmFranchiseSeasonStatistic
{
    public FhmOpaqueBytes Body { get; set; } = FhmTeamBinary.Opaque(108);

    public byte Flag { get; set; }

    public int Value { get; set; }

    public ushort Year { get; set; }

    internal static FhmFranchiseSeasonStatistic Read(FhmBinaryReader reader) => new()
    {
        Body = reader.ReadOpaqueBytes(108),
        Flag = reader.ReadByte(),
        Value = reader.ReadInt32(),
        Year = reader.ReadUInt16(),
    };

    internal void WriteTo(FhmBinaryWriter writer)
    {
        FhmTeamBinary.WriteOpaque(writer, Body, 108, nameof(Body));
        writer.WriteByte(Flag);
        writer.WriteInt32(Value);
        writer.WriteUInt16(Year);
    }
}

/// <summary>The thirteen active-line player source lists.</summary>
public sealed class FhmActiveLineUnit
{
    public IList<FhmActiveLineSlotList> Lists { get; } = CreateLists();

    internal static FhmActiveLineUnit Read(FhmBinaryReader reader)
    {
        var result = new FhmActiveLineUnit();
        foreach (var list in result.Lists)
        {
            FhmTeamBinary.ReadPlayerReferences(reader, list.PlayerReferences, "active line slots");
        }

        return result;
    }

    internal void WriteTo(FhmBinaryWriter writer)
    {
        FhmTeamBinary.RequireCount(Lists, 13, "active line lists");
        foreach (var list in Lists)
        {
            FhmTeamBinary.WritePlayerReferences(writer, list.PlayerReferences, "active line slots");
        }
    }

    private static IList<FhmActiveLineSlotList> CreateLists()
    {
        var result = new List<FhmActiveLineSlotList>(13);
        for (var index = 0; index < 13; index++)
        {
            result.Add(new FhmActiveLineSlotList((FhmLineGroup)index));
        }

        return result;
    }
}

/// <summary>One active-line group and its player source identities.</summary>
public sealed class FhmActiveLineSlotList
{
    public FhmActiveLineSlotList(FhmLineGroup group) => Group = group;

    public FhmLineGroup Group { get; }

    public IList<FhmPlayerInternalIdentity> PlayerReferences { get; } = [];
}

/// <summary>Team leadership and reserve-player identities.</summary>
public sealed class FhmLeadershipReserve
{
    public FhmPlayerInternalIdentity Captain { get; set; }

    public FhmPlayerInternalIdentity AlternateCaptain1 { get; set; }

    public FhmPlayerInternalIdentity AlternateCaptain2 { get; set; }

    public IList<FhmPlayerInternalIdentity> ReserveSlots { get; } = [];

    internal static FhmLeadershipReserve Read(FhmBinaryReader reader)
    {
        var result = new FhmLeadershipReserve
        {
            Captain = new FhmPlayerInternalIdentity(reader.ReadInt32()),
            AlternateCaptain1 = new FhmPlayerInternalIdentity(reader.ReadInt32()),
            AlternateCaptain2 = new FhmPlayerInternalIdentity(reader.ReadInt32()),
        };
        FhmTeamBinary.ReadPlayerReferences(reader, result.ReserveSlots, "team reserve slots");
        return result;
    }

    internal void WriteTo(FhmBinaryWriter writer)
    {
        writer.WriteInt32(Captain.Value);
        writer.WriteInt32(AlternateCaptain1.Value);
        writer.WriteInt32(AlternateCaptain2.Value);
        FhmTeamBinary.WritePlayerReferences(writer, ReserveSlots, "team reserve slots");
    }
}

/// <summary>The chained season-participation lists preceding roster lists.</summary>
public sealed class FhmSeasonParticipationChain
{
    public FhmSeasonParticipationChain() => Blocks.Add(new FhmSeasonParticipationBlock());

    public IList<FhmSeasonParticipationBlock> Blocks { get; } = [];

    internal static FhmSeasonParticipationChain Read(FhmBinaryReader reader)
    {
        var result = new FhmSeasonParticipationChain();
        result.Blocks.Clear();
        do
        {
            result.Blocks.Add(FhmSeasonParticipationBlock.Read(reader));
        }
        while (reader.PeekInt32() != 0);

        return result;
    }

    internal void WriteTo(FhmBinaryWriter writer)
    {
        if (Blocks.Count == 0)
        {
            throw new FhmFormatException("Season participation chain requires its terminating zero-count block.");
        }

        foreach (var block in Blocks)
        {
            block.WriteTo(writer);
        }
    }
}

/// <summary>A count-prefixed set of season participation records.</summary>
public sealed class FhmSeasonParticipationBlock
{
    public IList<FhmSeasonParticipationRecord> Records { get; } = [];

    internal static FhmSeasonParticipationBlock Read(FhmBinaryReader reader)
    {
        var result = new FhmSeasonParticipationBlock();
        var count = reader.ReadCount("season participation records");
        for (var index = 0; index < count; index++)
        {
            result.Records.Add(FhmSeasonParticipationRecord.Read(reader));
        }

        return result;
    }

    internal void WriteTo(FhmBinaryWriter writer)
    {
        writer.WriteCount(Records.Count, "season participation records");
        foreach (var record in Records)
        {
            record.WriteTo(writer);
        }
    }
}

/// <summary>A single competition participation entry.</summary>
public sealed class FhmSeasonParticipationRecord
{
    public ushort SequenceNumber { get; set; }

    public ushort Year { get; set; }

    public int ParticipationId { get; set; }

    public byte Flag { get; set; }

    internal static FhmSeasonParticipationRecord Read(FhmBinaryReader reader) => new()
    {
        SequenceNumber = reader.ReadUInt16(),
        Year = reader.ReadUInt16(),
        ParticipationId = reader.ReadInt32(),
        Flag = reader.ReadByte(),
    };

    internal void WriteTo(FhmBinaryWriter writer)
    {
        writer.WriteUInt16(SequenceNumber);
        writer.WriteUInt16(Year);
        writer.WriteInt32(ParticipationId);
        writer.WriteByte(Flag);
    }
}

/// <summary>The self-delimiting roster ID-list chain.</summary>
public sealed class FhmRosterChain
{
    public IList<IList<int>> Lists { get; } = [[]];

    internal static FhmRosterChain Read(FhmBinaryReader reader)
    {
        var result = new FhmRosterChain();
        result.Lists.Clear();
        do
        {
            var list = new List<int>();
            FhmTeamBinary.ReadIntList(reader, list, "team roster list");
            result.Lists.Add(list);
        }
        while (!reader.PeekBytes(3, 4).SequenceEqual(FhmTeamBinary.RosterPostSignature));

        return result;
    }

    internal void WriteTo(FhmBinaryWriter writer, FhmTeamPostHead postHead)
    {
        if (Lists.Count == 0)
        {
            throw new FhmFormatException("Team roster chain requires at least one list.");
        }

        if (Lists[0].Count != 0)
        {
            throw new FhmFormatException("Team roster chains must begin with the empty participation delimiter list.");
        }

        foreach (var list in Lists)
        {
            FhmTeamBinary.WriteIntList(writer, list, "team roster list");
        }

        if (postHead.Pre.Value.Length != 7 ||
            !postHead.Pre.Value.AsSpan(4, 3).SequenceEqual(FhmTeamBinary.RosterPostSignature))
        {
            throw new FhmFormatException("Team post-head prefix must retain the roster-chain termination signature.");
        }
    }
}

/// <summary>The POST head with position split and position-requirements word list.</summary>
public sealed class FhmTeamPostHead
{
    public FhmOpaqueBytes Pre { get; set; } = new([0, 0, 0, 0, 0, 100, 1]);

    public IList<int> UnknownListA { get; } = [];

    public FhmOpaqueBytes Gap { get; set; } = FhmTeamBinary.Opaque(196);

    public IList<FhmPlayerInternalIdentity> Goalies { get; } = [];

    public IList<FhmPlayerInternalIdentity> Defensemen { get; } = [];

    public IList<FhmPlayerInternalIdentity> Forwards { get; } = [];

    public int RegionId { get; set; } = -1;

    public ushort NationIndex { get; set; } = FhmReferences.UnsetNationIndex;

    public FhmPositionRequirementWords PositionRequirements { get; set; } = new();

    internal static FhmTeamPostHead Read(FhmBinaryReader reader)
    {
        var result = new FhmTeamPostHead
        {
            Pre = reader.ReadOpaqueBytes(7),
        };
        FhmTeamBinary.ReadIntList(reader, result.UnknownListA, "team post-head unknown list");
        result.Gap = reader.ReadOpaqueBytes(196);
        FhmTeamBinary.ReadPlayerReferences(reader, result.Goalies, "team goalie list");
        FhmTeamBinary.ReadPlayerReferences(reader, result.Defensemen, "team defensemen list");
        FhmTeamBinary.ReadPlayerReferences(reader, result.Forwards, "team forwards list");
        result.RegionId = reader.ReadInt32();
        result.NationIndex = reader.ReadUInt16();
        result.PositionRequirements = FhmPositionRequirementWords.Read(reader);
        return result;
    }

    internal void WriteTo(FhmBinaryWriter writer)
    {
        FhmTeamBinary.WriteOpaque(writer, Pre, 7, nameof(Pre));
        FhmTeamBinary.WriteIntList(writer, UnknownListA, "team post-head unknown list");
        FhmTeamBinary.WriteOpaque(writer, Gap, 196, nameof(Gap));
        FhmTeamBinary.WritePlayerReferences(writer, Goalies, "team goalie list");
        FhmTeamBinary.WritePlayerReferences(writer, Defensemen, "team defensemen list");
        FhmTeamBinary.WritePlayerReferences(writer, Forwards, "team forwards list");
        writer.WriteInt32(RegionId);
        writer.WriteUInt16(NationIndex);
        PositionRequirements.WriteTo(writer);
    }
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

        var pairs = new FhmPositionRequirement[LogicalEntryCount];
        for (var index = 0; index < pairs.Length; index++)
        {
            pairs[index] = new FhmPositionRequirement(Words[index * 2], Words[(index * 2) + 1]);
        }

        return pairs;
    }

    internal static FhmPositionRequirementWords Read(FhmBinaryReader reader)
    {
        var result = new FhmPositionRequirementWords { LogicalEntryCount = reader.ReadUInt16() };
        FhmTeamBinary.ReadIntList(reader, result.Words, "position requirement words");
        return result;
    }

    internal void WriteTo(FhmBinaryWriter writer)
    {
        writer.WriteUInt16(LogicalEntryCount);
        FhmTeamBinary.WriteIntList(writer, Words, "position requirement words");
    }
}

/// <summary>One normally paired position-requirement word pair.</summary>
public readonly record struct FhmPositionRequirement(int PositionCode, int StateCode);

/// <summary>The POST body with roster state, colours, abbreviation, and unit lists.</summary>
public sealed class FhmTeamPostBody
{
    public int OpenRosterSlots { get; set; } = FhmReferences.UnlimitedOpenRosterSlots;

    public IList<FhmPlayerInternalIdentity> AllTimePlayers { get; } = [];

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

    public IList<FhmQColor> Colours { get; } = FhmTeamBinary.CreateColours();

    public FhmOpaqueBytes Gap2 { get; set; } = FhmTeamBinary.Opaque(41);

    public string? Abbreviation { get; set; }

    public FhmOpaqueBytes Pad { get; set; } = FhmTeamBinary.Opaque(1);

    public IList<FhmTeamPostUnit> Units { get; } = FhmTeamBinary.CreatePostUnits();

    internal static FhmTeamPostBody Read(FhmBinaryReader reader)
    {
        var result = new FhmTeamPostBody
        {
            OpenRosterSlots = reader.ReadInt32(),
        };
        FhmTeamBinary.ReadPlayerReferences(reader, result.AllTimePlayers, "all-time player list");
        result.PreColourValue = reader.ReadUInt16();
        FhmTeamBinary.ReadIntList(reader, result.PreColourList, "pre-colour list");
        result.PreColourFlag1 = reader.ReadByte();
        result.PreColourValue1 = reader.ReadInt32();
        result.PreColourValue2 = reader.ReadInt32();
        result.PreColourValue3 = reader.ReadInt32();
        result.PreColourValue4 = reader.ReadUInt16();
        result.PreColourValue5 = reader.ReadUInt16();
        result.PreColourFlag2 = reader.ReadByte();
        result.PreColourFlag3 = reader.ReadByte();
        for (var index = 0; index < result.Colours.Count; index++)
        {
            result.Colours[index] = FhmQColor.Read(reader);
        }

        result.Gap2 = reader.ReadOpaqueBytes(41);
        result.Abbreviation = reader.ReadQString();
        result.Pad = reader.ReadOpaqueBytes(1);
        for (var index = 0; index < result.Units.Count; index++)
        {
            result.Units[index] = FhmTeamPostUnit.Read(reader);
        }

        return result;
    }

    internal void WriteTo(FhmBinaryWriter writer)
    {
        FhmTeamBinary.RequireCount(Colours, 13, "team colours");
        FhmTeamBinary.RequireCount(Units, 4, "team post units");
        writer.WriteInt32(OpenRosterSlots);
        FhmTeamBinary.WritePlayerReferences(writer, AllTimePlayers, "all-time player list");
        writer.WriteUInt16(PreColourValue);
        FhmTeamBinary.WriteIntList(writer, PreColourList, "pre-colour list");
        writer.WriteByte(PreColourFlag1);
        writer.WriteInt32(PreColourValue1);
        writer.WriteInt32(PreColourValue2);
        writer.WriteInt32(PreColourValue3);
        writer.WriteUInt16(PreColourValue4);
        writer.WriteUInt16(PreColourValue5);
        writer.WriteByte(PreColourFlag2);
        writer.WriteByte(PreColourFlag3);
        foreach (var colour in Colours)
        {
            colour.WriteTo(writer);
        }

        FhmTeamBinary.WriteOpaque(writer, Gap2, 41, nameof(Gap2));
        writer.WriteQString(Abbreviation);
        FhmTeamBinary.WriteOpaque(writer, Pad, 1, nameof(Pad));
        foreach (var unit in Units)
        {
            unit.WriteTo(writer);
        }
    }
}

/// <summary>A QColor in the team colour palette.</summary>
public sealed class FhmQColor
{
    public byte Spec { get; set; }

    public ushort Alpha { get; set; }

    public ushort Red { get; set; }

    public ushort Green { get; set; }

    public ushort Blue { get; set; }

    internal static FhmQColor Read(FhmBinaryReader reader)
    {
        var result = new FhmQColor
        {
            Spec = reader.ReadByte(),
            Alpha = reader.ReadUInt16(),
            Red = reader.ReadUInt16(),
            Green = reader.ReadUInt16(),
            Blue = reader.ReadUInt16(),
        };
        FhmTeamBinary.RequireContents(reader, [0, 0], "QColor reserved bytes");
        return result;
    }

    internal void WriteTo(FhmBinaryWriter writer)
    {
        writer.WriteByte(Spec);
        writer.WriteUInt16(Alpha);
        writer.WriteUInt16(Red);
        writer.WriteUInt16(Green);
        writer.WriteUInt16(Blue);
        writer.WriteByte(0);
        writer.WriteByte(0);
    }
}

/// <summary>One fixed-marker post-body unit list.</summary>
public sealed class FhmTeamPostUnit
{
    public IList<int> Values { get; } = [];

    internal static FhmTeamPostUnit Read(FhmBinaryReader reader)
    {
        FhmTeamBinary.RequireContents(reader, [0, 10], "team post-unit marker");
        var result = new FhmTeamPostUnit();
        FhmTeamBinary.ReadIntList(reader, result.Values, "team post-unit list");
        return result;
    }

    internal void WriteTo(FhmBinaryWriter writer)
    {
        writer.WriteByte(0);
        writer.WriteByte(10);
        FhmTeamBinary.WriteIntList(writer, Values, "team post-unit list");
    }
}

/// <summary>The team tail preceding and following tactical settings.</summary>
public sealed class FhmTeamTail
{
    public FhmOpaqueBytes Pre { get; set; } = FhmTeamBinary.Opaque(32);

    /// <summary>Gets opaque 12-byte records whose validated word count is three times this count.</summary>
    public IList<FhmOpaqueBytes> UnknownMRecords { get; } = [];

    public FhmOpaqueBytes Pre2Prefix { get; set; } = FhmTeamBinary.Opaque(139);

    /// <summary>Gets or sets current fan happiness on the 1..100 scale when <see cref="UnknownMRecords"/> is empty.</summary>
    public ushort FanHappiness { get; set; }

    public FhmOpaqueBytes Pre2Suffix { get; set; } = FhmTeamBinary.Opaque(21);

    public IList<FhmRetiredNumber> RetiredNumbers { get; } = [];

    public string? WikiUrl { get; set; }

    public string? WebsiteUrl { get; set; }

    public FhmTeamTacticsSettings Tactics { get; set; } = new();

    public FhmTeamTailRest Rest { get; set; } = new();

    internal static FhmTeamTail Read(FhmBinaryReader reader)
    {
        var result = new FhmTeamTail
        {
            Pre = reader.ReadOpaqueBytes(32),
        };
        var unknownMCount = reader.ReadUInt16();
        var unknownMWordCount = reader.ReadInt32();
        if (unknownMWordCount != unknownMCount * 3)
        {
            throw new FhmFormatException($"Team tail unknown M word count {unknownMWordCount} does not equal three times record count {unknownMCount}.");
        }

        result.Pre2Prefix = reader.ReadOpaqueBytes(139);
        result.FanHappiness = reader.ReadUInt16();
        result.Pre2Suffix = reader.ReadOpaqueBytes(21);
        for (var index = 0; index < unknownMCount; index++)
        {
            result.UnknownMRecords.Add(reader.ReadOpaqueBytes(12));
        }

        var retiredCount = reader.ReadCount("retired numbers");
        for (var index = 0; index < retiredCount; index++)
        {
            result.RetiredNumbers.Add(FhmRetiredNumber.Read(reader));
        }

        result.WikiUrl = reader.ReadQString();
        result.WebsiteUrl = reader.ReadQString();
        result.Tactics = FhmTeamTacticsSettings.ReadFrom(reader);
        result.Rest = FhmTeamTailRest.Read(reader);
        return result;
    }

    internal void WriteTo(FhmBinaryWriter writer)
    {
        FhmTeamBinary.WriteOpaque(writer, Pre, 32, nameof(Pre));
        if (UnknownMRecords.Count > ushort.MaxValue)
        {
            throw new FhmFormatException("Team tail unknown M records exceeds UInt16 count capacity.");
        }

        writer.WriteUInt16((ushort)UnknownMRecords.Count);
        writer.WriteInt32(UnknownMRecords.Count * 3);
        FhmTeamBinary.WriteOpaque(writer, Pre2Prefix, 139, nameof(Pre2Prefix));
        writer.WriteUInt16(FanHappiness);
        FhmTeamBinary.WriteOpaque(writer, Pre2Suffix, 21, nameof(Pre2Suffix));
        foreach (var record in UnknownMRecords)
        {
            FhmTeamBinary.WriteOpaque(writer, record, 12, "unknown M record");
        }

        writer.WriteCount(RetiredNumbers.Count, "retired numbers");
        foreach (var retiredNumber in RetiredNumbers)
        {
            retiredNumber.WriteTo(writer);
        }

        writer.WriteQString(WikiUrl);
        writer.WriteQString(WebsiteUrl);
        Tactics.WriteTo(writer);
        Rest.WriteTo(writer);
    }
}

/// <summary>A retired-number record.</summary>
public sealed class FhmRetiredNumber
{
    public ushort Year { get; set; }

    public ushort Number { get; set; }

    public ushort Flag { get; set; }

    public FhmPlayerInternalIdentity PlayerReference { get; set; }

    internal static FhmRetiredNumber Read(FhmBinaryReader reader) => new()
    {
        Year = reader.ReadUInt16(),
        Number = reader.ReadUInt16(),
        Flag = reader.ReadUInt16(),
        PlayerReference = new FhmPlayerInternalIdentity(reader.ReadInt32()),
    };

    internal void WriteTo(FhmBinaryWriter writer)
    {
        writer.WriteUInt16(Year);
        writer.WriteUInt16(Number);
        writer.WriteUInt16(Flag);
        writer.WriteInt32(PlayerReference.Value);
    }
}

/// <summary>The structurally complete team tail after tactical settings.</summary>
public sealed class FhmTeamTailRest
{
    public IList<FhmJuniorHistoryRecord> MajorJuniorHistory { get; } = [];

    public FhmTeamRecordIndex MainRivalRecordIndex { get; set; }

    public FhmTeamRecordIndex PotentialRivalRecordIndex { get; set; }

    public ushort PotentialRivalProgress { get; set; }

    public IList<FhmFanHappinessHistoryRecord> FanHappinessHistory { get; } = [];

    public IList<FhmActiveLineSlotLockList> ActiveLineSlotLocks { get; } = CreateLineSlotLocks();

    public IList<FhmManagedDepthChart> ManagedDepthCharts { get; } = CreateManagedDepthCharts();

    public int UnknownInt32_1 { get; set; }

    public int UnknownInt32_2 { get; set; }

    public IList<IList<ushort>> UnsignedShortLists { get; } = FhmTeamBinary.CreateUInt16Lists(7);

    public IList<FhmPlayerInternalIdentity> AdditionalPlayerIds { get; } = [];

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

    public IList<IList<FhmPlayerInternalIdentity>> NestedPlayerIdLists { get; } = [];

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

    internal static FhmTeamTailRest Read(FhmBinaryReader reader)
    {
        var result = new FhmTeamTailRest();
        var juniorHistoryCount = reader.ReadCount("major junior history");
        for (var index = 0; index < juniorHistoryCount; index++)
        {
            result.MajorJuniorHistory.Add(FhmJuniorHistoryRecord.Read(reader));
        }

        result.MainRivalRecordIndex = new FhmTeamRecordIndex(reader.ReadInt32());
        result.PotentialRivalRecordIndex = new FhmTeamRecordIndex(reader.ReadInt32());
        result.PotentialRivalProgress = reader.ReadUInt16();

        var fanHappinessHistoryCount = reader.ReadCount("fan happiness history");
        for (var index = 0; index < fanHappinessHistoryCount; index++)
        {
            result.FanHappinessHistory.Add(FhmFanHappinessHistoryRecord.Read(reader));
        }

        foreach (var locks in result.ActiveLineSlotLocks)
        {
            FhmTeamBinary.ReadByteList(reader, locks.Values, "active line slot locks");
        }

        foreach (var depthChart in result.ManagedDepthCharts)
        {
            FhmTeamBinary.ReadPlayerReferences(reader, depthChart.PlayerReferences, "managed depth chart");
        }

        result.UnknownInt32_1 = reader.ReadInt32();
        result.UnknownInt32_2 = reader.ReadInt32();
        foreach (var list in result.UnsignedShortLists)
        {
            FhmTeamBinary.ReadUInt16List(reader, list, "team unsigned-short list");
        }

        FhmTeamBinary.ReadPlayerReferences(reader, result.AdditionalPlayerIds, "additional player IDs");
        result.Flag1 = reader.ReadByte();
        result.Flag2 = reader.ReadByte();
        result.UnknownInt32_3 = reader.ReadInt32();
        result.Flag3 = reader.ReadByte();
        result.UnknownDouble_1 = reader.ReadDouble();
        result.UnknownInt32_4 = reader.ReadInt32();
        result.UnknownByte_1 = reader.ReadByte();
        result.UnknownByte_2 = reader.ReadByte();
        result.UnknownByte_3 = reader.ReadByte();
        result.UnknownUInt16_1 = reader.ReadUInt16();
        result.UnknownUInt16_2 = reader.ReadUInt16();
        result.UnknownByte_4 = reader.ReadByte();
        result.UnknownByte_5 = reader.ReadByte();
        FhmTeamBinary.ReadBytes(reader, result.Flags4To9);
        FhmTeamBinary.ReadBytes(reader, result.UnknownBytes6To9);
        result.UnknownUInt16_3 = reader.ReadUInt16();
        result.UnknownDouble_2 = reader.ReadDouble();
        result.UnknownInt32_5 = reader.ReadInt32();
        result.Flag10 = reader.ReadByte();
        result.UnknownUInt16_4 = reader.ReadUInt16();
        result.UnknownByte_10 = reader.ReadByte();

        var financeCurveRecordCount = reader.ReadCount("finance curve records");
        for (var index = 0; index < financeCurveRecordCount; index++)
        {
            result.FinanceCurveRecords.Add(reader.ReadOpaqueBytes(89));
        }

        result.UnknownInt32_6 = reader.ReadInt32();
        result.UnknownDouble_3 = reader.ReadDouble();
        result.Flag11 = reader.ReadByte();
        FhmTeamBinary.ReadBytes(reader, result.UnknownBytes11To13);
        result.Flag12 = reader.ReadByte();

        var nestedListCount = reader.ReadCount("nested player ID lists");
        for (var index = 0; index < nestedListCount; index++)
        {
            var list = new List<FhmPlayerInternalIdentity>();
            FhmTeamBinary.ReadPlayerReferences(reader, list, "nested player ID list");
            result.NestedPlayerIdLists.Add(list);
        }

        result.Flag13 = reader.ReadByte();
        result.Flag14 = reader.ReadByte();
        FhmTeamBinary.ReadBytes(reader, result.UnknownBytes14To16);

        var taggedPlayerIdCount = reader.ReadCount("tagged player IDs");
        for (var index = 0; index < taggedPlayerIdCount; index++)
        {
            result.TaggedPlayerIds.Add(new FhmTaggedPlayerId(new FhmPlayerInternalIdentity(reader.ReadInt32()), reader.ReadByte()));
        }

        result.ClosingUInt16_1 = reader.ReadUInt16();
        result.ClosingUInt16_2 = reader.ReadUInt16();
        result.ClosingFlag1 = reader.ReadByte();
        result.ClosingDouble1 = reader.ReadDouble();
        result.ClosingDouble2 = reader.ReadDouble();
        result.ClosingFlag2 = reader.ReadByte();
        result.ClosingDouble3 = reader.ReadDouble();
        result.ClosingDouble4 = reader.ReadDouble();
        result.ClosingFlag3 = reader.ReadByte();
        result.ClosingUInt16_3 = reader.ReadUInt16();
        result.ClosingUInt16_4 = reader.ReadUInt16();
        result.ClosingFlag4 = reader.ReadByte();
        result.ClosingInt32_1 = reader.ReadInt32();
        result.ClosingInt32_2 = reader.ReadInt32();
        FhmTeamBinary.ReadBytes(reader, result.ClosingFlags);
        result.ClosingInt32_3 = reader.ReadInt32();
        return result;
    }

    internal void WriteTo(FhmBinaryWriter writer)
    {
        FhmTeamBinary.RequireCount(ActiveLineSlotLocks, 13, "active line slot-lock lists");
        FhmTeamBinary.RequireCount(ManagedDepthCharts, 17, "managed depth charts");
        FhmTeamBinary.RequireCount(UnsignedShortLists, 7, "team unsigned-short lists");
        FhmTeamBinary.RequireLength(Flags4To9, 6, nameof(Flags4To9));
        FhmTeamBinary.RequireLength(UnknownBytes6To9, 4, nameof(UnknownBytes6To9));
        FhmTeamBinary.RequireLength(UnknownBytes11To13, 3, nameof(UnknownBytes11To13));
        FhmTeamBinary.RequireLength(UnknownBytes14To16, 3, nameof(UnknownBytes14To16));
        FhmTeamBinary.RequireLength(ClosingFlags, 9, nameof(ClosingFlags));

        writer.WriteCount(MajorJuniorHistory.Count, "major junior history");
        foreach (var record in MajorJuniorHistory)
        {
            record.WriteTo(writer);
        }

        writer.WriteInt32(MainRivalRecordIndex.Value);
        writer.WriteInt32(PotentialRivalRecordIndex.Value);
        writer.WriteUInt16(PotentialRivalProgress);
        writer.WriteCount(FanHappinessHistory.Count, "fan happiness history");
        foreach (var record in FanHappinessHistory)
        {
            record.WriteTo(writer);
        }

        foreach (var locks in ActiveLineSlotLocks)
        {
            FhmTeamBinary.WriteByteList(writer, locks.Values, "active line slot locks");
        }

        foreach (var depthChart in ManagedDepthCharts)
        {
            FhmTeamBinary.WritePlayerReferences(writer, depthChart.PlayerReferences, "managed depth chart");
        }

        writer.WriteInt32(UnknownInt32_1);
        writer.WriteInt32(UnknownInt32_2);
        foreach (var list in UnsignedShortLists)
        {
            FhmTeamBinary.WriteUInt16List(writer, list, "team unsigned-short list");
        }

        FhmTeamBinary.WritePlayerReferences(writer, AdditionalPlayerIds, "additional player IDs");
        writer.WriteByte(Flag1);
        writer.WriteByte(Flag2);
        writer.WriteInt32(UnknownInt32_3);
        writer.WriteByte(Flag3);
        writer.WriteDouble(UnknownDouble_1);
        writer.WriteInt32(UnknownInt32_4);
        writer.WriteByte(UnknownByte_1);
        writer.WriteByte(UnknownByte_2);
        writer.WriteByte(UnknownByte_3);
        writer.WriteUInt16(UnknownUInt16_1);
        writer.WriteUInt16(UnknownUInt16_2);
        writer.WriteByte(UnknownByte_4);
        writer.WriteByte(UnknownByte_5);
        FhmTeamBinary.WriteBytes(writer, Flags4To9);
        FhmTeamBinary.WriteBytes(writer, UnknownBytes6To9);
        writer.WriteUInt16(UnknownUInt16_3);
        writer.WriteDouble(UnknownDouble_2);
        writer.WriteInt32(UnknownInt32_5);
        writer.WriteByte(Flag10);
        writer.WriteUInt16(UnknownUInt16_4);
        writer.WriteByte(UnknownByte_10);

        writer.WriteCount(FinanceCurveRecords.Count, "finance curve records");
        foreach (var record in FinanceCurveRecords)
        {
            FhmTeamBinary.WriteOpaque(writer, record, 89, "finance curve record");
        }

        writer.WriteInt32(UnknownInt32_6);
        writer.WriteDouble(UnknownDouble_3);
        writer.WriteByte(Flag11);
        FhmTeamBinary.WriteBytes(writer, UnknownBytes11To13);
        writer.WriteByte(Flag12);

        writer.WriteCount(NestedPlayerIdLists.Count, "nested player ID lists");
        foreach (var list in NestedPlayerIdLists)
        {
            FhmTeamBinary.WritePlayerReferences(writer, list, "nested player ID list");
        }

        writer.WriteByte(Flag13);
        writer.WriteByte(Flag14);
        FhmTeamBinary.WriteBytes(writer, UnknownBytes14To16);
        writer.WriteCount(TaggedPlayerIds.Count, "tagged player IDs");
        foreach (var taggedPlayerId in TaggedPlayerIds)
        {
            writer.WriteInt32(taggedPlayerId.PlayerReference.Value);
            writer.WriteByte(taggedPlayerId.Tag);
        }

        writer.WriteUInt16(ClosingUInt16_1);
        writer.WriteUInt16(ClosingUInt16_2);
        writer.WriteByte(ClosingFlag1);
        writer.WriteDouble(ClosingDouble1);
        writer.WriteDouble(ClosingDouble2);
        writer.WriteByte(ClosingFlag2);
        writer.WriteDouble(ClosingDouble3);
        writer.WriteDouble(ClosingDouble4);
        writer.WriteByte(ClosingFlag3);
        writer.WriteUInt16(ClosingUInt16_3);
        writer.WriteUInt16(ClosingUInt16_4);
        writer.WriteByte(ClosingFlag4);
        writer.WriteInt32(ClosingInt32_1);
        writer.WriteInt32(ClosingInt32_2);
        FhmTeamBinary.WriteBytes(writer, ClosingFlags);
        writer.WriteInt32(ClosingInt32_3);
    }

    private static IList<FhmActiveLineSlotLockList> CreateLineSlotLocks()
    {
        var result = new List<FhmActiveLineSlotLockList>(13);
        for (var index = 0; index < 13; index++)
        {
            result.Add(new FhmActiveLineSlotLockList((FhmLineGroup)index));
        }

        return result;
    }

    private static IList<FhmManagedDepthChart> CreateManagedDepthCharts()
    {
        var roles = new[]
        {
            FhmManagedDepthChartRole.EvenStrengthLeftWings,
            FhmManagedDepthChartRole.EvenStrengthCentres,
            FhmManagedDepthChartRole.EvenStrengthRightWings,
            FhmManagedDepthChartRole.EvenStrengthLeftDefence,
            FhmManagedDepthChartRole.EvenStrengthRightDefence,
            FhmManagedDepthChartRole.Goalies,
            FhmManagedDepthChartRole.ExtraAttackers,
            FhmManagedDepthChartRole.ShootoutOrder,
            FhmManagedDepthChartRole.PowerPlayLeftWings,
            FhmManagedDepthChartRole.PowerPlayCentres,
            FhmManagedDepthChartRole.PowerPlayRightWings,
            FhmManagedDepthChartRole.PowerPlayLeftDefence,
            FhmManagedDepthChartRole.PowerPlayRightDefence,
            FhmManagedDepthChartRole.PenaltyKillForwardOne,
            FhmManagedDepthChartRole.PenaltyKillForwardTwo,
            FhmManagedDepthChartRole.PenaltyKillLeftDefence,
            FhmManagedDepthChartRole.PenaltyKillRightDefence,
        };
        return roles.Select(role => new FhmManagedDepthChart(role)).ToList();
    }
}

/// <summary>A major-junior team history entry.</summary>
public sealed class FhmJuniorHistoryRecord
{
    public ushort Flag { get; set; }

    public ushort Year { get; set; }

    public int TeamId { get; set; }

    public byte Pad { get; set; }

    internal static FhmJuniorHistoryRecord Read(FhmBinaryReader reader) => new()
    {
        Flag = reader.ReadUInt16(),
        Year = reader.ReadUInt16(),
        TeamId = reader.ReadInt32(),
        Pad = reader.ReadByte(),
    };

    internal void WriteTo(FhmBinaryWriter writer)
    {
        writer.WriteUInt16(Flag);
        writer.WriteUInt16(Year);
        writer.WriteInt32(TeamId);
        writer.WriteByte(Pad);
    }
}

/// <summary>A fan-happiness adjustment and optional related references.</summary>
public sealed class FhmFanHappinessHistoryRecord
{
    public FhmEnumValue<FhmFanHappinessEvent> EventType { get; set; }

    public ushort ResultingHappiness { get; set; }

    public FhmPlayerInternalIdentity PlayerReference { get; set; }

    public int StaffId { get; set; }

    public FhmTeamRecordIndex RelatedTeamRecordIndex { get; set; }

    public int CompetitionId { get; set; }

    public int LeagueId { get; set; }

    internal static FhmFanHappinessHistoryRecord Read(FhmBinaryReader reader) => new()
    {
        EventType = new FhmEnumValue<FhmFanHappinessEvent>(reader.ReadUInt16()),
        ResultingHappiness = reader.ReadUInt16(),
        PlayerReference = new FhmPlayerInternalIdentity(reader.ReadInt32()),
        StaffId = reader.ReadInt32(),
        RelatedTeamRecordIndex = new FhmTeamRecordIndex(reader.ReadInt32()),
        CompetitionId = reader.ReadInt32(),
        LeagueId = reader.ReadInt32(),
    };

    internal void WriteTo(FhmBinaryWriter writer)
    {
        writer.WriteUInt16(EventType.RawValue);
        writer.WriteUInt16(ResultingHappiness);
        writer.WriteInt32(PlayerReference.Value);
        writer.WriteInt32(StaffId);
        writer.WriteInt32(RelatedTeamRecordIndex.Value);
        writer.WriteInt32(CompetitionId);
        writer.WriteInt32(LeagueId);
    }
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

    public IList<FhmPlayerInternalIdentity> PlayerReferences { get; } = [];
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
public readonly record struct FhmTaggedPlayerId(FhmPlayerInternalIdentity PlayerReference, byte Tag);

internal static class FhmTeamBinary
{
    internal static ReadOnlySpan<byte> RosterPostSignature => [0, 100, 1];

    internal static FhmOpaqueBytes Opaque(int length) => new(new byte[length]);

    internal static void ReadIntList(FhmBinaryReader reader, IList<int> values, string collectionName)
    {
        var count = reader.ReadCount(collectionName);
        for (var index = 0; index < count; index++)
        {
            values.Add(reader.ReadInt32());
        }
    }

    internal static void WriteIntList(FhmBinaryWriter writer, IEnumerable<int> values, string collectionName)
    {
        if (values is not ICollection<int> collection)
        {
            throw new FhmFormatException($"{collectionName} must be a countable collection.");
        }

        writer.WriteCount(collection.Count, collectionName);
        foreach (var value in values)
        {
            writer.WriteInt32(value);
        }
    }

    internal static void ReadPlayerReferences(FhmBinaryReader reader, IList<FhmPlayerInternalIdentity> values, string collectionName)
    {
        var count = reader.ReadCount(collectionName);
        for (var index = 0; index < count; index++)
        {
            values.Add(new FhmPlayerInternalIdentity(reader.ReadInt32()));
        }
    }

    internal static void WritePlayerReferences(FhmBinaryWriter writer, IEnumerable<FhmPlayerInternalIdentity> values, string collectionName)
    {
        if (values is not ICollection<FhmPlayerInternalIdentity> collection)
        {
            throw new FhmFormatException($"{collectionName} must be a countable collection.");
        }

        writer.WriteCount(collection.Count, collectionName);
        foreach (var value in values)
        {
            writer.WriteInt32(value.Value);
        }
    }

    internal static void ReadUInt16List(FhmBinaryReader reader, IList<ushort> values, string collectionName)
    {
        var count = reader.ReadCount(collectionName);
        for (var index = 0; index < count; index++)
        {
            values.Add(reader.ReadUInt16());
        }
    }

    internal static void WriteUInt16List(FhmBinaryWriter writer, IEnumerable<ushort> values, string collectionName)
    {
        if (values is not ICollection<ushort> collection)
        {
            throw new FhmFormatException($"{collectionName} must be a countable collection.");
        }

        writer.WriteCount(collection.Count, collectionName);
        foreach (var value in values)
        {
            writer.WriteUInt16(value);
        }
    }

    internal static void ReadByteList(FhmBinaryReader reader, IList<byte> values, string collectionName)
    {
        var count = reader.ReadCount(collectionName);
        for (var index = 0; index < count; index++)
        {
            values.Add(reader.ReadByte());
        }
    }

    internal static void WriteByteList(FhmBinaryWriter writer, IEnumerable<byte> values, string collectionName)
    {
        if (values is not ICollection<byte> collection)
        {
            throw new FhmFormatException($"{collectionName} must be a countable collection.");
        }

        writer.WriteCount(collection.Count, collectionName);
        foreach (var value in values)
        {
            writer.WriteByte(value);
        }
    }

    internal static void ReadBytes(FhmBinaryReader reader, byte[] values)
    {
        for (var index = 0; index < values.Length; index++)
        {
            values[index] = reader.ReadByte();
        }
    }

    internal static void WriteBytes(FhmBinaryWriter writer, IEnumerable<byte> values)
    {
        foreach (var value in values)
        {
            writer.WriteByte(value);
        }
    }

    internal static void RequireContents(FhmBinaryReader reader, ReadOnlySpan<byte> expected, string name)
    {
        foreach (var value in expected)
        {
            if (reader.ReadByte() != value)
            {
                throw new FhmFormatException($"Invalid {name} at offset {reader.Position - 1}.");
            }
        }
    }

    internal static void WriteOpaque(FhmBinaryWriter writer, FhmOpaqueBytes value, int expectedLength, string name)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (value.Value.Length != expectedLength)
        {
            throw new FhmFormatException($"{name} must be exactly {expectedLength} bytes.");
        }

        writer.WriteOpaqueBytes(value);
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

    internal static IList<FhmQColor> CreateColours()
    {
        var result = new List<FhmQColor>(13);
        for (var index = 0; index < 13; index++)
        {
            result.Add(new FhmQColor());
        }

        return result;
    }

    internal static IList<FhmTeamPostUnit> CreatePostUnits()
    {
        var result = new List<FhmTeamPostUnit>(4);
        for (var index = 0; index < 4; index++)
        {
            result.Add(new FhmTeamPostUnit());
        }

        return result;
    }

    internal static IList<IList<ushort>> CreateUInt16Lists(int count)
    {
        var result = new List<IList<ushort>>(count);
        for (var index = 0; index < count; index++)
        {
            result.Add([]);
        }

        return result;
    }
}
