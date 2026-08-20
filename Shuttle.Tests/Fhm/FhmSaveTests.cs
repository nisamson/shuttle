using System.Buffers.Binary;
using Shuttle.BinarySerde.Common.QFormat;
using Shuttle.Fhm.Serde.Domain.Binary;
using Shuttle.Fhm.Serde.Domain.Files;
using Shuttle.Fhm.Serde.Domain.Model;
using Shuttle.Fhm.Serde.Domain.SaveFolder;
using Shuttle.Fhm.Serde.Wire.Leagues;
using Shuttle.Fhm.Serde.Wire.PlayerRoles;
using Shuttle.Fhm.Serde.Wire.StoredLines;
using Shuttle.Fhm.Serde.Wire.Tactics;
using Shuttle.Fhm.Serde.Wire.Teams;
using Shuttle.Fhm.Serde.Wire.Trades;
using Shuttle.Fhm.Serde.Wire.ZoneEvents;

namespace Shuttle.Tests.Fhm;

public sealed class FhmSaveTests
{
    [Fact]
    public void GameSettings_RoundTripAllEightyDocumentedFields()
    {
        var settings = new FhmGameSettingsFile();
        settings.Set(FhmGameSetting.Setting001, (byte)1);
        settings.Set(FhmGameSetting.Setting014, 12.5d);
        settings.Set(FhmGameSetting.Setting039, "nullable Qt text");
        settings.Set(FhmGameSetting.Setting072, (ushort)72);
        settings.Set(FhmGameSetting.Setting080, (byte)80);
        settings.ReferencedObjectId = -1;

        Assert.Equal(80, settings.Values.Count);
        Assert.Equal(80, Enum.GetValues<FhmGameSetting>().Length);
        Assert.Throws<ArgumentException>(() => settings.Set(FhmGameSetting.Setting001, (ushort)1));
        Assert.Throws<InvalidOperationException>(() => settings.Get<ushort>(FhmGameSetting.Setting001));
        var root = CreateTestRoot();
        try
        {
            var save = new FhmSave();
            save.Files.Add(settings.RelativePath, settings);
            new FhmSaveWriter().Write(save, root);
            var roundTripped = Assert.IsType<FhmGameSettingsFile>(new FhmSaveReader().Read(root).Files[settings.RelativePath]);
            Assert.Equal(12.5d, roundTripped.Get<double>(FhmGameSetting.Setting014));
            Assert.Equal("nullable Qt text", roundTripped.Get<string>(FhmGameSetting.Setting039));
            Assert.Equal(-1, roundTripped.ReferencedObjectId);
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
            File.WriteAllBytes(
                Path.Combine(root, "players.dat"),
                CreateVersionedCountHeader(57, 0));

            var exception = Assert.Throws<FhmFormatException>(() => new FhmSaveReader().Read(root));
            Assert.Contains("version 57", exception.Message);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void FolderReader_RejectsNegativePlayersCount()
    {
        var root = CreateTestRoot();
        try
        {
            File.WriteAllBytes(
                Path.Combine(root, "players.dat"),
                CreateVersionedCountHeader(FhmPlayersFile.CurrentVersion, -1));

            Assert.Throws<FhmFormatException>(() => new FhmSaveReader().Read(root));
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
            Assert.Equal(0, first.InternalIdentity);
            Assert.Equal(220765, first.ExportedPlayerId);
            Assert.Equal(5218, first.TeamId);
            Assert.Equal(15, first.FranchiseId);
            Assert.Equal((ushort)20, first.PositionRatings.Centre);
            Assert.Equal((ushort)65280, first.PositionRatings.UnknownRightWingRaw);
            Assert.Equal((byte)19, first.RatingAttributes.ShootingAccuracy);
            Assert.Null(first.UnusedString01);
            Assert.Equal(string.Empty, first.UnusedString02);
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
    public void PlayerRecord_ByteApi_RoundTripsTheBinarySerializerWireContract()
    {
        var source = CreateStructuredPlayersFile().Players[0];

        var result = FhmPlayerRecord.FromBytes(source.ToBytes());

        Assert.Equal(source.ToBytes(), result.ToBytes());
        Assert.Equal(source.ExportedPlayerId, result.ExportedPlayerId);
        Assert.Equal(source.UnknownString01, result.UnknownString01);
        Assert.Equal(source.UnknownRecordList01[0].Data.Value, result.UnknownRecordList01[0].Data.Value);
        Assert.Equal(source.PrimaryRole!.TendencyValue, result.PrimaryRole!.TendencyValue);
    }

    [Fact]
    public void PlayersFile_AnalyzeInternalIdentities_ReportsOrdinalInvariantAndViolations()
    {
        var valid = CreateStructuredPlayersFile();

        var validAnalysis = valid.AnalyzeInternalIdentities();

        Assert.True(validAnalysis.IsRecordOrdinalInvariant);
        Assert.Empty(validAnalysis.DuplicateIdentities);
        Assert.Empty(validAnalysis.NegativeRecords);
        Assert.Empty(validAnalysis.OutOfRangeRecords);
        Assert.Empty(validAnalysis.MissingIdentities);
        Assert.Empty(validAnalysis.OrdinalMismatches);

        var invalid = new FhmPlayersFile();
        invalid.Players.Add(new FhmPlayerRecord { InternalIdentity = 0 });
        invalid.Players.Add(new FhmPlayerRecord { InternalIdentity = 2 });
        invalid.Players.Add(new FhmPlayerRecord { InternalIdentity = 2 });
        invalid.Players.Add(new FhmPlayerRecord { InternalIdentity = FhmNullConstants.Null });

        var invalidAnalysis = invalid.AnalyzeInternalIdentities();

        Assert.False(invalidAnalysis.IsRecordOrdinalInvariant);
        Assert.Equal([2], invalidAnalysis.DuplicateIdentities);
        Assert.Equal([3], invalidAnalysis.NegativeRecords.Select(record => record.RecordOrdinal));
        Assert.Equal([3], invalidAnalysis.OutOfRangeRecords.Select(record => record.RecordOrdinal));
        Assert.Equal([1, 3], invalidAnalysis.MissingIdentities);
        Assert.Equal([1, 3], invalidAnalysis.OrdinalMismatches.Select(record => record.RecordOrdinal));
    }

    [Fact]
    public void EnumValue_PreservesFutureRawValue()
    {
        var unknown = new FhmEnumValue<FhmPlayingRole>(1234);

        Assert.False(unknown.IsKnown);
        Assert.Equal((ushort)1234, unknown.RawValue);
    }

    [Fact]
    public void TradeFiles_BinarySerializerOutputMatchesLegacyQtWireLayout()
    {
        var root = CreateTestRoot();
        try
        {
            var trade = new FhmTradeFile { VersionTag = 3 };
            var proposal = new FhmTradeRecord
            {
                Field0 = -1,
                Date = new FhmDate(2028, 7, 1),
                Field4 = 4,
                Field5 = -5,
                Flag = 6,
                Field14 = 7,
            };
            proposal.Fields1To3[0] = 1;
            proposal.Fields1To3[1] = -2;
            proposal.Fields1To3[2] = 3;
            proposal.InterleavedUnsignedFields[0] = 8;
            proposal.InterleavedUnsignedFields[1] = 9;
            proposal.InterleavedUnsignedFields[2] = 10;
            proposal.InterleavedUnsignedFields[3] = 11;
            proposal.Fields10And11[0] = 12;
            proposal.Fields10And11[1] = 13;
            proposal.Fields12And13[0] = -14;
            proposal.Fields12And13[1] = 15;
            proposal.Fields15And16[0] = -16;
            proposal.Fields15And16[1] = 17;
            for (var index = 0; index < proposal.AssetLists.Count; index++)
            {
                proposal.AssetLists[index].Add(index - 4);
            }

            proposal.PairLists[0].Add(new(-18, 19));
            proposal.PairLists[1].Add(new(20, 21));
            trade.Records.Add(proposal);

            var history = new FhmTradeHistoryFile { VersionTag = 22 };
            var completed = new FhmTradeHistoryRecord
            {
                Id = -23,
                Date = new FhmDate(2028, 7, 2),
                TeamA = 24,
                TeamB = -25,
                Flag = 26,
            };
            completed.Fields2And3[0] = 27;
            completed.Fields2And3[1] = -28;
            for (var index = 0; index < completed.AssetLists.Count; index++)
            {
                completed.AssetLists[index].Add(index + 29);
            }

            completed.DraftPickLists[0].Add(new(30, -31, 32, -33, 34, 35));
            completed.DraftPickLists[1].Add(new(36, -37, 38, -39, 40, 41));
            history.Records.Add(completed);

            var save = new FhmSave();
            Add(save, trade);
            Add(save, history);
            new FhmSaveWriter().Write(save, root);

            Assert.Equal(CreateSerializedTradeBytes(trade), File.ReadAllBytes(Path.Combine(root, "trade.dat")));
            Assert.Equal(CreateSerializedTradeHistoryBytes(history), File.ReadAllBytes(Path.Combine(root, "trade_history.dat")));

            var loaded = new FhmSaveReader().Read(root);
            var loadedProposal = Assert.Single(Assert.IsType<FhmTradeFile>(loaded.Files["trade.dat"]).Records);
            Assert.Equal(proposal.AssetLists.SelectMany(list => list), loadedProposal.AssetLists.SelectMany(list => list));
            Assert.Equal(proposal.PairLists.SelectMany(list => list), loadedProposal.PairLists.SelectMany(list => list));
            var loadedCompleted = Assert.Single(Assert.IsType<FhmTradeHistoryFile>(loaded.Files["trade_history.dat"]).Records);
            Assert.Equal(completed.AssetLists.SelectMany(list => list), loadedCompleted.AssetLists.SelectMany(list => list));
            Assert.Equal(completed.DraftPickLists.SelectMany(list => list), loadedCompleted.DraftPickLists.SelectMany(list => list));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Theory]
    [InlineData("trade.dat")]
    [InlineData("trade_history.dat")]
    public void TradeFiles_RejectNegativeRecordCounts(string fileName)
    {
        var root = CreateTestRoot();
        try
        {
            File.WriteAllBytes(Path.Combine(root, fileName), CreateVersionedCountHeader(3, -1));

            Assert.Throws<FhmFormatException>(() => new FhmSaveReader().Read(root));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
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
        settings.WriteTo(stream);
        stream.Position = 0;
        var roundTripped = FhmTeamTacticsSettings.ReadFrom(stream);
        Assert.Equal((ushort)99, roundTripped.FinalOffensiveOrientation.RawValue);
        Assert.False(roundTripped.FinalOffensiveOrientation.IsKnown);
        Assert.Equal((byte)1, roundTripped.Selectors[4].DelayedUseOwnSettingsFlags[3]);
        Assert.Equal((ushort)88, roundTripped.Selectors[21].SystemIds[11]);
        Assert.Equal((ushort)3, roundTripped.Tendencies[0].Values[(int)FhmTendency.Shooting]);
    }

    [Fact]
    public void TeamTacticsFile_BinarySerializerRoute_PreservesLegacyQtWireLayout()
    {
        var root = CreateTestRoot();
        var source = Path.Combine(root, "source");
        var destination = Path.Combine(root, "destination");
        try
        {
            Directory.CreateDirectory(source);
            var sourcePath = Path.Combine(source, "team_tactics.dat");
            using (var stream = File.Create(sourcePath))
            {
                FhmTeamTacticsFileSerializer.Serialize(stream, new()
                {
                    VersionTag = 2,
                    RecordCount = 2,
                    Records =
                    [
                        new()
                        {
                            GlobalId = 42,
                            ZoneGroupRaw = (int)FhmTacticZone.OffensiveZoneAttack,
                            Name = new() { Value = "Cycle" },
                            RatingA = 3,
                            RatingB = 4,
                        },
                        new()
                        {
                            GlobalId = -1,
                            ZoneGroupRaw = 99,
                            Name = new(),
                            RatingA = -5,
                            RatingB = 6,
                        },
                    ],
                });
            }

            var loaded = new FhmSaveReader().Read(source);
            var tactics = Assert.IsType<FhmTeamTacticsFile>(loaded.Files["team_tactics.dat"]);
            Assert.Equal(2, tactics.VersionTag);
            Assert.Equal(2, tactics.Records.Count);
            Assert.Equal("Cycle", tactics.Records[0].Name);
            Assert.Equal(-1, tactics.Records[1].GlobalId);
            Assert.Null(tactics.Records[1].Name);
            Assert.Equal(-5, tactics.Records[1].RatingA);

            new FhmSaveWriter().Write(loaded, destination);
            Assert.Equal(File.ReadAllBytes(sourcePath), File.ReadAllBytes(Path.Combine(destination, "team_tactics.dat")));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void ResidualCodecs_BinarySerializerRoutes_PreserveLegacyQtWireLayouts()
    {
        var root = CreateTestRoot();
        var source = Path.Combine(root, "source");
        var destination = Path.Combine(root, "destination");
        try
        {
            Directory.CreateDirectory(source);
            var leaguesPath = Path.Combine(source, "leagues.dat");
            var tacticsPath = Path.Combine(source, "tactics.dat");
            var zoneEventsPath = Path.Combine(source, "zone_event_mod.dat");
            using (var stream = File.Create(leaguesPath))
            {
                FhmLeaguesFileSerializer.SerializeHeader(stream, new() { VersionTag = 4, RecordCount = 1 });
                FhmLeaguesFileSerializer.SerializeRecord(stream, new()
                {
                    LeagueId = 15,
                    Flag0 = 1,
                    Flag1 = 2,
                    Flag2 = 3,
                    Name = new() { Value = "Synthetic Hockey League" },
                    ShortName = new(),
                    Abbreviation = new() { Value = "SHL" },
                    Nickname = new() { Value = string.Empty },
                    TypeParentId = 4,
                    TypeLevelId = 5,
                    ConfigDouble0 = 1.25,
                    EarlyInt0 = 6,
                    EarlyInt1 = 7,
                    ConfigDoublesPrimary = Enumerable.Range(0, 17).Select(index => index + 0.5).ToList(),
                    ConfigInt0 = 8,
                    ConfigInt1 = 9,
                    ConfigUInt160 = 10,
                    ConfigInt2 = 11,
                    ConfigUInt161 = 12,
                    FoundingDate = new() { Year = 2020, Month = 1, Day = 2 },
                });
                stream.Write([0xDE, 0xAD]);
            }

            using (var stream = File.Create(tacticsPath))
            {
                FhmTacticsFileSerializer.SerializeHeader(stream, new() { Version = 13, TacticCount = 2 });
                stream.Write([0xBE, 0xEF]);
            }

            using (var stream = File.Create(zoneEventsPath))
            {
                FhmZoneEventModifiersFileSerializer.SerializeHeader(stream, new() { Version = 14, ZoneCount = 15 });
                FhmZoneEventModifiersFileSerializer.SerializeGrid(stream, new() { ByteLength = 3, Data = [1, 2, 3] });
                FhmZoneEventModifiersFileSerializer.SerializeGrid(stream, new() { ByteLength = 0, Data = [] });
            }

            var loaded = new FhmSaveReader().Read(source);
            var leagues = Assert.IsType<FhmLeaguesFile>(loaded.Files["leagues.dat"]);
            Assert.Equal(4, leagues.VersionTag);
            Assert.Equal("Synthetic Hockey League", leagues.League.Name);
            Assert.Null(leagues.League.ShortName);
            Assert.Equal([0xDE, 0xAD], leagues.League.OpaqueLeagueBody);
            var tactics = Assert.IsType<FhmTacticsFile>(loaded.Files["tactics.dat"]);
            Assert.Equal(13, tactics.Version);
            Assert.Equal(2, tactics.TacticCount);
            Assert.Equal([0xBE, 0xEF], tactics.RecordsOpaque);
            var zoneEvents = Assert.IsType<FhmZoneEventModifiersFile>(loaded.Files["zone_event_mod.dat"]);
            Assert.Equal(14, zoneEvents.Version);
            Assert.Equal(15, zoneEvents.ZoneCount);
            Assert.Equal([[1, 2, 3], []], zoneEvents.ModifierGrids.Select(grid => grid.Data));

            new FhmSaveWriter().Write(loaded, destination);
            Assert.Equal(File.ReadAllBytes(leaguesPath), File.ReadAllBytes(Path.Combine(destination, "leagues.dat")));
            Assert.Equal(File.ReadAllBytes(tacticsPath), File.ReadAllBytes(Path.Combine(destination, "tactics.dat")));
            Assert.Equal(File.ReadAllBytes(zoneEventsPath), File.ReadAllBytes(Path.Combine(destination, "zone_event_mod.dat")));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void StoredLinesFile_BinarySerializerRoute_RoundTripsAllGroupsLosslessly()
    {
        var root = CreateTestRoot();
        var source = Path.Combine(root, "source");
        var destination = Path.Combine(root, "destination");
        try
        {
            Directory.CreateDirectory(source);
            var sourcePath = Path.Combine(source, "stored_lines.dat");
            using (var stream = File.Create(sourcePath))
            {
                FhmStoredLinesFileSerializer.Serialize(stream, new()
                {
                    StoredLineCount = 1,
                    StoredLines =
                    [
                        new()
                        {
                            Name = new() { Value = "All groups" },
                            PlayerGroups = Enumerable.Range(0, Enum.GetValues<FhmLineGroup>().Length)
                                .Select(group => ToWireList([group == 0 ? -1 : group]))
                                .ToList(),
                            UnitLocks = Enumerable.Range(0, Enum.GetValues<FhmLineGroup>().Length)
                                .Select(group => ToWireList([(byte)group, (byte)(255 - group)]))
                                .ToList(),
                        },
                    ],
                });
            }

            var loaded = new FhmSaveReader().Read(source);
            var storedLines = Assert.IsType<FhmStoredLinesFile>(loaded.Files["stored_lines.dat"]);
            var line = Assert.Single(storedLines.StoredLines);
            Assert.Equal("All groups", line.Name);
            Assert.Equal(13, line.PlayerGroups.Count);
            Assert.Equal(13, line.UnitLocks.Count);
            for (var group = 0; group < Enum.GetValues<FhmLineGroup>().Length; group++)
            {
                Assert.Equal([group == 0 ? -1 : group], line.PlayerGroups[group]);
                Assert.Equal([(byte)group, (byte)(255 - group)], line.UnitLocks[group]);
            }

            new FhmSaveWriter().Write(loaded, destination);
            Assert.Equal(File.ReadAllBytes(sourcePath), File.ReadAllBytes(Path.Combine(destination, "stored_lines.dat")));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void StoredLinesFile_WriteRejectsMissingPlayerGroup()
    {
        var root = CreateTestRoot();
        try
        {
            var storedLines = new FhmStoredLinesFile();
            var line = new FhmStoredLine();
            line.PlayerGroups.RemoveAt(line.PlayerGroups.Count - 1);
            storedLines.StoredLines.Add(line);
            var save = new FhmSave();
            Add(save, storedLines);

            Assert.Throws<FhmFormatException>(() => new FhmSaveWriter().Write(save, root));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void PlayerRolesFile_BinarySerializerRoute_PreservesLegacyQtWireLayout()
    {
        var root = CreateTestRoot();
        var source = Path.Combine(root, "source");
        var destination = Path.Combine(root, "destination");
        try
        {
            Directory.CreateDirectory(source);
            var sourcePath = Path.Combine(source, "player_roles.dat");
            using (var stream = File.Create(sourcePath))
            {
                FhmPlayerRolesFileSerializer.Serialize(stream, new()
                {
                    VersionTag = 7,
                    RecordCount = 1,
                    Records =
                    [
                        new()
                        {
                            RoleId = 42,
                            Name = new(),
                            WeightGroupA = Enumerable.Range(10, 8).ToList(),
                            WeightGroupB = Enumerable.Range(20, 13).ToList(),
                            WeightGroupC = Enumerable.Range(30, 17).ToList(),
                            WeightGroupD = Enumerable.Range(40, 4).ToList(),
                            AppliesToForwards = 1,
                            AppliesToDefencemen = 0,
                            AppliesToGoalies = 1,
                            RoleFlags = 0xA5,
                            PositionCategory = 0x1234,
                            ShortName = new() { Value = "R" },
                            TuningValueA = 2,
                            TuningValueB = 3,
                            Description = new() { Value = "Role description" },
                            TuningValueC = 4,
                            WeightGroupE = Enumerable.Range(50, 19).ToList(),
                            WeightGroupF = Enumerable.Range(70, 9).ToList(),
                            IndexListA = ToWireList<byte>([9, 8]),
                            IndexListB = ToWireList<byte>([]),
                            IndexListC = ToWireList<byte>([7]),
                            IndexListD = ToWireList<byte>([6, 5, 4]),
                        },
                    ],
                });
            }

            var loaded = new FhmSaveReader().Read(source);
            var playerRoles = Assert.IsType<FhmPlayerRolesFile>(loaded.Files["player_roles.dat"]);
            Assert.Equal(7, playerRoles.VersionTag);
            var role = Assert.Single(playerRoles.Records);
            Assert.Equal(42, role.RoleId);
            Assert.Null(role.Name);
            Assert.Equal(10, role.WeightGroupA[0]);
            Assert.Equal(32, role.WeightGroupC[2]);
            Assert.Equal((byte)0xA5, role.RoleFlags);
            Assert.Equal((ushort)0x1234, role.PositionCategory);
            Assert.Equal("R", role.ShortName);
            Assert.Equal("Role description", role.Description);
            Assert.Equal(68, role.WeightGroupE[18]);
            Assert.Equal([9, 8], role.IndexLists[0]);
            Assert.Empty(role.IndexLists[1]);
            Assert.Equal([7], role.IndexLists[2]);
            Assert.Equal([6, 5, 4], role.IndexLists[3]);

            new FhmSaveWriter().Write(loaded, destination);
            Assert.Equal(File.ReadAllBytes(sourcePath), File.ReadAllBytes(Path.Combine(destination, "player_roles.dat")));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
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
            InternalIdentity = 0,
            UnknownString01 = "face-key",
            UnusedString01 = null,
            UnusedString02 = string.Empty,
            UnknownDate01 = new FhmDate(2026, 8, 8),
            UnknownString04 = "note",
            ExportedPlayerId = 220765,
            TeamId = 5218,
            FranchiseId = 15,
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
            InternalIdentity = 1,
        };
        second.PositionRatings.Centre = 14;
        second.RatingAttributes.Speed = 17;
        players.Players.Add(second);
        return players;
    }

    private static void Add(FhmSave save, IFhmSaveFile file) => save.Files.Add(file.RelativePath, file);

    private static byte[] CreateSerializedTradeBytes(FhmTradeFile trade)
    {
        using var stream = new MemoryStream();
        FhmTradeFileSerializer.Serialize(stream, new()
        {
            VersionTag = trade.VersionTag,
            TradeCount = trade.Records.Count,
            Records = trade.Records.Select(record => new FhmTradeRecordData
            {
                Field0 = record.Field0,
                Date = ToWireDate(record.Date),
                Field1 = record.Fields1To3[0],
                Field2 = record.Fields1To3[1],
                Field3 = record.Fields1To3[2],
                Field4 = record.Field4,
                Field5 = record.Field5,
                AssetList0 = ToWireList(record.AssetLists[0]),
                AssetList1 = ToWireList(record.AssetLists[1]),
                AssetList2 = ToWireList(record.AssetLists[2]),
                AssetList3 = ToWireList(record.AssetLists[3]),
                AssetList4 = ToWireList(record.AssetLists[4]),
                InterleavedUnsignedField0 = record.InterleavedUnsignedFields[0],
                AssetList5 = ToWireList(record.AssetLists[5]),
                InterleavedUnsignedField1 = record.InterleavedUnsignedFields[1],
                AssetList6 = ToWireList(record.AssetLists[6]),
                InterleavedUnsignedField2 = record.InterleavedUnsignedFields[2],
                AssetList7 = ToWireList(record.AssetLists[7]),
                InterleavedUnsignedField3 = record.InterleavedUnsignedFields[3],
                Field10 = record.Fields10And11[0],
                Field11 = record.Fields10And11[1],
                Field12 = record.Fields12And13[0],
                Field13 = record.Fields12And13[1],
                Flag = record.Flag,
                Field14 = record.Field14,
                Field15 = record.Fields15And16[0],
                Field16 = record.Fields15And16[1],
                PairList0 = ToWireList(record.PairLists[0].Select(pair => new FhmIntUInt16PairData { Id = pair.Id, Value = pair.Value })),
                PairList1 = ToWireList(record.PairLists[1].Select(pair => new FhmIntUInt16PairData { Id = pair.Id, Value = pair.Value })),
            }).ToList(),
        });

        return stream.ToArray();
    }

    private static byte[] CreateSerializedTradeHistoryBytes(FhmTradeHistoryFile history)
    {
        using var stream = new MemoryStream();
        FhmTradeHistoryFileSerializer.Serialize(stream, new()
        {
            VersionTag = history.VersionTag,
            TradeCount = history.Records.Count,
            Records = history.Records.Select(record => new FhmTradeHistoryRecordData
            {
                Id = record.Id,
                Date = ToWireDate(record.Date),
                TeamA = record.TeamA,
                TeamB = record.TeamB,
                Field2 = record.Fields2And3[0],
                Field3 = record.Fields2And3[1],
                AssetList0 = ToWireList(record.AssetLists[0]),
                AssetList1 = ToWireList(record.AssetLists[1]),
                AssetList2 = ToWireList(record.AssetLists[2]),
                AssetList3 = ToWireList(record.AssetLists[3]),
                AssetList4 = ToWireList(record.AssetLists[4]),
                AssetList5 = ToWireList(record.AssetLists[5]),
                AssetList6 = ToWireList(record.AssetLists[6]),
                AssetList7 = ToWireList(record.AssetLists[7]),
                AssetList8 = ToWireList(record.AssetLists[8]),
                AssetList9 = ToWireList(record.AssetLists[9]),
                DraftPickList0 = ToWireList(record.DraftPickLists[0].Select(ToWireDraftPick)),
                DraftPickList1 = ToWireList(record.DraftPickLists[1].Select(ToWireDraftPick)),
                Flag = record.Flag,
            }).ToList(),
        });

        return stream.ToArray();
    }

    private static QDate ToWireDate(FhmDate value) =>
        new() { Year = value.Year, Month = value.Month, Day = value.Day };

    private static QList<T> ToWireList<T>(IEnumerable<T> values)
    {
        var items = values.ToList();
        return new() { Length = items.Count, Items = items };
    }

    private static FhmDraftPickDescriptorData ToWireDraftPick(FhmDraftPickDescriptor value) =>
        new()
        {
            FieldUInt16 = value.FieldUInt16,
            FieldInt0 = value.FieldInt0,
            FieldInt1 = value.FieldInt1,
            FieldInt2 = value.FieldInt2,
            FieldUInt160 = value.FieldUInt160,
            FieldUInt161 = value.FieldUInt161,
        };

    private static byte[] CreateVersionedCountHeader(int version, int count)
    {
        var bytes = new byte[sizeof(int) * 2];
        BinaryPrimitives.WriteInt32BigEndian(bytes, version);
        BinaryPrimitives.WriteInt32BigEndian(bytes.AsSpan(sizeof(int)), count);
        return bytes;
    }

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
