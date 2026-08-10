using Shuttle.Fhm.SaveData.Binary;
using Shuttle.Fhm.SaveData.Files;
using Shuttle.Fhm.SaveData.Model;
using Shuttle.Fhm.SaveData.SaveFolder;

namespace Shuttle.Tests.Fhm;

public sealed class FhmSaveTests
{
    [Fact]
    public void QtPrimitives_RoundTripNullEmptyAndBigEndianValues()
    {
        using var stream = new MemoryStream();
        using (var writer = new FhmBinaryWriter(stream, leaveOpen: true))
        {
            writer.WriteInt32(unchecked((int)0x01020304));
            writer.WriteUInt16(0xABCD);
            writer.WriteDouble(12.5);
            writer.WriteQString(null);
            writer.WriteQString(string.Empty);
            writer.WriteQString("Ångström");
            writer.WriteDate(new FhmDate(2030, 2, 3));
        }

        Assert.Equal([1, 2, 3, 4, 0xAB, 0xCD], stream.ToArray()[..6]);
        stream.Position = 0;
        using var reader = new FhmBinaryReader(stream);
        Assert.Equal(unchecked((int)0x01020304), reader.ReadInt32());
        Assert.Equal((ushort)0xABCD, reader.ReadUInt16());
        Assert.Equal(12.5, reader.ReadDouble());
        Assert.Null(reader.ReadQString());
        Assert.Equal(string.Empty, reader.ReadQString());
        Assert.Equal("Ångström", reader.ReadQString());
        Assert.Equal(new FhmDate(2030, 2, 3), reader.ReadDate());
        reader.EnsureEof("primitive fixture");
    }

    [Fact]
    public void QtPrimitives_RoundTripUnpairedUtf16Surrogates()
    {
        const string value = "\uD800\uDC00\uD801";
        using var stream = new MemoryStream();
        using (var writer = new FhmBinaryWriter(stream, leaveOpen: true))
        {
            writer.WriteQString(value);
        }

        Assert.Equal([0, 0, 0, 6, 0xD8, 0x00, 0xDC, 0x00, 0xD8, 0x01], stream.ToArray());
        stream.Position = 0;
        using var reader = new FhmBinaryReader(stream);
        Assert.Equal(value, reader.ReadQString());
        reader.EnsureEof("surrogate fixture");
    }

