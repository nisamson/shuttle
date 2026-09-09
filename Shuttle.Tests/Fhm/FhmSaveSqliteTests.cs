using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
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
    public async Task TeamReportService_ResolvesTeamPlayersLinesAndTactics()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var root = CreateTestRoot();
        try
        {
            var database = Path.Combine(root, "save.sqlite");
            await new FhmSaveSqliteWriter().WriteAsync(CreateSave(), database, cancellationToken);

            var report = await new TeamReportService().GetAsync(database, "Original City", cancellationToken);

            Assert.Equal("Original City", report.Team.Name);
            var player = Assert.Single(report.Players);
            Assert.Equal("Original Original", player.Name);
            Assert.Equal("Goalie", player.PrimaryPosition);
            Assert.Equal("Synthetic Tactical Role", Assert.Single(player.TacticalRoles).Name);
            var line = Assert.Single(report.CurrentLines);
            Assert.Equal("Even Strength Forwards", line.Group);
            Assert.Equal(["Original Original"], line.Players);
            Assert.Equal("Global settings", report.CurrentTactics.Selectors[0].Scope);
            Assert.Equal("Breakout", report.CurrentTactics.Selectors[0].Systems[0].Zone);

            var reports = await new TeamReportService().GetAllAsync(database, cancellationToken);
            Assert.Collection(reports, value => Assert.Equal("Original City", value.Team.Name));
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

                command.CommandText = "SELECT AffiliateParentRecordOrdinal FROM Teams WHERE RecordOrdinal = 0;";
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
    public async Task WriteAsync_PreservesNonCalendarPersonnelBirthDates()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var root = CreateTestRoot();
        try
        {
            var database = Path.Combine(root, "save.sqlite");
            var destination = Path.Combine(root, "destination");
            var save = CreateSave();
            var record = Assert.IsType<FhmPersonnelFile>(save.Files["personal.dat"]).Records[0];
            record.BirthDate = new(2000, 0, 0);

            await new FhmSaveSqliteWriter().WriteAsync(save, database, cancellationToken);

            await using (var context = new FhmSaveSqliteContext(FhmSaveSqliteContext.CreateOptions(database)))
            {
                var personnel = await context.Personnel.SingleAsync(
                    value => value.PersonnelId == record.PersonnelId,
                    cancellationToken);
                Assert.Equal(2000, personnel.BirthYear);
                Assert.Equal(0, personnel.BirthMonth);
                Assert.Equal(0, personnel.BirthDay);
            }

            await using (var context = new FhmSaveSqliteContext(FhmSaveSqliteContext.CreateOptions(database)))
            {
                await context.Database.ExecuteSqlRawAsync(
                    "UPDATE Personnel SET Salary = Salary + 1 WHERE PersonnelId = {0}",
                    new object[] { record.PersonnelId },
                    cancellationToken);
            }

            await new FhmSaveSqliteReader().ExportAsync(database, destination, cancellationToken);
            var reloaded = new FhmSaveReader().Read(destination);
            var personnelFile = Assert.IsType<FhmPersonnelFile>(reloaded.Files["personal.dat"]);
            Assert.Equal(
                new FhmDate(2000, 0, 0),
                personnelFile.Records.Single(value => value.PersonnelId == record.PersonnelId).BirthDate);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task WriteAsync_PreservesUnresolvedPersonnelTeamReference()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var root = CreateTestRoot();
        try
        {
            var database = Path.Combine(root, "save.sqlite");
            var destination = Path.Combine(root, "destination");
            var save = CreateSave();
            var record = Assert.IsType<FhmPersonnelFile>(save.Files["personal.dat"]).Records[0];
            record.TeamRecordIndex = 65_540;
            record.FirstNameNameId = 192_242;

            await new FhmSaveSqliteWriter().WriteAsync(save, database, cancellationToken);

            await using (var context = new FhmSaveSqliteContext(FhmSaveSqliteContext.CreateOptions(database)))
            {
                var personnel = await context.Personnel.SingleAsync(
                    value => value.PersonnelId == record.PersonnelId,
                    cancellationToken);
                Assert.Null(personnel.TeamRecordOrdinal);
                Assert.Equal(65_540, personnel.UnresolvedTeamRecordIndex);
                Assert.Equal(192_242, personnel.FirstNameNameId);
                Assert.Null(personnel.FirstNameLookupNameId);
                await context.Database.ExecuteSqlRawAsync(
                    "UPDATE Personnel SET Salary = Salary + 1 WHERE PersonnelId = {0}",
                    new object[] { record.PersonnelId },
                    cancellationToken);
            }

            await new FhmSaveSqliteReader().ExportAsync(database, destination, cancellationToken);
            var reloaded = new FhmSaveReader().Read(destination);
            var personnelFile = Assert.IsType<FhmPersonnelFile>(reloaded.Files["personal.dat"]);
            Assert.Equal(
                65_540,
                personnelFile.Records.Single(value => value.PersonnelId == record.PersonnelId).TeamRecordIndex);
            Assert.Equal(
                192_242,
                personnelFile.Records.Single(value => value.PersonnelId == record.PersonnelId).FirstNameNameId);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task WriteAsync_ExcludesAuxiliarySaveFiles()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var root = CreateTestRoot();
        try
        {
            var database = Path.Combine(root, "save.sqlite");
            var save = CreateSave();
            save.OpaqueFiles.Add(new FhmOpaqueFile("graphics/logo.png", [0]));
            save.OpaqueFiles.Add(new FhmOpaqueFile("import_export/bundle.zip", [1]));
            save.OpaqueFiles.Add(new FhmOpaqueFile("rs_one/restore.dat", [2]));
            await new FhmSaveSqliteWriter().WriteAsync(save, database, cancellationToken);

            await using var context = new FhmSaveSqliteContext(FhmSaveSqliteContext.CreateOptions(database));
            Assert.DoesNotContain(
                await context.Files.Select(file => file.RelativePath).ToListAsync(cancellationToken),
                path => path.StartsWith("graphics/", StringComparison.OrdinalIgnoreCase) ||
                    path.StartsWith("import_export/", StringComparison.OrdinalIgnoreCase) ||
                    path.StartsWith("rs", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task WriteFromDirectoryAsync_StreamsLargeOpaqueFileIntoOrderedChunks()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var root = CreateTestRoot();
        try
        {
            var source = Path.Combine(root, "source");
            var destination = Path.Combine(root, "destination");
            var database = Path.Combine(root, "save.sqlite");
            new FhmSaveWriter().Write(CreateSave(), source);
            var opaqueDirectory = Path.Combine(source, "assets");
            Directory.CreateDirectory(opaqueDirectory);
            var opaqueContent = new byte[(32 * 1024 * 1024) + 37];
            for (var index = 0; index < opaqueContent.Length; index++)
            {
                opaqueContent[index] = (byte)(index % byte.MaxValue);
            }

            var opaquePath = Path.Combine(opaqueDirectory, "large-unmodeled.bin");
            await File.WriteAllBytesAsync(opaquePath, opaqueContent, cancellationToken);

            await new FhmSaveSqliteWriter().WriteFromDirectoryAsync(source, database, cancellationToken);

            await using (var context = new FhmSaveSqliteContext(FhmSaveSqliteContext.CreateOptions(database)))
            {
                var file = await context.Files.SingleAsync(
                    value => value.RelativePath == "assets/large-unmodeled.bin",
                    cancellationToken);
                Assert.Equal(SaveFileKind.Opaque, file.Kind);
                Assert.Empty(file.Content);
                var chunks = await context.FileChunks
                    .Where(value => value.RelativePath == file.RelativePath)
                    .OrderBy(value => value.Ordinal)
                    .ToListAsync(cancellationToken);
                Assert.Equal([0, 1], chunks.Select(value => value.Ordinal));
                Assert.Equal(32 * 1024 * 1024, chunks[0].Content.Length);
                Assert.Equal(37, chunks[1].Content.Length);
            }

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
    public async Task WriteFromDirectoryAsync_PreservesMultiRecordLeaguesAsOpaqueBaseline()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var root = CreateTestRoot();
        try
        {
            var source = Path.Combine(root, "source");
            var destination = Path.Combine(root, "destination");
            var database = Path.Combine(root, "save.sqlite");
            new FhmSaveWriter().Write(CreateSave(), source);

            var leaguesContent = new byte[12];
            System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(leaguesContent, 35);
            System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(leaguesContent.AsSpan(sizeof(int)), 2);
            new byte[] { 0xDE, 0xAD, 0xBE, 0xEF }.CopyTo(leaguesContent, 8);
            await File.WriteAllBytesAsync(Path.Combine(source, "leagues.dat"), leaguesContent, cancellationToken);

            await new FhmSaveSqliteWriter().WriteFromDirectoryAsync(source, database, cancellationToken);
            await new FhmSaveSqliteReader().ExportAsync(database, destination, cancellationToken);

            var exportedLeagues = await File.ReadAllBytesAsync(
                Path.Combine(destination, "leagues.dat"),
                cancellationToken);
            Assert.Equal(leaguesContent, exportedLeagues);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task ExportAsync_StreamsBaselineAndAppliesDocumentedEntities()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var root = CreateTestRoot();
        try
        {
            var database = Path.Combine(root, "save.sqlite");
            var materializedDestination = Path.Combine(root, "materialized");
            var streamedDestination = Path.Combine(root, "streamed");
            await new FhmSaveSqliteWriter().WriteAsync(CreateSave(), database, cancellationToken);
            await using (var context = new FhmSaveSqliteContext(FhmSaveSqliteContext.CreateOptions(database)))
            {
                await context.Database.ExecuteSqlRawAsync(
                    "UPDATE Names SET Text = 'Streamed Name'; UPDATE Players SET ExternalId = 99;",
                    cancellationToken);
                context.Files.AddRange(
                    new SaveFile
                    {
                        RelativePath = "opaque-inline.bin",
                        Kind = SaveFileKind.Opaque,
                        Content = [1, 2, 3],
                    },
                    new SaveFile
                    {
                        RelativePath = "nested/opaque-chunked.bin",
                        Kind = SaveFileKind.Opaque,
                        Content = [],
                    },
                    new SaveFile
                    {
                        RelativePath = "graphics/logo.png",
                        Kind = SaveFileKind.Opaque,
                        Content = [4],
                    },
                    new SaveFile
                    {
                        RelativePath = "import_export/bundle.zip",
                        Kind = SaveFileKind.Opaque,
                        Content = [5],
                    },
                    new SaveFile
                    {
                        RelativePath = "rs_one/restore.dat",
                        Kind = SaveFileKind.Opaque,
                        Content = [6],
                    });
                context.FileChunks.AddRange(
                    new SaveFileChunk
                    {
                        RelativePath = "nested/opaque-chunked.bin",
                        Ordinal = 0,
                        Content = [7, 8],
                    },
                    new SaveFileChunk
                    {
                        RelativePath = "nested/opaque-chunked.bin",
                        Ordinal = 1,
                        Content = [9, 10],
                    });
                await context.SaveChangesAsync(cancellationToken);
                await context.Database.ExecuteSqlRawAsync(
                    "UPDATE SaveManifest SET SourceFileCount = SourceFileCount + 5;",
                    cancellationToken);
            }

            var reader = new FhmSaveSqliteReader();
            new FhmSaveWriter().Write(
                await reader.ReadAsync(database, cancellationToken),
                materializedDestination);

            await reader.ExportAsync(database, streamedDestination, cancellationToken);

            AssertFoldersEqual(materializedDestination, streamedDestination);
            var reloaded = new FhmSaveReader().Read(streamedDestination);
            Assert.Equal("Streamed Name", Assert.IsType<FhmNamesFile>(reloaded.Files["names.dat"]).MasterNames[0].Text);
            Assert.Equal(99, Assert.Single(Assert.IsType<FhmPlayersFile>(reloaded.Files["players.dat"]).Players).ExportedPlayerId);
            Assert.Equal(
                [1, 2, 3],
                Assert.Single(reloaded.OpaqueFiles, file => file.RelativePath == "opaque-inline.bin").Content);
            Assert.Equal(
                [7, 8, 9, 10],
                Assert.Single(reloaded.OpaqueFiles, file => file.RelativePath == "nested/opaque-chunked.bin").Content);
            Assert.False(File.Exists(Path.Combine(streamedDestination, "graphics", "logo.png")));
            Assert.False(File.Exists(Path.Combine(streamedDestination, "import_export", "bundle.zip")));
            Assert.False(File.Exists(Path.Combine(streamedDestination, "rs_one", "restore.dat")));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task WriteFromDirectoryAsync_StreamsPlayersInBoundedBatches()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var root = CreateTestRoot();
        try
        {
            var source = Path.Combine(root, "source");
            var destination = Path.Combine(root, "destination");
            var database = Path.Combine(root, "save.sqlite");
            var save = CreateSave();
            var players = Assert.IsType<FhmPlayersFile>(save.Files["players.dat"]);
            for (var playerId = 1; playerId <= 250; playerId++)
            {
                players.Players.Add(new FhmPlayerRecord
                {
                    InternalIdentity = playerId,
                    ExportedPlayerId = playerId + 10,
                    FirstNameId = 1,
                    SurnameId = 1,
                    CommonNameId = FhmNullConstants.Null,
                    BirthDate = new(2000, 1, 1),
                    TeamId = 0,
                    FranchiseId = 1,
                    PrimaryContractRole = new((ushort)FhmPlayingRole.Goalscorer),
                    SupplementaryContractRole = new((ushort)FhmSquadStatus.StarPlayer),
                });
            }

            new FhmSaveWriter().Write(save, source);
            await new FhmSaveSqliteWriter().WriteFromDirectoryAsync(source, database, cancellationToken);

            await using (var context = new FhmSaveSqliteContext(FhmSaveSqliteContext.CreateOptions(database)))
            {
                var storedPlayers = await context.Players
                    .OrderBy(player => player.RecordOrdinal)
                    .ToListAsync(cancellationToken);
                Assert.Equal(251, storedPlayers.Count);
                Assert.Equal(Enumerable.Range(0, 251), storedPlayers.Select(player => player.RecordOrdinal));
                Assert.All(storedPlayers, player => Assert.Equal(0, player.TeamRecordOrdinal));
            }

            new FhmSaveWriter().Write(
                await new FhmSaveSqliteReader().ReadAsync(database, cancellationToken),
                destination);
            AssertFoldersEqual(source, destination);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task WriteFromDirectoryAsync_StreamsPersonnelInBoundedBatchesAndPreservesParity()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var root = CreateTestRoot();
        try
        {
            var source = Path.Combine(root, "source");
            var destination = Path.Combine(root, "destination");
            var database = Path.Combine(root, "save.sqlite");
            new FhmSaveWriter().Write(CreateSave(), source);
            var personnel = Enumerable.Range(0, 251)
                .Select(personnelId => (personnelId, 1, (int?)0, FhmPersonnelJob.Scout))
                .ToArray();
            await File.WriteAllBytesAsync(
                Path.Combine(source, "personal.dat"),
                FhmSaveTests.CreatePersonnelFileBytes(personnel),
                cancellationToken);

            await new FhmSaveSqliteWriter().WriteFromDirectoryAsync(source, database, cancellationToken);

            await using (var context = new FhmSaveSqliteContext(FhmSaveSqliteContext.CreateOptions(database)))
            {
                Assert.Equal(251, await context.Personnel.CountAsync(cancellationToken));
                var firstPersonnel = await context.Personnel.SingleAsync(
                    value => value.PersonnelId == 0,
                    cancellationToken);
                Assert.Equal(1, firstPersonnel.FirstNameLookupNameId);
                Assert.Equal(1, firstPersonnel.SurnameLookupNameId);
                Assert.Equal(0, firstPersonnel.TeamRecordOrdinal);
                Assert.Null(firstPersonnel.UnresolvedTeamRecordIndex);
            }

            await new FhmSaveSqliteReader().ExportAsync(database, destination, cancellationToken);
            AssertFoldersEqual(source, destination);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task WriteAsync_StoresPlayerTacticalRolesAsInGameRoleEnums()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var root = CreateTestRoot();
        try
        {
            var database = Path.Combine(root, "save.sqlite");
            await new FhmSaveSqliteWriter().WriteAsync(CreateSave(), database, cancellationToken);

            await using var context = new FhmSaveSqliteContext(FhmSaveSqliteContext.CreateOptions(database));
            Assert.Equal(InGameRole.BackcheckingForward, await context.TacticalRoles
                .Select(value => value.RoleId)
                .SingleAsync(cancellationToken));
            Assert.Equal(InGameRole.BackcheckingForward, await context.PlayerTacticalRoleAssignments
                .Select(value => value.RoleId)
                .SingleAsync(cancellationToken));
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
    public async Task ImportExport_PreservesTeamStaffTailWithMultipleMatchingPersonnel()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var root = CreateTestRoot();
        try
        {
            var source = Path.Combine(root, "source");
            var materializedDestination = Path.Combine(root, "materialized");
            var streamedDestination = Path.Combine(root, "streamed");
            var database = Path.Combine(root, "save.sqlite");
            var save = CreateSave();
            var personnel = Assert.IsType<FhmPersonnelFile>(save.Files["personal.dat"]);
            personnel.Records.Single(value => value.PersonnelId == 1).Job = FhmPersonnelJob.GeneralManager;
            new FhmSaveWriter().Write(save, source);

            await new FhmSaveSqliteWriter().WriteFromDirectoryAsync(source, database, cancellationToken);

            var materialized = await new FhmSaveSqliteReader().ReadAsync(database, cancellationToken);
            new FhmSaveWriter().Write(materialized, materializedDestination);
            await new FhmSaveSqliteReader().ExportAsync(database, streamedDestination, cancellationToken);

            AssertFoldersEqual(source, materializedDestination);
            AssertFoldersEqual(source, streamedDestination);
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
                        BirthYear = 1981,
                        BirthMonth = 2,
                        BirthDay = 3,
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
                    "UPDATE Personnel SET TeamRecordOrdinal = NULL WHERE PersonnelId = 1",
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
                    "UPDATE TeamActiveLineSlots SET PlayerInternalId = NULL WHERE TeamRecordOrdinal = 0 AND \"Group\" = 0 AND SlotOrdinal = 0",
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
            Assert.Equal(1, reloadedTeam.Tail.HeadCoachPersonnelId);
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
            Assert.Equal(0, player.Team?.RecordOrdinal);
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
            Assert.Equal("Original", playerTeam.GeneralManager?.Surname?.Text);

            var storedLine = await context.StoredLines
                .Include(value => value.Slots)
                .SingleAsync(cancellationToken);
            Assert.Equal(0, storedLine.Team?.RecordOrdinal);
            Assert.Equal(player.InternalId, Assert.Single(storedLine.Slots).Player?.InternalId);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task Import_OverwriteReplacesExistingDatabase()
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

            await new FhmSaveSqliteWriter().WriteAsync(
                CreateSave(),
                database,
                new FhmSaveSqliteWriteOptions { Overwrite = true },
                cancellationToken);

            await using (var verification = new SqliteConnection($"Data Source={database};Pooling=False"))
            {
                await verification.OpenAsync(cancellationToken);
                await using var verificationCommand = verification.CreateCommand();
                verificationCommand.CommandText = "SELECT COUNT(*) FROM SaveManifest;";
                Assert.Equal(1L, Convert.ToInt64(await verificationCommand.ExecuteScalarAsync(cancellationToken)));
            }
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task Schema_PreservesUnknownPersonnelTendencyAndRejectsInvalidRating()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var root = CreateTestRoot();
        try
        {
            var database = Path.Combine(root, "save.sqlite");
            await new FhmSaveSqliteWriter().WriteAsync(CreateSave(), database, cancellationToken);
            await using var context = new FhmSaveSqliteContext(FhmSaveSqliteContext.CreateOptions(database));

            await context.Database.ExecuteSqlRawAsync(
                "UPDATE Personnel SET LoyaltyTendency = 5 WHERE PersonnelId = 0",
                cancellationToken);
            Assert.Equal(
                (FhmLoyaltyTendency)5,
                await context.Personnel
                    .Where(value => value.PersonnelId == 0)
                    .Select(value => value.LoyaltyTendency)
                    .SingleAsync(cancellationToken));

            var exception = await Assert.ThrowsAsync<SqliteException>(
                () => context.Database.ExecuteSqlRawAsync(
                    "UPDATE Personnel SET Negotiating = 21 WHERE PersonnelId = 0",
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

    [Fact]
    public async Task ImportExport_AllowsDuplicateDecodedTeamIdsAndUsesRecordOrdinalsForRelationships()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var root = CreateTestRoot();
        try
        {
            var database = Path.Combine(root, "save.sqlite");
            var destination = Path.Combine(root, "destination");
            await new FhmSaveSqliteWriter().WriteAsync(CreateDuplicateTeamIdsSave(), database, cancellationToken);

            await using (var context = new FhmSaveSqliteContext(FhmSaveSqliteContext.CreateOptions(database)))
            {
                var teamRows = await context.Teams
                    .OrderBy(team => team.RecordOrdinal)
                    .ToListAsync(cancellationToken);
                Assert.Equal([1, 1], teamRows.Select(team => team.TeamId));
                Assert.Equal([0, 1], teamRows.Select(team => team.RecordOrdinal));
                Assert.Equal(1, teamRows[0].AffiliateParentRecordOrdinal);
                Assert.Equal([0, 1], await context.Players
                    .OrderBy(player => player.InternalId)
                    .Select(player => player.TeamRecordOrdinal)
                    .ToListAsync(cancellationToken));
                Assert.Equal([0, 1], await context.TeamTactics
                    .OrderBy(tactic => tactic.TeamRecordOrdinal)
                    .Select(tactic => tactic.TeamRecordOrdinal)
                    .ToListAsync(cancellationToken));
                Assert.Equal([0, 1], await context.TeamActiveLineSlots
                    .OrderBy(slot => slot.TeamRecordOrdinal)
                    .Select(slot => slot.TeamRecordOrdinal)
                    .ToListAsync(cancellationToken));
            }

            var exported = await new FhmSaveSqliteReader().ReadAsync(database, cancellationToken);
            new FhmSaveWriter().Write(exported, destination);
            var reloaded = new FhmSaveReader().Read(destination);
            var players = Assert.IsType<FhmPlayersFile>(reloaded.Files["players.dat"]).Players;
            var teams = Assert.IsType<FhmTeamsFile>(reloaded.Files["teams.dat"]).Teams;

            Assert.Equal([1, 1], teams.Select(team => team.TeamId));
            Assert.Equal([0, 1], teams.Select(team => team.RecordIndex));
            Assert.Equal(1, teams[0].AffiliateParentId);
            Assert.Equal([0, 1], players.OrderBy(player => player.InternalIdentity).Select(player => player.TeamId));
            Assert.Equal([0, 1], teams
                .Select((team, ordinal) => (team, ordinal))
                .OrderBy(value => value.ordinal)
                .Select(value => value.team.ActiveLines.Lists[(int)FhmLineGroup.EvenStrengthForwards].PlayerReferences.Single()));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task Migration_ConvertsExistingTeamRelationshipsToRecordOrdinals()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var root = CreateTestRoot();
        try
        {
            var database = Path.Combine(root, "save.sqlite");
            await using var context = new FhmSaveSqliteContext(FhmSaveSqliteContext.CreateOptions(database));
            var migrator = context.Database.GetService<IMigrator>();
            await migrator.MigrateAsync("20260824000000_AddSaveFileChunks", cancellationToken);
            await context.Database.ExecuteSqlRawAsync(
                """
                INSERT INTO "Teams" (
                    "TeamId", "RecordOrdinal", "RecordIndex", "Flag1", "NicknamePlacement",
                    "LeagueId", "ConferenceId", "DivisionId", "LocationId", "MarketSize",
                    "FanLoyalty", "Finance1", "Finance2", "Finance3", "Finance4")
                VALUES (10, 3, 77, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
                INSERT INTO "Players" (
                    "InternalId", "RecordOrdinal", "ExternalId", "BirthDate", "TeamId",
                    "Goalie", "LeftDefense", "RightDefense", "LeftWing", "Center", "RightWing",
                    "PrimaryContractRole", "SupplementaryContractRole", "SerializedRecord")
                VALUES (9, 0, 0, '2000-01-01', 10, 0, 0, 0, 0, 0, 0, 0, 0, X'00');
                INSERT INTO "TeamTactics" ("TeamId", "SerializedSettings") VALUES (10, X'00');
                INSERT INTO "StoredLines" ("LineOrdinal", "Name", "TeamId") VALUES (0, 'Legacy', 10);
                INSERT INTO "TeamActiveLineSlots" ("TeamId", "Group", "SlotOrdinal", "PlayerInternalId")
                VALUES (10, 0, 0, NULL);
                """,
                cancellationToken);

            await migrator.MigrateAsync(targetMigration: null, cancellationToken: cancellationToken);

            Assert.Equal(3, (await context.Players.SingleAsync(cancellationToken)).TeamRecordOrdinal);
            Assert.Equal(3, (await context.TeamTactics.SingleAsync(cancellationToken)).TeamRecordOrdinal);
            Assert.Equal(3, (await context.StoredLines.SingleAsync(cancellationToken)).TeamRecordOrdinal);
            Assert.Equal(3, (await context.TeamActiveLineSlots.SingleAsync(cancellationToken)).TeamRecordOrdinal);
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

    private static FhmSave CreateDuplicateTeamIdsSave()
    {
        var save = CreateSave();
        var players = Assert.IsType<FhmPlayersFile>(save.Files["players.dat"]);
        players.Players.Add(new FhmPlayerRecord
        {
            InternalIdentity = 1,
            ExportedPlayerId = 11,
            FirstNameId = 1,
            SurnameId = 1,
            CommonNameId = FhmNullConstants.Null,
            BirthDate = new(2001, 1, 1),
            TeamId = 1,
            FranchiseId = 1,
            PrimaryContractRole = new((ushort)FhmPlayingRole.Goalscorer),
            SupplementaryContractRole = new((ushort)FhmSquadStatus.StarPlayer),
        });

        var teams = Assert.IsType<FhmTeamsFile>(save.Files["teams.dat"]);
        teams.Teams[0].AffiliateParentId = 1;
        var duplicateTeamId = new FhmTeamRecord
        {
            RecordIndex = 1,
            TeamId = 1,
            AffiliateParentId = FhmNullConstants.Null,
            AffiliateParentId2 = FhmNullConstants.Null,
            City = "Duplicate City",
        };
        duplicateTeamId.ActiveLines.Lists[(int)FhmLineGroup.EvenStrengthForwards].PlayerReferences.Add(1);
        teams.Teams.Add(duplicateTeamId);
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
