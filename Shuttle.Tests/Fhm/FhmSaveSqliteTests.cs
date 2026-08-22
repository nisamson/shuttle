using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Shuttle.Fhm.Serde.Domain.Binary;
using Shuttle.Fhm.Serde.Domain.Files;
using Shuttle.Fhm.Serde.Domain.Model;
using Shuttle.Fhm.Serde.Domain.SaveFolder;
using Shuttle.Fhm.Serde.Sqlite;
using Shuttle.Fhm.Serde.Sqlite.Entities;

namespace Shuttle.Tests.Fhm;

public sealed class FhmSaveSqliteTests
{
    [Fact]
    public async Task OpenAsync_AppliesCurrentSchemaMigration()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var root = CreateTestRoot();
        try
        {
            var database = Path.Combine(root, "save.sqlite");
            await using (var context = await FhmSaveSqliteContext.OpenAsync(database, cancellationToken))
            {
                Assert.NotEmpty(await context.Database.GetAppliedMigrationsAsync(cancellationToken));
            }
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task WriteAsync_StoresWireSentinelsAsNull()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var root = CreateTestRoot();
        try
        {
            var database = Path.Combine(root, "save.sqlite");
            await new FhmSaveSqliteWriter().WriteAsync(CreateSave(), database, cancellationToken);
            await using var context = new FhmSaveSqliteContext(FhmSaveSqliteContext.CreateOptions(database));
            await context.Database.OpenConnectionAsync(cancellationToken);
            try
            {
                await using var command = context.Database.GetDbConnection().CreateCommand();
                command.CommandText = "SELECT CommonNameId FROM Players WHERE RecordOrdinal = 0;";
                Assert.IsType<DBNull>(await command.ExecuteScalarAsync(cancellationToken));

                command.CommandText = "SELECT AffiliateParentId FROM Teams WHERE TeamId = 1;";
                Assert.IsType<DBNull>(await command.ExecuteScalarAsync(cancellationToken));
            }
            finally
            {
                await context.Database.CloseConnectionAsync();
            }
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task ImportExport_UneditedSavePreservesEveryFileByte()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var root = CreateTestRoot();
        try
        {
            var source = Path.Combine(root, "source");
            var destination = Path.Combine(root, "destination");
            var database = Path.Combine(root, "save.sqlite");
            var save = CreateSave();
            new FhmSaveWriter().Write(save, source);

            var sourceSave = new FhmSaveReader().Read(source);
            var sqliteWriter = new FhmSaveSqliteWriter();
            await sqliteWriter.WriteAsync(sourceSave, database, cancellationToken);
            await Assert.ThrowsAsync<IOException>(() => sqliteWriter.WriteAsync(sourceSave, database, cancellationToken));
            await sqliteWriter.WriteAsync(
                sourceSave,
                database,
                new FhmSaveSqliteWriteOptions { Overwrite = true },
                cancellationToken);
            var exported = await new FhmSaveSqliteReader().ReadAsync(database, cancellationToken);
            new FhmSaveWriter().Write(exported, destination);

            AssertFoldersEqual(source, destination);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task ImportExport_AppliesEditableEntitiesAndPreservesOpaqueContent()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var root = CreateTestRoot();
        try
        {
            var database = Path.Combine(root, "save.sqlite");
            var destination = Path.Combine(root, "destination");
            await new FhmSaveSqliteWriter().WriteAsync(CreateSave(), database, cancellationToken);

            await using (var context = new FhmSaveSqliteContext(FhmSaveSqliteContext.CreateOptions(database)))
            {
                await context.Database.ExecuteSqlRawAsync(
                    "UPDATE Names SET Text = 'Edited'",
                    cancellationToken);
                await context.Database.ExecuteSqlRawAsync(
                    "UPDATE Players SET ExternalId = 99, PrimaryContractRole = 8, SupplementaryContractRole = 3",
                    cancellationToken);
                await context.Database.ExecuteSqlRawAsync(
                    "UPDATE PlayerAttributes SET Passing = 19",
                    cancellationToken);
                await context.Database.ExecuteSqlRawAsync(
                    "UPDATE PlayerContractYears SET MajorLeagueSalary = 1647321 WHERE YearNumber = 1",
                    cancellationToken);
                await context.Database.ExecuteSqlRawAsync(
                    "UPDATE TacticalRoles SET Name = 'Edited Tactical Role' WHERE RoleId = 8",
                    cancellationToken);
                await context.Database.ExecuteSqlRawAsync(
                    "UPDATE TacticalRoleWeights SET Value = 77 WHERE RoleId = 8 AND \"Group\" = 0 AND Ordinal = 0",
                    cancellationToken);
                await context.Database.ExecuteSqlRawAsync(
                    "UPDATE PlayerTacticalRoleTendencyValues SET UseOverride = 1, Value = 3 WHERE PlayerInternalId = 0 AND Slot = 0 AND Tendency = 0",
                    cancellationToken);
                await context.Database.ExecuteSqlRawAsync(
                    """
                    UPDATE Personnel
                    SET NicknameNameId = 1,
                        BirthDate = '1981-02-03',
                        NationalityId = 151,
                        BirthCityId = 71152,
                        Reputation = 75,
                        BasedInLocationId = 123,
                        CoachingGoalies = 17,
                        LineMatchingTendency = 4,
                        GoalieHandlingTendency = 4,
                        VeteranPreference = 4,
                        InnovationTendency = 4,
                        LoyaltyTendency = 0,
                        Salary = 400123,
                        Job = 0
                    WHERE PersonnelId = 0
                    """,
                    cancellationToken);
                await context.Database.ExecuteSqlRawAsync(
                    "UPDATE Personnel SET TeamId = NULL WHERE PersonnelId = 1",
                    cancellationToken);
                await context.Database.ExecuteSqlRawAsync(
                    "UPDATE Teams SET City = 'Edited City'",
                    cancellationToken);
                await context.Database.ExecuteSqlRawAsync(
                    "UPDATE GameSettings SET ByteValue = 7 WHERE SettingOrdinal = 0",
                    cancellationToken);
                await context.Database.ExecuteSqlRawAsync(
                    "UPDATE StoredLineSlots SET PlayerInternalId = NULL",
                    cancellationToken);
                await context.Database.ExecuteSqlRawAsync(
                    "UPDATE TacticSystems SET RatingA = 9",
                    cancellationToken);
                var teamTactics = context.TeamTactics.Single();
                byte[] editedTeamTactics = [.. teamTactics.SerializedSettings];
                editedTeamTactics[1] = 1;
                await context.Database.ExecuteSqlRawAsync(
                    "UPDATE TeamTactics SET SerializedSettings = {0}",
                    new object[] { editedTeamTactics },
                    cancellationToken);
                await context.Database.ExecuteSqlRawAsync(
                    "UPDATE TeamActiveLineSlots SET PlayerInternalId = NULL WHERE TeamId = 1 AND \"Group\" = 0 AND SlotOrdinal = 0",
                    cancellationToken);
                await context.Database.ExecuteSqlRawAsync(
                    "UPDATE TacticTemplates SET DisplayName = 'Edited Template'",
                    cancellationToken);
                await context.Database.ExecuteSqlRawAsync(
                    "UPDATE SetPlays SET Data = X'070809' WHERE IsExtraRecord = 0",
                    cancellationToken);
                await context.Database.ExecuteSqlRawAsync(
                    "UPDATE ModifierCatalogues SET Data = X'0504' WHERE RelativePath = 'shot_type_mod.dat'",
                    cancellationToken);
                await context.Database.ExecuteSqlRawAsync(
                    "UPDATE ModifierCatalogues SET Data = X'030201' WHERE RelativePath = 'zone_event_mod.dat'",
                    cancellationToken);
                await context.Database.ExecuteSqlRawAsync(
                    "UPDATE Tactics SET RecordsOpaque = X'010203'",
                    cancellationToken);
            }

            var exported = await new FhmSaveSqliteReader().ReadAsync(database, cancellationToken);
            new FhmSaveWriter().Write(exported, destination);
            var reloaded = new FhmSaveReader().Read(destination);

            Assert.Equal("Edited", Assert.IsType<FhmNamesFile>(reloaded.Files["names.dat"]).MasterNames[0].Text);
            var player = Assert.Single(Assert.IsType<FhmPlayersFile>(reloaded.Files["players.dat"]).Players);
            Assert.Equal(99, player.ExportedPlayerId);
            Assert.Equal(FhmPlayingRole.Playmaker, player.PrimaryContractRole.Value);
            Assert.Equal(FhmSquadStatus.Leader, player.SupplementaryContractRole.Value);
            Assert.Equal((byte)19, player.RatingAttributes.Passing);
            Assert.Equal(1_647_321, Assert.Single(player.Contracts).Salaries[0].MajorLeagueSalary);
            Assert.Equal(345_000, Assert.Single(player.Contracts).Salaries[0].MinorLeagueSalary);
            Assert.Null(Assert.Single(player.Contracts).Salaries[1].MajorLeagueSalary);
            var tacticalRoles = Assert.IsType<FhmPlayerRolesFile>(reloaded.Files["player_roles.dat"]);
            var tacticalRole = Assert.Single(tacticalRoles.Records);
            Assert.Equal("Edited Tactical Role", tacticalRole.Name);
            Assert.Equal(77, tacticalRole.WeightGroupA[0]);
            Assert.Equal((byte)1, player.TacticalRole!.AttackingUseOverride);
            Assert.Equal((ushort)3, player.TacticalRole.AttackingTendencyValue);
            var personnel = Assert.IsType<FhmPersonnelFile>(reloaded.Files["personal.dat"]).Records[0];
            Assert.Equal<int?>(17, personnel.CoachingGoalies);
            Assert.Equal(1, personnel.FirstNameNameId);
            Assert.Equal(1, personnel.NicknameNameId);
            Assert.Equal(new FhmDate(1981, 2, 3), personnel.BirthDate);
            Assert.Equal((ushort)151, personnel.NationalityId);
            Assert.Equal(71_152, personnel.BirthCityId);
            Assert.Equal((ushort)75, personnel.Reputation);
            Assert.Equal((ushort)123, personnel.BasedInLocationId);
            Assert.Equal(400_123, personnel.Salary);
            Assert.Equal(FhmPersonnelJob.GmHeadCoach, personnel.Job);
            Assert.Equal(FhmLineMatchingTendency.VeryAggressive, personnel.LineMatchingTendency);
            Assert.Equal(FhmGoalieHandlingTendency.VeryAggressive, personnel.GoalieHandlingTendency);
            Assert.Equal(FhmVeteranPreference.LovesVeterans, personnel.VeteranPreference);
            Assert.Equal(FhmInnovationTendency.VeryInnovative, personnel.InnovationTendency);
            Assert.Equal(FhmLoyaltyTendency.VeryRuthless, personnel.LoyaltyTendency);
            Assert.Null(Assert.IsType<FhmPersonnelFile>(reloaded.Files["personal.dat"]).Records[1].TeamRecordIndex);
            var reloadedTeam = Assert.Single(Assert.IsType<FhmTeamsFile>(reloaded.Files["teams.dat"]).Teams);
            Assert.Equal(0, reloadedTeam.Tail.GeneralManagerPersonnelId);
            Assert.Equal(0, reloadedTeam.Tail.HeadCoachPersonnelId);
            Assert.Equal(FhmNullConstants.Null, reloadedTeam.ActiveLines.Lists[0].PlayerReferences[0]);
            Assert.Equal("Edited City", Assert.Single(Assert.IsType<FhmTeamsFile>(reloaded.Files["teams.dat"]).Teams).City);
            Assert.Equal((byte)7, Assert.IsType<FhmGameSettingsFile>(reloaded.Files["game_settings.dat"]).Get<byte>(FhmGameSetting.Setting001));
            Assert.Equal(FhmNullConstants.Null, Assert.IsType<FhmStoredLinesFile>(reloaded.Files["stored_lines.dat"]).StoredLines[0].PlayerGroups[0][0]);
            Assert.Equal(9, Assert.Single(Assert.IsType<FhmTeamTacticsFile>(reloaded.Files["team_tactics.dat"]).Records).RatingA);
            Assert.Equal((ushort)1, Assert.Single(Assert.IsType<FhmTeamsFile>(reloaded.Files["teams.dat"]).Teams).Tail.Tactics.TeamValue1);
            Assert.Equal("Edited Template", Assert.Single(Assert.IsType<FhmTacticTemplatesFile>(reloaded.Files["tactic_templates.dat"]).Templates).DisplayName);
            Assert.Equal([7, 8, 9], Assert.Single(Assert.IsType<FhmSetPlayFile>(reloaded.Files["set_play_5on4.dat"]).Formations).Data);
            Assert.Equal([5, 4], Assert.Single(Assert.IsType<FhmLengthPrefixedCatalogueFile>(reloaded.Files["shot_type_mod.dat"]).Blocks).Data);
            Assert.Equal([3, 2, 1], Assert.Single(Assert.IsType<FhmZoneEventModifiersFile>(reloaded.Files["zone_event_mod.dat"]).ModifierGrids).Data);
            Assert.Equal([1, 2, 3], Assert.IsType<FhmTacticsFile>(reloaded.Files["tactics.dat"]).RecordsOpaque);
            Assert.Equal([10, 20, 30], Assert.Single(reloaded.OpaqueFiles).Content);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task Entities_ExposeDomainNavigationsAndValueObjects()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var root = CreateTestRoot();
        try
        {
            var database = Path.Combine(root, "save.sqlite");
            await new FhmSaveSqliteWriter().WriteAsync(CreateSave(), database, cancellationToken);
            await using var context = new FhmSaveSqliteContext(FhmSaveSqliteContext.CreateOptions(database));

            var player = await context.Players
                .Include(value => value.TacticalRoleAssignments)
                    .ThenInclude(value => value.Role)
                .Include(value => value.TacticalRoleAssignments)
                    .ThenInclude(value => value.Tendencies)
                .Include(value => value.Team)
                    .ThenInclude(value => value!.ActiveLineSlots)
                .SingleAsync(cancellationToken);
            Assert.Equal("Original", player.FirstNameText);
            Assert.Equal("Original", player.SurnameText);
            Assert.Equal(new DateOnly(2000, 1, 1), player.BirthDate);
            Assert.Equal(1, player.Team?.TeamId);
            Assert.NotNull(player.Attributes);
            Assert.Equal(player.PositionAffinity.Goalie, player.PositionAffinity.GetRating(player.PrimaryPosition));
            Assert.Equal(FhmPlayingRole.Goalscorer, player.PrimaryContractRole);
            Assert.Equal(FhmSquadStatus.StarPlayer, player.SupplementaryContractRole);
            var tacticalAssignment = Assert.Single(player.TacticalRoleAssignments);
            Assert.Equal(PlayerRoleSlot.Tactical, tacticalAssignment.Slot);
            Assert.Equal("Synthetic Tactical Role", tacticalAssignment.Role.Name);
            Assert.Equal((ushort)3, Assert.Single(
                tacticalAssignment.Tendencies,
                value => value.Tendency == PlayerRoleTendency.Attacking).Value);
            var activeLineSlot = Assert.Single(player.Team!.ActiveLineSlots);
            Assert.Equal(FhmLineGroup.EvenStrengthForwards, activeLineSlot.Group);
            Assert.Same(player, activeLineSlot.Player);
            var contract = Assert.Single(player.Contracts);
            Assert.Collection(
                contract.Years.OrderBy(value => value.YearNumber),
                year =>
                {
                    Assert.Equal(1, year.YearNumber);
                    Assert.Equal(1_645_340, year.MajorLeagueSalary);
                    Assert.Equal(345_000, year.MinorLeagueSalary);
                },
                year => Assert.Null(year.MajorLeagueSalary),
                year => Assert.Null(year.MajorLeagueSalary),
                year => Assert.Null(year.MajorLeagueSalary),
                year => Assert.Null(year.MajorLeagueSalary),
                year => Assert.Null(year.MajorLeagueSalary),
                year => Assert.Null(year.MajorLeagueSalary),
                year => Assert.Null(year.MajorLeagueSalary),
                year => Assert.Null(year.MajorLeagueSalary),
                year => Assert.Null(year.MajorLeagueSalary),
                year => Assert.Null(year.MajorLeagueSalary),
                year => Assert.Null(year.MajorLeagueSalary),
                year => Assert.Null(year.MajorLeagueSalary),
                year => Assert.Null(year.MajorLeagueSalary));
            var playerTeam = Assert.IsType<Team>(player.Team);
            Assert.Equal(2, playerTeam.Staff.Count);
            Assert.Equal(0, playerTeam.GeneralManager?.PersonnelId);
            Assert.Equal(1, playerTeam.HeadCoach?.PersonnelId);
            Assert.Equal("Original", playerTeam.GeneralManager?.Surname.Text);

            var storedLine = await context.StoredLines
                .Include(value => value.Slots)
                .SingleAsync(cancellationToken);
            Assert.Equal(1, storedLine.Team?.TeamId);
            Assert.Equal(player.InternalId, Assert.Single(storedLine.Slots).Player?.InternalId);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task Import_RefusesNonAdapterDatabaseWithoutAddingTables()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var root = CreateTestRoot();
        try
        {
            var database = Path.Combine(root, "existing.sqlite");
            await using (var connection = new SqliteConnection($"Data Source={database};Pooling=False"))
            {
                await connection.OpenAsync(cancellationToken);
                await using var command = connection.CreateCommand();
                command.CommandText = "CREATE TABLE ExistingData (Id INTEGER PRIMARY KEY);";
                await command.ExecuteNonQueryAsync(cancellationToken);
            }

            var exception = await Assert.ThrowsAsync<InvalidDataException>(() =>
                new FhmSaveSqliteWriter().WriteAsync(
                    CreateSave(),
                    database,
                    new FhmSaveSqliteWriteOptions { Overwrite = true },
                    cancellationToken));
            Assert.Contains("not an adapter database", exception.Message);

            await using (var verification = new SqliteConnection($"Data Source={database};Pooling=False"))
            {
                await verification.OpenAsync(cancellationToken);
                await using var verificationCommand = verification.CreateCommand();
                verificationCommand.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = 'SaveManifest';";
                Assert.Equal(0L, Convert.ToInt64(await verificationCommand.ExecuteScalarAsync(cancellationToken)));
            }
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task Schema_RejectsInvalidPersonnelPersonalityTendency()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var root = CreateTestRoot();
        try
        {
            var database = Path.Combine(root, "save.sqlite");
            await new FhmSaveSqliteWriter().WriteAsync(CreateSave(), database, cancellationToken);
            await using var context = new FhmSaveSqliteContext(FhmSaveSqliteContext.CreateOptions(database));

            var exception = await Assert.ThrowsAsync<SqliteException>(
                () => context.Database.ExecuteSqlRawAsync(
                    "UPDATE Personnel SET LoyaltyTendency = 5 WHERE PersonnelId = 0",
                    cancellationToken));

            Assert.Equal(19, exception.SqliteErrorCode);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task Export_RejectsDirectSqlIdentityAndValidationChanges()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var root = CreateTestRoot();
        try
        {
            var database = Path.Combine(root, "save.sqlite");
            await new FhmSaveSqliteWriter().WriteAsync(CreateSave(), database, cancellationToken);
            await using (var context = new FhmSaveSqliteContext(FhmSaveSqliteContext.CreateOptions(database)))
            {
                await context.Database.ExecuteSqlRawAsync("UPDATE Players SET RecordOrdinal = 5", cancellationToken);
            }

            var exception = await Assert.ThrowsAsync<InvalidDataException>(
                () => new FhmSaveSqliteReader().ReadAsync(database, cancellationToken));
            Assert.Contains(nameof(Player), exception.Message);

            await using var invalidRatings = new FhmSaveSqliteContext(FhmSaveSqliteContext.CreateOptions(database));
            await invalidRatings.Database.ExecuteSqlRawAsync("UPDATE Players SET RecordOrdinal = 0, Goalie = 21", cancellationToken);
            exception = await Assert.ThrowsAsync<InvalidDataException>(
                () => new FhmSaveSqliteReader().ReadAsync(database, cancellationToken));
            Assert.Contains("outside 0..20", exception.Message);

            await invalidRatings.Database.ExecuteSqlRawAsync(
                "UPDATE Players SET Goalie = 0; DELETE FROM PlayerTacticalRoleTendencyValues WHERE Tendency = 8",
                cancellationToken);
            exception = await Assert.ThrowsAsync<InvalidDataException>(
                () => new FhmSaveSqliteReader().ReadAsync(database, cancellationToken));
            Assert.Contains("inserted, deleted, or reordered", exception.Message);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static FhmSave CreateSave()
    {
        var save = new FhmSave();
        var names = new FhmNamesFile();
        names.MasterNames.Add(new FhmNameEntry("Original", 1, 0, 0, 0, 0, 0));
        names.FirstNameLists[0].Add(1);
        names.SurnameLists[0].Add(1);
        Add(save, names);

        var roles = new FhmPlayerRolesFile { VersionTag = 6 };
        roles.Records.Add(new global::Shuttle.Fhm.Serde.Domain.Files.FhmPlayerRoleDefinition
        {
            RoleId = 8,
            Name = "Synthetic Tactical Role",
            AppliesToForwards = 1,
            PositionCategory = 4,
            ShortName = "STR",
            Description = "Synthetic tactical role",
        });
        Add(save, roles);

        var players = new FhmPlayersFile();
        var player = new FhmPlayerRecord
        {
            InternalIdentity = 0,
            ExportedPlayerId = 10,
            FirstNameId = 1,
            SurnameId = 1,
            CommonNameId = FhmNullConstants.Null,
            BirthDate = new(2000, 1, 1),
            TeamId = 0,
            FranchiseId = 1,
            PrimaryContractRole = new((ushort)FhmPlayingRole.Goalscorer),
            SupplementaryContractRole = new((ushort)FhmSquadStatus.StarPlayer),
            TacticalRole = new FhmPlayerRoleInstance
            {
                RoleId = 8,
                AttackingUseOverride = 1,
                AttackingTendencyValue = 3,
            },
        };
        var contract = new FhmPlayerContract();
        contract.Salaries[0].MajorLeagueSalary = 1_645_340;
        contract.Salaries[0].MinorLeagueSalary = 345_000;
        player.Contracts.Add(contract);
        players.Players.Add(player);
        Add(save, players);

        var teams = new FhmTeamsFile { VersionTag = 1 };
        var team = new FhmTeamRecord
        {
            RecordIndex = 0,
            TeamId = 1,
            AffiliateParentId = FhmNullConstants.Null,
            AffiliateParentId2 = FhmNullConstants.Null,
            City = "Original City",
        };
        team.Tail.GeneralManagerPersonnelId = 0;
        team.Tail.HeadCoachPersonnelId = 1;
        team.ActiveLines.Lists[(int)FhmLineGroup.EvenStrengthForwards].PlayerReferences.Add(0);
        teams.Teams.Add(team);
        Add(save, teams);

        using var personnelStream = new MemoryStream(
            FhmSaveTests.CreatePersonnelFileBytes(
                (0, 1, 0, FhmPersonnelJob.GeneralManager),
                (1, 1, 0, FhmPersonnelJob.HeadCoach)),
            writable: false);
        Add(save, FhmPersonnelFile.Read(personnelStream));

        var settings = new FhmGameSettingsFile();
        settings.Set(FhmGameSetting.Setting001, (byte)1);
        Add(save, settings);

        var lines = new FhmStoredLinesFile();
        var line = new FhmStoredLine { Name = "Default" };
        line.PlayerGroups[0].Add(0);
        line.UnitLocks[0].Add(0);
        lines.StoredLines.Add(line);
        Add(save, lines);

        var systems = new FhmTeamTacticsFile { VersionTag = 1 };
        systems.Records.Add(new FhmTacticSystem { GlobalId = 1, ZoneGroupRaw = 0, Name = "System", RatingA = 1, RatingB = 2 });
        Add(save, systems);

        var templates = new FhmTacticTemplatesFile { Version = 1 };
        templates.Templates.Add(new FhmTacticTemplate("template", 0, "Template", new byte[FhmTacticTemplatesFile.SettingsBlobLength]));
        Add(save, templates);

        var setPlay = new FhmSetPlayFile("set_play_5on4.dat") { Version = 1 };
        setPlay.Formations.Add(new FhmLengthPrefixedBlock([1]));
        setPlay.ExtraRecords.Add(new FhmLengthPrefixedBlock([2]));
        Add(save, setPlay);

        var shotType = new FhmLengthPrefixedCatalogueFile("shot_type_mod.dat", hasCount: false) { Version = 1 };
        shotType.Blocks.Add(new FhmLengthPrefixedBlock([3]));
        Add(save, shotType);

        var tacticalSettings = new FhmLengthPrefixedCatalogueFile("tactical_settings_mod.dat", hasCount: false) { Version = 1 };
        tacticalSettings.Blocks.Add(new FhmLengthPrefixedBlock([4]));
        Add(save, tacticalSettings);

        var zoneEvents = new FhmZoneEventModifiersFile { Version = 1, ZoneCount = 1 };
        zoneEvents.ModifierGrids.Add(new FhmLengthPrefixedBlock([5]));
        Add(save, zoneEvents);

        Add(save, new FhmTacticsFile { Version = 1, TacticCount = 1, RecordsOpaque = [6] });
        save.OpaqueFiles.Add(new FhmOpaqueFile("nested/opaque.bin", [10, 20, 30]));
        return save;
    }

    private static void Add(FhmSave save, IFhmSaveFile file) => save.Files.Add(file.RelativePath, file);

    private static string CreateTestRoot()
    {
        var root = Path.Combine(Directory.GetCurrentDirectory(), "sqlite-adapter-test-artifacts", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }

    private static void AssertFoldersEqual(string expectedRoot, string actualRoot)
    {
        var expected = Directory.EnumerateFiles(expectedRoot, "*", SearchOption.AllDirectories)
            .ToDictionary(path => Path.GetRelativePath(expectedRoot, path), StringComparer.OrdinalIgnoreCase);
        var actual = Directory.EnumerateFiles(actualRoot, "*", SearchOption.AllDirectories)
            .ToDictionary(path => Path.GetRelativePath(actualRoot, path), StringComparer.OrdinalIgnoreCase);
        Assert.Equal(expected.Keys.Order(), actual.Keys.Order());
        foreach (var (relativePath, expectedPath) in expected)
        {
            Assert.Equal(File.ReadAllBytes(expectedPath), File.ReadAllBytes(actual[relativePath]));
        }
    }
}