    [Fact]
    public void GameSettings_RoundTripAllEightyDocumentedFields()
    {
        var settings = new FhmGameSettingsFile();
        settings.Set(FhmGameSetting.Setting001, (byte)1);
        settings.Set(FhmGameSetting.Setting072, (ushort)72);
        settings.Set(FhmGameSetting.Setting080, (byte)80);

        Assert.Equal(80, settings.Values.Count);
        Assert.Equal(80, Enum.GetValues<FhmGameSetting>().Length);
        var root = CreateTestRoot();
        try
        {
            var save = new FhmSave();
            save.Files.Add(settings.RelativePath, settings);
            new FhmSaveWriter().Write(save, root);
            var roundTripped = Assert.IsType<FhmGameSettingsFile>(new FhmSaveReader().Read(root).Files[settings.RelativePath]);
            Assert.Equal((ushort)72, roundTripped.Get<ushort>(FhmGameSetting.Setting072));
            Assert.Equal((byte)80, roundTripped.Get<byte>(FhmGameSetting.Setting080));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void FolderReaderWriter_RoundTripsDocumentedAndOpaqueSyntheticFiles()
    {
        var root = CreateTestRoot();
        var source = Path.Combine(root, "source");
        var destination = Path.Combine(root, "destination");
        var rewritten = Path.Combine(root, "rewritten");
        try
        {
            var save = CreateSave();
            new FhmSaveWriter().Write(save, source);
            Directory.CreateDirectory(Path.Combine(source, "assets"));
            File.WriteAllBytes(Path.Combine(source, "assets", "unmodeled.bin"), [9, 8, 7, 6]);

            var loaded = new FhmSaveReader().Read(source);
            Assert.Equal(save.Files.Count, loaded.Files.Count);
            var opaque = Assert.Single(loaded.OpaqueFiles);
            Assert.Equal("assets/unmodeled.bin", opaque.RelativePath);
            Assert.Equal([9, 8, 7, 6], opaque.Content);

            new FhmSaveWriter().Write(loaded, destination);
            AssertFoldersEqual(source, destination);

            var info = Assert.IsType<FhmInfoFile>(loaded.Files["info.dat"]);
            info.Description = "Edited fixture";
            new FhmSaveWriter().Write(loaded, rewritten);
            Assert.Equal([9, 8, 7, 6], File.ReadAllBytes(Path.Combine(rewritten, "assets", "unmodeled.bin")));
            Assert.NotEqual(
                File.ReadAllBytes(Path.Combine(source, "info.dat")),
                File.ReadAllBytes(Path.Combine(rewritten, "info.dat")));
            AssertFoldersEqualExcept(source, rewritten, "info.dat");
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void FolderReader_RejectsUnsupportedPlayersVersion()
    {
        var root = CreateTestRoot();
        try
        {
            using (var stream = File.Create(Path.Combine(root, "players.dat")))
            {
                using var writer = new FhmBinaryWriter(stream);
                writer.WriteInt32(57);
                writer.WriteInt32(0);
            }

            var exception = Assert.Throws<FhmFormatException>(() => new FhmSaveReader().Read(root));
            Assert.Contains("version 57", exception.Message);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void PlayersFile_RoundTripsStructuredMultipleRecordsAndMutations()
    {
        var root = CreateTestRoot();
        var source = Path.Combine(root, "source");
        var rewritten = Path.Combine(root, "rewritten");
        var mutated = Path.Combine(root, "mutated");
        try
        {
            var save = new FhmSave();
            var players = CreateStructuredPlayersFile();
            Add(save, players);
            new FhmSaveWriter().Write(save, source);
            var sourceBytes = File.ReadAllBytes(Path.Combine(source, "players.dat"));

            var loaded = new FhmSaveReader().Read(source);
            var loadedPlayers = Assert.IsType<FhmPlayersFile>(loaded.Files["players.dat"]);
            Assert.Equal(2, loadedPlayers.Players.Count);

            var first = loadedPlayers.Players[0];
            Assert.Equal(new FhmPlayerInternalIdentity(0), first.InternalIdentity);
            Assert.Equal((ushort)20, first.PositionRatings.Centre);
            Assert.Equal((ushort)65280, first.PositionRatings.UnknownRightWingRaw);
            Assert.Equal((byte)19, first.RatingAttributes.ShootingAccuracy);
            Assert.Null(first.UnknownString02);
            Assert.Equal(string.Empty, first.UnknownString03);
            Assert.Single(first.Contracts);
            Assert.Single(first.AggregateSkaterStats01);
            Assert.Equal((ushort)99, first.AggregateSkaterStats01[0].GameType.RawValue);
            Assert.False(first.AggregateSkaterStats01[0].GameType.IsKnown);
            Assert.Equal((ushort)536, first.DetailedSkaterGameStats[0].OnIceShotsFor);
            Assert.Single(first.UnknownRecordList03);
            Assert.Single(first.UnknownU1S4PairList);
            Assert.NotNull(first.PrimaryRole);
            Assert.Equal((ushort)3, first.PrimaryRole!.ShootingTendencyValue);
            Assert.Null(first.SupplementaryRole);

            new FhmSaveWriter().Write(loaded, rewritten);
            Assert.Equal(sourceBytes, File.ReadAllBytes(Path.Combine(rewritten, "players.dat")));

            loadedPlayers.Players[1].RatingAttributes.Speed = 21;
            loadedPlayers.Players[1].PositionRatings.UnknownCentreRaw = 12345;
            new FhmSaveWriter().Write(loaded, mutated);
            var mutatedBytes = File.ReadAllBytes(Path.Combine(mutated, "players.dat"));
            Assert.NotEqual(sourceBytes, mutatedBytes);

            var mutatedSave = new FhmSaveReader().Read(mutated);
            var mutatedPlayers = Assert.IsType<FhmPlayersFile>(mutatedSave.Files["players.dat"]);
            Assert.Equal((byte)21, mutatedPlayers.Players[1].RatingAttributes.Speed);
            Assert.Equal((ushort)12345, mutatedPlayers.Players[1].PositionRatings.UnknownCentreRaw);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void EnumValue_PreservesFutureRawValue()
    {
        var unknown = new FhmEnumValue<FhmPlayingRole>(1234);

        Assert.False(unknown.IsKnown);
        Assert.Equal((ushort)1234, unknown.RawValue);
    }

    [Fact]
    public void TeamTacticsSettings_RoundTripDelayedFlagsAndUnknownEnums()
    {
        var settings = new FhmTeamTacticsSettings
        {
            TeamValue1 = 2,
            FinalOffensiveOrientation = new FhmEnumValue<FhmOffensiveOrientation>(99),
            FinalPhysicalOrientation = new FhmEnumValue<FhmPhysicalOrientation>((ushort)FhmPhysicalOrientation.Physical),
        };
        settings.BaseSettings[58] = 7;
        settings.Selectors[4].DelayedUseOwnSettingsFlags[3] = 1;
        settings.Selectors[21].SystemIds[11] = 88;
        foreach (var selector in settings.Selectors.Where(selector => selector.BlockIndex != 21))
        {
            selector.OffensiveOrientation = new FhmEnumValue<FhmOffensiveOrientation>((ushort)FhmOffensiveOrientation.Balanced);
            selector.PhysicalOrientation = new FhmEnumValue<FhmPhysicalOrientation>((ushort)FhmPhysicalOrientation.NonPhysical);
        }

        settings.Tendencies[0].Values[(int)FhmTendency.Shooting] = 3;
        using var stream = new MemoryStream();
        using (var writer = new FhmBinaryWriter(stream, leaveOpen: true))
        {
            settings.WriteTo(writer);
        }

        stream.Position = 0;
        using var reader = new FhmBinaryReader(stream);
        var roundTripped = FhmTeamTacticsSettings.ReadFrom(reader);
        reader.EnsureEof("team tactics fixture");
        Assert.Equal((ushort)99, roundTripped.FinalOffensiveOrientation.RawValue);
        Assert.False(roundTripped.FinalOffensiveOrientation.IsKnown);
        Assert.Equal((byte)1, roundTripped.Selectors[4].DelayedUseOwnSettingsFlags[3]);
        Assert.Equal((ushort)88, roundTripped.Selectors[21].SystemIds[11]);
        Assert.Equal((ushort)3, roundTripped.Tendencies[0].Values[(int)FhmTendency.Shooting]);
    }

    private static FhmSave CreateSave()
    {
        var save = new FhmSave();
        Add(save, new FhmInfoFile { Description = null, NameId = string.Empty });

        var names = new FhmNamesFile();
        names.MasterNames.Add(new FhmNameEntry("O'Connor", 10, 4, -2, 1, 0, 1));
        names.FirstNameLists[0].Add(10);
        names.SurnameLists[101].Add(10);
        names.ScalarArrayA[0] = -1;
        names.ScalarArrayB[101] = 9999;
        Add(save, names);

        var role = new FhmPlayerRolesFile { VersionTag = 1 };
        var definition = new FhmPlayerRoleDefinition
        {
            RoleId = 8,
            Name = "Playmaker",
            AppliesToForwards = 1,
            PositionCategory = 4,
            ShortName = "PLY",
            Description = "Synthetic test role",
            TuningValueA = 2,
            TuningValueB = 3,
            TuningValueC = 4,
        };
        definition.WeightGroupA[0] = 9;
        definition.WeightGroupE[18] = -4;
        definition.IndexLists[0].Add(7);
        role.Records.Add(definition);
        Add(save, role);

        var teamTactics = new FhmTeamTacticsFile { VersionTag = 2 };
        teamTactics.Records.Add(new FhmTacticSystem
        {
            GlobalId = 42,
            ZoneGroupRaw = (int)FhmTacticZone.OffensiveZoneAttack,
            Name = "Cycle",
            RatingA = 3,
            RatingB = 4,
        });
        Add(save, teamTactics);

        var storedLines = new FhmStoredLinesFile();
        var storedLine = new FhmStoredLine { Name = "Default" };
        storedLine.PlayerGroups[(int)FhmLineGroup.EvenStrengthForwards].Add(5);
        storedLine.PlayerGroups[(int)FhmLineGroup.EvenStrengthForwards].Add(-1);
        storedLine.UnitLocks[(int)FhmLineGroup.EvenStrengthForwards].Add(1);
        storedLine.UnitLocks[(int)FhmLineGroup.EvenStrengthForwards].Add(0);
        storedLines.StoredLines.Add(storedLine);
        Add(save, storedLines);

        var trade = new FhmTradeFile { VersionTag = 3 };
        var tradeRecord = new FhmTradeRecord { Field0 = 1, Date = new FhmDate(2028, 7, 1), Field4 = 2, Field5 = 3 };
        tradeRecord.AssetLists[0].Add(11);
        tradeRecord.InterleavedUnsignedFields[0] = 4;
        tradeRecord.PairLists[1].Add(new FhmIntUInt16Pair(-1, 9999));
        trade.Records.Add(tradeRecord);
        Add(save, trade);

        var history = new FhmTradeHistoryFile { VersionTag = 3 };
        var historyRecord = new FhmTradeHistoryRecord { Id = 99, Date = new FhmDate(2028, 7, 2), TeamA = 1, TeamB = 2, Flag = 1 };
        historyRecord.AssetLists[3].Add(1001);
        historyRecord.DraftPickLists[0].Add(new FhmDraftPickDescriptor(1, 2, 3, 4, 5, 6));
        history.Records.Add(historyRecord);
        Add(save, history);

        var setPlay = new FhmSetPlayFile("set_play_5on4.dat") { Version = 4 };
        setPlay.Formations.Add(new FhmLengthPrefixedBlock([1, 2]));
        setPlay.ExtraRecords.Add(new FhmLengthPrefixedBlock([3]));
        Add(save, setPlay);

        var leagues = new FhmLeaguesFile { VersionTag = 1 };
        leagues.League.LeagueId = 15;
        leagues.League.Name = "Synthetic Hockey League";
        leagues.League.ShortName = null;
        leagues.League.FoundingDate = new FhmDate(2020, 1, 1);
        leagues.League.ConfigDoublesPrimary[0] = 1.25;
        leagues.League.OpaqueLeagueBody = [0xDE, 0xAD];
        Add(save, leagues);

        Add(save, new FhmPlayersFile());
        Add(save, new FhmTeamsFile { VersionTag = 1 });
        var gameSettings = new FhmGameSettingsFile();
        gameSettings.Set(FhmGameSetting.Setting001, (byte)1);
        gameSettings.Set(FhmGameSetting.Setting007, (ushort)2);
        gameSettings.ReferencedObjectId = -1;
        Add(save, gameSettings);
        Add(save, new FhmTacticsFile { Version = 1, TacticCount = 0, RecordsOpaque = [4, 5] });
        var zoneEvents = new FhmZoneEventModifiersFile { Version = 1, ZoneCount = 12 };
        zoneEvents.ModifierGrids.Add(new FhmLengthPrefixedBlock([6, 7]));
        Add(save, zoneEvents);

        var shotType = new FhmLengthPrefixedCatalogueFile("shot_type_mod.dat", hasCount: false) { Version = 1 };
        shotType.Blocks.Add(new FhmLengthPrefixedBlock([8]));
        Add(save, shotType);
        var tacticalSettings = new FhmLengthPrefixedCatalogueFile("tactical_settings_mod.dat", hasCount: false) { Version = 1 };
        tacticalSettings.Blocks.Add(new FhmLengthPrefixedBlock([9]));
        Add(save, tacticalSettings);
        return save;
    }

    private static FhmPlayersFile CreateStructuredPlayersFile()
    {
        var players = new FhmPlayersFile();
        var first = new FhmPlayerRecord
        {
            FirstNameId = 101,
            SurnameId = 102,
            CommonNameId = -1,
            BirthDate = new FhmDate(2000, 2, 3),
            InternalIdentity = new FhmPlayerInternalIdentity(0),
            UnknownString01 = "face-key",
            UnknownString02 = null,
            UnknownString03 = string.Empty,
            UnknownDate01 = new FhmDate(2026, 8, 8),
            UnknownString04 = "note",
        };
        first.PositionRatings.Goalie = 1;
        first.PositionRatings.UnknownGoalieRaw = 2;
        first.PositionRatings.LeftDefenceman = 3;
        first.PositionRatings.UnknownLeftDefencemanRaw = 4;
        first.PositionRatings.RightDefenceman = 5;
        first.PositionRatings.UnknownRightDefencemanRaw = 6;
        first.PositionRatings.LeftWing = 7;
        first.PositionRatings.UnknownLeftWingRaw = 8;
        first.PositionRatings.Centre = 20;
        first.PositionRatings.UnknownCentreRaw = 10;
        first.PositionRatings.RightWing = 12;
        first.PositionRatings.UnknownRightWingRaw = 65280;
        first.RatingAttributes.BigGames = 11;
        first.RatingAttributes.Speed = 18;
        first.RatingAttributes.ShootingAccuracy = 19;
        first.UnknownU2Values01[0] = 174;
        first.UnknownS4Values01[0] = 560;
        first.UnknownRecordList01.Add(new FhmFixed24Record { Data = new FhmOpaqueBytes([1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24]) });
        first.UnknownS4List01.Add(-5);
        first.UnknownPairList01.Add(new FhmFixed8Record { Data = new FhmOpaqueBytes([1, 2, 3, 4, 5, 6, 7, 8]) });

        var contract = new FhmPlayerContract { UnknownS401 = 17, UnknownDate01 = new FhmDate(2024, 7, 1), UnknownF801 = 12.5 };
        contract.UnknownS4List.Add(22);
        contract.UnknownU1List01.Add(1);
        contract.UnknownU1List02.Add(2);
        first.Contracts.Add(contract);

        var skaterAggregate = new FhmAggregateSkaterStats
        {
            Year = 2026,
            TeamId = 560,
            LeagueId = 10,
            GameType = new FhmEnumValue<FhmGameType>(99),
            GamesPlayed = 46,
            Goals = 11,
            Assists = 12,
            TimeOnIceMilliseconds = 123456,
            Fights = 2,
            FightWins = 1,
        };
        first.AggregateSkaterStats01.Add(skaterAggregate);
        first.AggregateGoalieStats01.Add(new FhmAggregateGoalieStats
        {
            Year = 2026,
            TeamId = 560,
            LeagueId = 10,
            GameType = new FhmEnumValue<FhmGameType>((ushort)FhmGameType.RegularSeason),
            MinutesMilliseconds = 3600000,
            Wins = 1,
        });
        first.AggregateSkaterStats02.Add(new FhmAggregateSkaterStats { Year = 2025 });
        first.AggregateGoalieStats02.Add(new FhmAggregateGoalieStats { Year = 2025 });
        first.DetailedSkaterGameStats.Add(new FhmDetailedSkaterGameStats
        {
            Year = 2026,
            GameType = new FhmEnumValue<FhmGameType>((ushort)FhmGameType.Playoffs),
            Goals = 1,
            TimeOnIceMilliseconds = 20000,
            OnIceShotsFor = 536,
            BlockedAttemptsAgainst = 693,
        });
        first.DetailedGoalieGameStats.Add(new FhmDetailedGoalieGameStats
        {
            Year = 2026,
            MinutesMilliseconds = 3600000,
            ShotsAgainst = 31,
        });

        first.UnknownS4List02.Add(1);
        first.UnknownS4List03.Add(2);
        first.UnknownU2List01.Add(3);
        first.DatedStringRecords.Add(new FhmDatedStringRecord { JulianDay = 2460000, UnknownU101 = 4, Text = "event" });
        first.UnknownRecordList02.Add(new FhmFixed8Record { Data = new FhmOpaqueBytes([8, 7, 6, 5, 4, 3, 2, 1]) });
        first.UnknownRecordList03.Add(new FhmFixed23Record { Data = new FhmOpaqueBytes([1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1]) });
        first.PrimaryRole = new FhmPlayerRoleInstance { RoleId = 8, AttackingUseOverride = 1, AttackingTendencyValue = 3, ShootingUseOverride = 1, ShootingTendencyValue = 3, ReservedTendencyValue = 2 };
        first.UnknownU1PairList.Add(new FhmFixed2Record { Value01 = 5, Value02 = 6 });
        first.UnknownDatedU2Records.Add(new FhmDatedUshortRecord { Date = new FhmDate(2026, 1, 1) });
        first.UnknownF8Values07[0] = 1.5;
        first.UnknownS4List04.Add(4);
        first.UnknownRecordList04.Add(new FhmFixed16Record { Data = new FhmOpaqueBytes([1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16]) });
        first.SpecialAbilities.Add(9);
        first.UnknownS4List05.Add(5);
        first.UnknownS4List06.Add(6);
        first.UnknownU1S4PairList.Add(new FhmByteInt32Pair { UnknownU1 = 7, UnknownS4 = -8 });
        players.Players.Add(first);

        var second = new FhmPlayerRecord
        {
            FirstNameId = 201,
            SurnameId = 202,
            CommonNameId = 203,
            BirthDate = new FhmDate(2001, 4, 5),
            InternalIdentity = new FhmPlayerInternalIdentity(1),
        };
        second.PositionRatings.Centre = 14;
        second.RatingAttributes.Speed = 17;
        players.Players.Add(second);
        return players;
    }

    private static void Add(FhmSave save, IFhmSaveFile file) => save.Files.Add(file.RelativePath, file);

    private static string CreateTestRoot()
    {
        var root = Path.Combine(Environment.CurrentDirectory, "fhm-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }

    private static void AssertFoldersEqual(string expected, string actual)
    {
        var expectedFiles = GetFiles(expected);
        var actualFiles = GetFiles(actual);
        Assert.Equal(expectedFiles.Keys.Order(), actualFiles.Keys.Order());
        foreach (var relativePath in expectedFiles.Keys)
        {
            Assert.Equal(File.ReadAllBytes(expectedFiles[relativePath]), File.ReadAllBytes(actualFiles[relativePath]));
        }
    }

    private static void AssertFoldersEqualExcept(string expected, string actual, string except)
    {
        var expectedFiles = GetFiles(expected);
        var actualFiles = GetFiles(actual);
        Assert.Equal(expectedFiles.Keys.Order(), actualFiles.Keys.Order());
        foreach (var relativePath in expectedFiles.Keys.Where(path => !string.Equals(path, except, StringComparison.OrdinalIgnoreCase)))
        {
            Assert.Equal(File.ReadAllBytes(expectedFiles[relativePath]), File.ReadAllBytes(actualFiles[relativePath]));
        }
    }

    private static Dictionary<string, string> GetFiles(string root) =>
        Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
            .ToDictionary(
                path => Path.GetRelativePath(root, path).Replace('\\', '/'),
                StringComparer.OrdinalIgnoreCase);
}
