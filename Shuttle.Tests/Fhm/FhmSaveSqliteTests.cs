using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Shuttle.Fhm.Serde.Domain.Files;
using Shuttle.Fhm.Serde.Domain.Model;
using Shuttle.Fhm.Serde.Domain.SaveFolder;
using Shuttle.Fhm.Serde.Sqlite;

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
    public async Task ImportExport_AppliesEditableProjectionsAndPreservesOpaqueContent()
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
                    "UPDATE Names SET Text = 'Edited' WHERE CollectionKind = 'Master'",
                    cancellationToken);
                await context.Database.ExecuteSqlRawAsync(
                    "UPDATE Players SET ExternalId = 99",
                    cancellationToken);
                await context.Database.ExecuteSqlRawAsync(
                    "UPDATE PlayerAttributes SET Passing = 19",
                    cancellationToken);
                await context.Database.ExecuteSqlRawAsync(
                    "UPDATE Teams SET City = 'Edited City'",
                    cancellationToken);
                await context.Database.ExecuteSqlRawAsync(
                    "UPDATE GameSettings SET IntegerValue = 7 WHERE SettingOrdinal = 0",
                    cancellationToken);
                await context.Database.ExecuteSqlRawAsync(
                    "UPDATE StoredLineSlots SET PlayerInternalId = -1",
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
            Assert.Equal((byte)19, player.RatingAttributes.Passing);
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
                await context.Database.ExecuteSqlRawAsync("UPDATE Players SET InternalId = 5", cancellationToken);
            }

            var exception = await Assert.ThrowsAsync<InvalidDataException>(
                () => new FhmSaveSqliteReader().ReadAsync(database, cancellationToken));
            Assert.Contains("immutable identity", exception.Message);

            await using var invalidRatings = new FhmSaveSqliteContext(FhmSaveSqliteContext.CreateOptions(database));
            await invalidRatings.Database.ExecuteSqlRawAsync("UPDATE Players SET InternalId = 0, Goalie = 21", cancellationToken);
            exception = await Assert.ThrowsAsync<InvalidDataException>(
                () => new FhmSaveSqliteReader().ReadAsync(database, cancellationToken));
            Assert.Contains("outside 0..20", exception.Message);
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

        var players = new FhmPlayersFile();
        players.Players.Add(new FhmPlayerRecord
        {
            InternalIdentity = 0,
            ExportedPlayerId = 10,
            FirstNameId = 1,
            SurnameId = 1,
            CommonNameId = FhmNullConstants.Null,
            BirthDate = new(2000, 1, 1),
            TeamId = 1,
            FranchiseId = 1,
        });
        Add(save, players);

        var teams = new FhmTeamsFile { VersionTag = 1 };
        teams.Teams.Add(new FhmTeamRecord { RecordIndex = 0, TeamId = 1, City = "Original City" });
        Add(save, teams);

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
