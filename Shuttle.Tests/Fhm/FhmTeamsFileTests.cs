using Shuttle.Fhm.SaveData.Binary;
using Shuttle.Fhm.SaveData.Files;
using Shuttle.Fhm.SaveData.Model;
using Shuttle.Fhm.SaveData.SaveFolder;

namespace Shuttle.Tests.Fhm;

public sealed class FhmTeamsFileTests
{
    [Fact]
    public void TeamsFile_RoundTripsStructuredRecordsAndPersistsMutations()
    {
        var root = CreateTestRoot();
        var source = Path.Combine(root, "source");
        var rewritten = Path.Combine(root, "rewritten");
        var mutated = Path.Combine(root, "mutated");
        try
        {
            var teams = new FhmTeamsFile { VersionTag = 10 };
            teams.Teams.Add(CreateTeam());
            WriteTeams(teams, source);
            var sourceBytes = File.ReadAllBytes(Path.Combine(source, "teams.dat"));

            var loaded = ReadTeams(source);
            var team = Assert.Single(loaded.Teams);
            Assert.Equal(17, team.TeamId);
            Assert.Equal("Test City", team.City);
            Assert.Equal(2030, Assert.Single(team.SeasonHistory).Year);
            Assert.Equal(5, Assert.Single(team.SeasonParticipation.Blocks).Records[0].ParticipationId);
            Assert.Equal(11, Assert.Single(team.PostHead.Goalies));
            Assert.False(team.PostHead.PositionRequirements.HasConsistentPairs);
            Assert.Equal([3, 2, 99], team.PostHead.PositionRequirements.Words);
            Assert.Equal((ushort)77, team.Tail.Tactics.FinalOffensiveOrientation.RawValue);
            Assert.False(team.Tail.Tactics.FinalOffensiveOrientation.IsKnown);
            Assert.Equal((ushort)99, Assert.Single(team.Tail.Rest.FanHappinessHistory).EventType.RawValue);
            Assert.False(Assert.Single(team.Tail.Rest.FanHappinessHistory).EventType.IsKnown);
            Assert.Equal((byte)1, team.Tail.Rest.ActiveLineSlotLocks[0].Values[0]);
            Assert.Equal(22, Assert.Single(team.Tail.Rest.ManagedDepthCharts[0].PlayerReferences));
            Assert.Equal([9, 8, 7], team.Tail.Rest.FinanceCurveRecords[0].Value[..3]);

            WriteTeams(loaded, rewritten);
            Assert.Equal(sourceBytes, File.ReadAllBytes(Path.Combine(rewritten, "teams.dat")));

            team.FanLoyalty = 88;
            team.Tail.FanHappiness = 73;
            team.Tail.Tactics.Selectors[1].SystemIds[0] = 55;
            WriteTeams(loaded, mutated);

            var mutatedBytes = File.ReadAllBytes(Path.Combine(mutated, "teams.dat"));
            Assert.NotEqual(sourceBytes, mutatedBytes);
            var mutatedTeam = Assert.Single(ReadTeams(mutated).Teams);
            Assert.Equal((ushort)88, mutatedTeam.FanLoyalty);
            Assert.Equal((ushort)73, mutatedTeam.Tail.FanHappiness);
            Assert.Equal((ushort)55, mutatedTeam.Tail.Tactics.Selectors[1].SystemIds[0]);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void TeamsFile_DerivesCountAndRejectsTrailingBytes()
    {
        var root = CreateTestRoot();
        var source = Path.Combine(root, "source");
        try
        {
            var teams = new FhmTeamsFile { VersionTag = 10 };
            teams.Teams.Add(CreateTeam());
            teams.Teams.Add(CreateTeam());
            WriteTeams(teams, source);

            var bytes = File.ReadAllBytes(Path.Combine(source, "teams.dat"));
            Assert.Equal(2, System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(4, 4)));

            File.WriteAllBytes(Path.Combine(source, "teams.dat"), [.. bytes, 0xFF]);
            var exception = Assert.Throws<FhmFormatException>(() => ReadTeams(source));
            Assert.Contains("unread bytes", exception.Message);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void TeamsFile_RejectsInvalidRosterDelimiterAndSelectorOrdering()
    {
        var team = CreateTeam();
        team.Roster.Lists[0].Add(10);
        var teams = new FhmTeamsFile { VersionTag = 10 };
        teams.Teams.Add(team);

        var save = new FhmSave();
        save.Files.Add(teams.RelativePath, teams);
        var root = CreateTestRoot();
        try
        {
            var rosterException = Assert.Throws<FhmFormatException>(() => new FhmSaveWriter().Write(save, root));
            Assert.Contains("empty participation delimiter", rosterException.Message);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }

        team.Roster.Lists[0].Clear();
        (team.Tail.Tactics.Selectors[0], team.Tail.Tactics.Selectors[1]) =
            (team.Tail.Tactics.Selectors[1], team.Tail.Tactics.Selectors[0]);
        root = CreateTestRoot();
        try
        {
            var selectorException = Assert.Throws<FhmFormatException>(() => new FhmSaveWriter().Write(save, root));
            Assert.Contains("serialized order", selectorException.Message);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static FhmTeamRecord CreateTeam()
    {
        var team = new FhmTeamRecord
        {
            RecordIndex = 3,
            TeamId = 17,
            InternalCode = "TST",
            InternalCode2 = null,
            Flag1 = 1,
            City = "Test City",
            Nickname = "Rockets",
            NicknamePlacement = 1,
            AffiliateParentId = -1,
            AffiliateParentId2 = -1,
            LeagueId = 1,
            ConferenceId = 2,
            DivisionId = 3,
            LocationId = 4,
            MarketSize = 55,
            FanLoyalty = 66,
            Finance1 = 1,
            Finance2 = 2,
            Finance3 = 3,
            UnknownInt32_13 = -4,
            UnknownInt32_14 = -5,
            Finance4 = 6,
        };
        team.SeasonHistory.Add(new FhmTeamSeasonRecord
        {
            Year = 2030,
            City = "Test City",
            Nickname = "Rockets",
            Abbreviation = "TST",
            Stats = new FhmOpaqueBytes(Enumerable.Range(0, 134).Select(value => (byte)value).ToArray()),
        });
        team.FranchiseHistory.NameHistoryCount = 1;
        team.FranchiseHistory.NameHistory.Add("Test City Rockets");
        team.FranchiseHistory.AbbreviationHistory.Add("TST");
        team.ActiveLines.Lists[0].PlayerReferences.Add(10);
        team.LeadershipReserve.Captain = 10;
        team.LeadershipReserve.AlternateCaptain1 = 11;
        team.LeadershipReserve.ReserveSlots.Add(12);
        var participation = new FhmSeasonParticipationBlock();
        participation.Records.Add(new FhmSeasonParticipationRecord { SequenceNumber = 1, Year = 2030, ParticipationId = 5, Flag = 1 });
        team.SeasonParticipation.Blocks.Clear();
        team.SeasonParticipation.Blocks.Add(participation);
        team.PostHead.Goalies.Add(11);
        team.PostHead.Defensemen.Add(12);
        team.PostHead.Forwards.Add(13);
        team.PostHead.RegionId = 9;
        team.PostHead.PositionRequirements.LogicalEntryCount = 1;
        team.PostHead.PositionRequirements.Words.Add(3);
        team.PostHead.PositionRequirements.Words.Add(2);
        team.PostHead.PositionRequirements.Words.Add(99);
        team.PostBody.AllTimePlayers.Add(10);
        team.PostBody.PreColourList.Add(20);
        team.PostBody.Colours[0] = new FhmQColor { Spec = 1, Alpha = 65535, Red = 1, Green = 2, Blue = 3 };
        team.PostBody.Abbreviation = "TST";
        team.PostBody.Units[0].Values.Add(4);
        team.Tail.UnknownMRecords.Add(new FhmOpaqueBytes(new byte[12]));
        team.Tail.FanHappiness = 61;
        team.Tail.RetiredNumbers.Add(new FhmRetiredNumber { Year = 2020, Number = 9, Flag = 1, PlayerReference = 10 });
        team.Tail.WikiUrl = "https://example.test/wiki";
        team.Tail.WebsiteUrl = "https://example.test";
        ConfigureTactics(team.Tail.Tactics);
        team.Tail.Rest.MajorJuniorHistory.Add(new FhmJuniorHistoryRecord { Flag = 1, Year = 2029, TeamId = 99, Pad = 0 });
        team.Tail.Rest.MainRivalRecordIndex = 2;
        team.Tail.Rest.PotentialRivalRecordIndex = 4;
        team.Tail.Rest.PotentialRivalProgress = 3;
        team.Tail.Rest.FanHappinessHistory.Add(new FhmFanHappinessHistoryRecord
        {
            EventType = new FhmEnumValue<FhmFanHappinessEvent>(99),
            ResultingHappiness = 61,
            PlayerReference = 10,
            StaffId = -1,
            RelatedTeamRecordIndex = 2,
            CompetitionId = -1,
            LeagueId = 1,
        });
        team.Tail.Rest.ActiveLineSlotLocks[0].Values.Add(1);
        team.Tail.Rest.ManagedDepthCharts[0].PlayerReferences.Add(22);
        team.Tail.Rest.UnsignedShortLists[0].Add(6);
        team.Tail.Rest.AdditionalPlayerIds.Add(23);
        team.Tail.Rest.Flags4To9[0] = 4;
        team.Tail.Rest.UnknownBytes6To9[3] = 9;
        team.Tail.Rest.FinanceCurveRecords.Add(new FhmOpaqueBytes([9, 8, 7, .. new byte[86]]));
        team.Tail.Rest.NestedPlayerIdLists.Add([24]);
        team.Tail.Rest.TaggedPlayerIds.Add(new FhmTaggedPlayerId(25, 2));
        team.Tail.Rest.ClosingFlags[8] = 1;
        return team;
    }

    private static void ConfigureTactics(FhmTeamTacticsSettings tactics)
    {
        tactics.TeamValue1 = 1;
        tactics.TacticsObjectVersion = 7;
        tactics.FinalOffensiveOrientation = new FhmEnumValue<FhmOffensiveOrientation>(77);
        tactics.FinalPhysicalOrientation = new FhmEnumValue<FhmPhysicalOrientation>((ushort)FhmPhysicalOrientation.Physical);
        tactics.Selectors[4].DelayedUseOwnSettingsFlags[0] = 1;
        foreach (var selector in tactics.Selectors.Where(selector => selector.BlockIndex != 21))
        {
            selector.OffensiveOrientation = new FhmEnumValue<FhmOffensiveOrientation>((ushort)FhmOffensiveOrientation.Balanced);
            selector.PhysicalOrientation = new FhmEnumValue<FhmPhysicalOrientation>((ushort)FhmPhysicalOrientation.NonPhysical);
        }
    }

    private static FhmTeamsFile ReadTeams(string directory) =>
        Assert.IsType<FhmTeamsFile>(new FhmSaveReader().Read(directory).Files["teams.dat"]);

    private static void WriteTeams(FhmTeamsFile teams, string directory)
    {
        var save = new FhmSave();
        save.Files.Add(teams.RelativePath, teams);
        new FhmSaveWriter().Write(save, directory);
    }

    private static string CreateTestRoot()
    {
        var root = Path.Combine(Environment.CurrentDirectory, "fhm-teams-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }
}
