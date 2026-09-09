using EFCore.BulkExtensions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Shuttle.Fhm.Serde.Domain.Files;
using Shuttle.Fhm.Serde.Domain.Model;
using Shuttle.Fhm.Serde.Domain.SaveFolder;

namespace Shuttle.Fhm.Serde.Sqlite;

/// <summary>Options governing creation of an FHM save SQLite database.</summary>
public sealed class FhmSaveSqliteWriteOptions
{
    /// <summary>Gets or sets whether an existing database file may be replaced by a new import.</summary>
    public bool Overwrite { get; init; }

    /// <summary>Gets or sets an optional recipient of completed import-phase timings.</summary>
    public IProgress<FhmSaveSqliteWriteProgress>? Progress { get; init; }
    /// <summary>Gets or sets an optional recipient of source capture progress updates.</summary>
    public IProgress<FhmSaveReadProgress>? SourceProgress { get; init; }
}

/// <summary>Reports elapsed time for a completed SQLite save import phase.</summary>
public sealed record FhmSaveSqliteWriteProgress(string Phase, TimeSpan Elapsed);

/// <summary>Imports an FHM save into the lossless SQLite format.</summary>
public sealed class FhmSaveSqliteWriter
{
    private const int MaximumInlineFileContentLength = 32 * 1024 * 1024;
    private const int SqliteBulkInsertBatchSize = 250;

    /// <summary>Current compatible SQLite schema version.</summary>
    public const int SchemaVersion = 1;

    /// <summary>Writes a new database, refusing to replace an existing file.</summary>
    public Task WriteAsync(FhmSave save, string databasePath, CancellationToken cancellationToken = default) =>
        WriteAsync(save, databasePath, new FhmSaveSqliteWriteOptions(), cancellationToken);

    /// <summary>Writes a new database using the supplied import options.</summary>
    public async Task WriteAsync(
        FhmSave save,
        string databasePath,
        FhmSaveSqliteWriteOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(save);
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);
        ArgumentNullException.ThrowIfNull(options);

        var sourceFiles = GetSourceFiles(save);
        await WritePreparedAsync(
            databasePath,
            options,
            sourceFiles.Count,
            context =>
            {
                AddMaterializedSourceFiles(context, sourceFiles);
                return Task.CompletedTask;
            },
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Streams one FHM save folder into a new database without retaining unsupported source files as
    /// <see cref="FhmOpaqueFile"/> byte arrays.
    /// </summary>
    public Task WriteFromDirectoryAsync(
        string sourceDirectory,
        string databasePath,
        CancellationToken cancellationToken = default) =>
        WriteFromDirectoryAsync(
            sourceDirectory,
            databasePath,
            new FhmSaveSqliteWriteOptions(),
            cancellationToken);

    /// <summary>
    /// Streams one FHM save folder into a new database using the supplied import options.
    /// </summary>
    public async Task WriteFromDirectoryAsync(
        string sourceDirectory,
        string databasePath,
        FhmSaveSqliteWriteOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);
        ArgumentNullException.ThrowIfNull(options);
        if (!Directory.Exists(sourceDirectory))
        {
            throw new DirectoryNotFoundException($"FHM save folder '{sourceDirectory}' does not exist.");
        }

        var root = Path.GetFullPath(sourceDirectory);
        var sourcePaths = GetSourcePaths(root);
        await WritePreparedAsync(
            databasePath,
            options,
            sourcePaths.Count,
            context => AddDirectorySourceFilesAsync(
                context,
                sourcePaths,
                options.SourceProgress,
                cancellationToken),
            cancellationToken).ConfigureAwait(false);
    }

    private static async Task WritePreparedAsync(
        string databasePath,
        FhmSaveSqliteWriteOptions options,
        int sourceFileCount,
        Func<FhmSaveSqliteContext, Task> addSourceFiles,
        CancellationToken cancellationToken)
    {
        var elapsed = System.Diagnostics.Stopwatch.StartNew();
        var fullPath = Path.GetFullPath(databasePath);
        if (File.Exists(fullPath))
        {
            if (!options.Overwrite)
            {
                throw new IOException(
                    $"SQLite save database '{fullPath}' already exists. Set Overwrite to replace it with a new import.");
            }

            File.Delete(fullPath);
            File.Delete($"{fullPath}-shm");
            File.Delete($"{fullPath}-wal");
        }

        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        await using var context = new FhmSaveSqliteContext(FhmSaveSqliteContext.CreateOptions(fullPath));

        await context.Database.MigrateAsync(cancellationToken);
        ReportElapsed(options, "Schema migration", elapsed);
        await context.Database.OpenConnectionAsync(cancellationToken);
        try
        {
            await FhmSaveSqliteContext.ConfigureFreshImportConnectionAsync(context, cancellationToken);
            await context.Database.ExecuteSqlRawAsync("PRAGMA foreign_keys = OFF;", cancellationToken);
            await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                if (await context.Manifests.AnyAsync(cancellationToken))
                {
                    throw new InvalidDataException("A new SQLite import database unexpectedly contains an adapter manifest.");
                }

                ReportElapsed(options, "Source preparation", elapsed);
                context.Manifests.Add(new SaveManifest
                {
                    SchemaVersion = SchemaVersion,
                    SourceFormatVersion = "FHM 10 save folder",
                    SourceFileCount = sourceFileCount,
                });

                await addSourceFiles(context).ConfigureAwait(false);
                ReportElapsed(options, "Source capture and entity projection", elapsed);
                await BulkInsertAsync(context, cancellationToken);
                ReportElapsed(options, "SQLite persistence", elapsed);
                await EnsureForeignKeysValidAsync(context, transaction, cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                ReportElapsed(options, "Transaction commit", elapsed);
            }
            catch
            {
                await transaction.RollbackAsync(CancellationToken.None);
                throw;
            }
        }
        finally
        {
            await context.Database.CloseConnectionAsync();
        }
    }

    private static async Task EnsureForeignKeysValidAsync(
        FhmSaveSqliteContext context,
        IDbContextTransaction transaction,
        CancellationToken cancellationToken)
    {
        await using var command = context.Database.GetDbConnection().CreateCommand();
        command.Transaction = transaction.GetDbTransaction();
        command.CommandText = "PRAGMA foreign_key_check;";
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            throw new InvalidDataException(
                $"SQLite foreign-key validation failed for {reader.GetString(0)} row {reader.GetInt64(1)}, " +
                $"parent table {reader.GetString(2)}, constraint {reader.GetInt32(3)}.");
        }
    }

    private static void ReportElapsed(
        FhmSaveSqliteWriteOptions options,
        string phase,
        System.Diagnostics.Stopwatch elapsed)
    {
        options.Progress?.Report(new FhmSaveSqliteWriteProgress(phase, elapsed.Elapsed));
        elapsed.Restart();
    }

    private static async Task BulkInsertAsync(FhmSaveSqliteContext context, CancellationToken cancellationToken)
    {
        await BulkInsertAsync<SaveManifest>(context, cancellationToken);
        await BulkInsertAsync<SaveFile>(context, cancellationToken);
        await BulkInsertAsync<SaveFileChunk>(context, cancellationToken);

        await BulkInsertAsync<Name>(context, cancellationToken);
        await BulkInsertAsync<NameListEntry>(context, cancellationToken);
        await BulkInsertAsync<NameScalar>(context, cancellationToken);

        await BulkInsertAsync<PlayerRoleCatalogue>(context, cancellationToken);
        await BulkInsertAsync<PlayerRoleDefinition>(context, cancellationToken);
        await BulkInsertAsync<PlayerRoleWeight>(context, cancellationToken);
        await BulkInsertAsync<PlayerRoleIndexEntry>(context, cancellationToken);

        await BulkInsertTeamsAsync(context, cancellationToken);
        await BulkInsertAsync<TeamTactic>(context, cancellationToken);
        await BulkInsertAsync<Player>(context, cancellationToken);
        await BulkInsertAsync<PlayerAttributes>(context, cancellationToken);
        await BulkInsertAsync<PlayerContract>(context, cancellationToken);
        await BulkInsertAsync<PlayerContractYear>(context, cancellationToken);
        await BulkInsertAsync<PlayerRoleAssignment>(context, cancellationToken);
        await BulkInsertAsync<PlayerRoleTendencyValue>(context, cancellationToken);
        await BulkInsertAsync<Personnel>(context, cancellationToken);

        await BulkInsertAsync<TacticFile>(context, cancellationToken);
        await BulkInsertAsync<TacticSystem>(context, cancellationToken);
        await BulkInsertAsync<TacticTemplate>(context, cancellationToken);
        await BulkInsertAsync<SetPlay>(context, cancellationToken);
        await BulkInsertAsync<ModifierCatalogue>(context, cancellationToken);
        await BulkInsertAsync<Tactics>(context, cancellationToken);
        await BulkInsertAsync<StoredLine>(context, cancellationToken);
        await BulkInsertAsync<StoredLineSlot>(context, cancellationToken);
        await BulkInsertAsync<TeamActiveLineSlot>(context, cancellationToken);

        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.Entity is not GameSetting)
            {
                entry.State = EntityState.Unchanged;
            }
        }

        await context.SaveChangesAsync(cancellationToken);
        context.ChangeTracker.Clear();
    }

    private static async Task BulkInsertAsync<TEntity>(FhmSaveSqliteContext context, CancellationToken cancellationToken)
        where TEntity : class
    {
        var entities = context.ChangeTracker.Entries<TEntity>()
            .Where(entry => entry.State == EntityState.Added)
            .Select(entry => entry.Entity)
            .ToList();
        if (entities.Count == 0)
        {
            return;
        }

        try
        {
            await context.BulkInsertAsync(
                entities,
                new BulkConfig { BatchSize = SqliteBulkInsertBatchSize },
                cancellationToken: cancellationToken);
        }
        catch (SqliteException exception) when (exception.SqliteErrorCode == 19)
        {
            throw new InvalidDataException(
                $"SQLite foreign-key constraint failed while inserting {entities.Count} {typeof(TEntity).Name} entities.",
                exception);
        }
    }

    private static async Task BulkInsertTeamsAsync(FhmSaveSqliteContext context, CancellationToken cancellationToken)
    {
        var teams = context.ChangeTracker.Entries<Team>()
            .Where(entry => entry.State == EntityState.Added)
            .Select(entry => entry.Entity)
            .ToList();
        if (teams.Count == 0)
        {
            return;
        }

        var affiliateReferences = teams
            .Select(team => new TeamAffiliateReferences(
                team,
                team.AffiliateParentRecordOrdinal,
                team.SecondaryAffiliateParentRecordOrdinal))
            .ToList();
        foreach (var reference in affiliateReferences)
        {
            reference.Team.AffiliateParentRecordOrdinal = null;
            reference.Team.SecondaryAffiliateParentRecordOrdinal = null;
        }

        await BulkInsertAsync<Team>(context, cancellationToken);

        foreach (var reference in affiliateReferences)
        {
            reference.Team.AffiliateParentRecordOrdinal = reference.AffiliateParentRecordOrdinal;
            reference.Team.SecondaryAffiliateParentRecordOrdinal = reference.SecondaryAffiliateParentRecordOrdinal;
        }

        try
        {
            await context.BulkUpdateAsync(
                teams,
                new BulkConfig { BatchSize = SqliteBulkInsertBatchSize },
                cancellationToken: cancellationToken);
        }
        catch (SqliteException exception) when (exception.SqliteErrorCode == 19)
        {
            throw new InvalidDataException(
                $"SQLite foreign-key constraint failed while restoring affiliate references for {teams.Count} Team entities.",
                exception);
        }
    }

    private static async Task EnsureExistingDatabaseIsAdapterFormatAsync(
        FhmSaveSqliteContext context,
        string databasePath,
        CancellationToken cancellationToken)
    {
        await context.Database.OpenConnectionAsync(cancellationToken);
        try
        {
            await using var command = context.Database.GetDbConnection().CreateCommand();
            command.CommandText = """
                SELECT COUNT(*)
                FROM sqlite_master
                WHERE type = 'table' AND name = 'SaveManifest';
                """;
            var result = await command.ExecuteScalarAsync(cancellationToken);
            if (Convert.ToInt64(result, System.Globalization.CultureInfo.InvariantCulture) != 1)
            {
                throw new InvalidDataException(
                    $"SQLite save database '{databasePath}' is not an adapter database and cannot be replaced safely.");
            }
        }
        finally
        {
            await context.Database.CloseConnectionAsync();
        }
    }

    private static async Task ClearExistingSaveAsync(FhmSaveSqliteContext context, CancellationToken cancellationToken)
    {
        await context.StoredLineSlots.ExecuteDeleteAsync(cancellationToken);
        await context.StoredLines.ExecuteDeleteAsync(cancellationToken);
        await context.TeamActiveLineSlots.ExecuteDeleteAsync(cancellationToken);
        await context.PlayerTacticalRoleTendencyValues.ExecuteDeleteAsync(cancellationToken);
        await context.PlayerTacticalRoleAssignments.ExecuteDeleteAsync(cancellationToken);
        await context.PlayerContractYears.ExecuteDeleteAsync(cancellationToken);
        await context.PlayerContracts.ExecuteDeleteAsync(cancellationToken);
        await context.PlayerAttributes.ExecuteDeleteAsync(cancellationToken);
        await context.Players.ExecuteDeleteAsync(cancellationToken);
        await context.TacticalRoleIndexEntries.ExecuteDeleteAsync(cancellationToken);
        await context.TacticalRoleWeights.ExecuteDeleteAsync(cancellationToken);
        await context.TacticalRoles.ExecuteDeleteAsync(cancellationToken);
        await context.TacticalRoleCatalogues.ExecuteDeleteAsync(cancellationToken);
        await context.Personnel.ExecuteDeleteAsync(cancellationToken);
        await context.TeamTactics.ExecuteDeleteAsync(cancellationToken);
        await context.Teams.ExecuteDeleteAsync(cancellationToken);
        await context.NameListEntries.ExecuteDeleteAsync(cancellationToken);
        await context.NameScalars.ExecuteDeleteAsync(cancellationToken);
        await context.Names.ExecuteDeleteAsync(cancellationToken);
        await context.GameSettings.ExecuteDeleteAsync(cancellationToken);
        await context.TacticSystems.ExecuteDeleteAsync(cancellationToken);
        await context.TacticTemplates.ExecuteDeleteAsync(cancellationToken);
        await context.SetPlays.ExecuteDeleteAsync(cancellationToken);
        await context.ModifierCatalogues.ExecuteDeleteAsync(cancellationToken);
        await context.Tactics.ExecuteDeleteAsync(cancellationToken);
        await context.TacticFiles.ExecuteDeleteAsync(cancellationToken);
        await context.FileChunks.ExecuteDeleteAsync(cancellationToken);
        await context.Files.ExecuteDeleteAsync(cancellationToken);
        await context.Manifests.ExecuteDeleteAsync(cancellationToken);
    }

    private static void AddMaterializedSourceFiles(
        FhmSaveSqliteContext context,
        IEnumerable<SourceFile> sourceFiles)
    {
        var files = sourceFiles.ToList();
        var teamRecordOrdinalsByRecordIndex = files
            .Where(value => value.Kind == SaveFileKind.Documented)
            .Select(value => FhmSaveFileFactory.TryRead(value.RelativePath, value.Content))
            .OfType<FhmTeamsFile>()
            .SelectMany(value => value.Teams)
            .Select((value, ordinal) => (value.RecordIndex, ordinal))
            .ToDictionary(value => value.RecordIndex, value => value.ordinal);
        var playerTeamRecordOrdinals = files
            .Where(value => value.Kind == SaveFileKind.Documented)
            .Select(value => FhmSaveFileFactory.TryRead(value.RelativePath, value.Content))
            .OfType<FhmPlayersFile>()
            .SelectMany(value => value.Players)
            .ToDictionary(
                value => value.InternalIdentity,
                value => ResolveTeamRecordOrdinal(
                    value.TeamId,
                    teamRecordOrdinalsByRecordIndex,
                    $"player {value.InternalIdentity}"));

        foreach (var sourceFile in files)
        {
            AddBaselineContent(context, sourceFile.RelativePath, sourceFile.Kind, sourceFile.Content);
            if (sourceFile.Kind == SaveFileKind.Documented)
            {
                AddEntities(
                    context,
                    sourceFile.RelativePath,
                    FhmSaveFileFactory.TryRead(sourceFile.RelativePath, sourceFile.Content),
                    playerTeamRecordOrdinals,
                    teamRecordOrdinalsByRecordIndex);
            }
        }
    }

    private static List<SourcePath> GetSourcePaths(string root)
    {
        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var sourcePaths = new List<SourcePath>();
        foreach (var filePath in FhmSaveReader.EnumerateFiles(root))
        {
            var relativePath = FhmSaveFileFactory.NormalizeRelativePath(Path.GetRelativePath(root, filePath));
            if (!paths.Add(relativePath))
            {
                throw new InvalidDataException($"Duplicate save-file path '{relativePath}'.");
            }

            sourcePaths.Add(new(relativePath, filePath));
        }

        return sourcePaths
            .OrderBy(value => GetProjectionPriority(value.RelativePath))
            .ToList();
    }

    private static async Task AddDirectorySourceFilesAsync(
        FhmSaveSqliteContext context,
        IEnumerable<SourcePath> sourcePaths,
        IProgress<FhmSaveReadProgress>? progress,
        CancellationToken cancellationToken)
    {
        var teamRecordOrdinalsByRecordIndex = new Dictionary<int, int>();
        var playerTeamRecordOrdinals = new Dictionary<int, int?>();
        var teamFilesWithDeferredActiveLines = new List<FhmTeamsFile>();
        foreach (var sourcePath in sourcePaths)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var kind = FhmSaveFileFactory.IsDocumentedPath(sourcePath.RelativePath)
                ? SaveFileKind.Documented
                : SaveFileKind.Opaque;
            progress?.Report(new FhmSaveReadProgress("Reading", sourcePath.RelativePath));
            InsertBaselineFile(context, sourcePath.RelativePath, kind);
            var chunkOrdinal = 0;
            using var source = new FileStream(
                sourcePath.FilePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 1024 * 1024,
                FileOptions.SequentialScan);
            var hasOpaqueMultiRecordLeagues = sourcePath.RelativePath.Equals(
                "leagues.dat",
                StringComparison.OrdinalIgnoreCase) &&
                FhmLeaguesFile.HasMultipleRecords(source);
            using var capture = new CapturingReadStream(
                source,
                MaximumInlineFileContentLength,
                chunk => InsertBaselineChunk(context, sourcePath.RelativePath, chunkOrdinal++, chunk));
            progress?.Report(new FhmSaveReadProgress("Decoding", sourcePath.RelativePath));
            if (sourcePath.RelativePath.Equals("players.dat", StringComparison.OrdinalIgnoreCase))
            {
                await BulkInsertAsync(context, cancellationToken).ConfigureAwait(false);
                await AddStreamedPlayersAsync(
                    context,
                    capture,
                    playerTeamRecordOrdinals,
                    teamRecordOrdinalsByRecordIndex,
                    cancellationToken).ConfigureAwait(false);
                capture.Drain();
                var playerContent = capture.Complete();
                if (playerContent.InlineContent is { } inlinePlayerContent)
                {
                    UpdateBaselineFileContent(context, sourcePath.RelativePath, inlinePlayerContent);
                }

                progress?.Report(new FhmSaveReadProgress("Decoded", sourcePath.RelativePath));
                continue;
            }

            if (sourcePath.RelativePath.Equals("personal.dat", StringComparison.OrdinalIgnoreCase))
            {
                await BulkInsertAsync(context, cancellationToken).ConfigureAwait(false);
                using (var reader = new FhmPersonnelFileReader(capture))
                {
                    await AddPersonnelInBatchesAsync(
                        context,
                        reader,
                        teamRecordOrdinalsByRecordIndex,
                        cancellationToken).ConfigureAwait(false);
                }

                capture.Drain();
                var personnelContent = capture.Complete();
                if (personnelContent.InlineContent is { } inlinePersonnelContent)
                {
                    UpdateBaselineFileContent(context, sourcePath.RelativePath, inlinePersonnelContent);
                }

                progress?.Report(new FhmSaveReadProgress("Decoded", sourcePath.RelativePath));
                continue;
            }

            var documented = kind == SaveFileKind.Documented && !hasOpaqueMultiRecordLeagues
                ? FhmSaveFileFactory.TryRead(sourcePath.RelativePath, capture)
                : null;
            capture.Drain();
            var content = capture.Complete();
            if (kind == SaveFileKind.Documented && documented is null && !hasOpaqueMultiRecordLeagues)
            {
                throw new InvalidDataException(
                    $"Documented save file '{sourcePath.RelativePath}' has no matching codec.");
            }

            if (content.InlineContent is { } inlineContent)
            {
                UpdateBaselineFileContent(context, sourcePath.RelativePath, inlineContent);
            }
            progress?.Report(new FhmSaveReadProgress("Decoded", sourcePath.RelativePath));

            if (documented is null)
            {
                continue;
            }

            if (documented is FhmTeamsFile teams)
            {
                foreach (var (team, ordinal) in teams.Teams.Select((value, index) => (value, index)))
                {
                    if (!teamRecordOrdinalsByRecordIndex.TryAdd(team.RecordIndex, ordinal))
                    {
                        throw new InvalidDataException($"Teams file contains duplicate record index {team.RecordIndex}.");
                    }
                }

                AddTeams(
                    context,
                    teams,
                    teamRecordOrdinalsByRecordIndex,
                    includeActiveLineSlots: false);
                teamFilesWithDeferredActiveLines.Add(teams);
                continue;
            }
            else if (documented is FhmPlayersFile players)
            {
                foreach (var player in players.Players)
                {
                    if (!playerTeamRecordOrdinals.TryAdd(
                        player.InternalIdentity,
                        ResolveTeamRecordOrdinal(
                            player.TeamId,
                            teamRecordOrdinalsByRecordIndex,
                            $"player {player.InternalIdentity}")))
                    {
                        throw new InvalidDataException($"Players file contains duplicate internal identity {player.InternalIdentity}.");
                    }
                }
            }
            AddEntities(
                context,
                sourcePath.RelativePath,
                documented,
                playerTeamRecordOrdinals,
                teamRecordOrdinalsByRecordIndex);
        }

        foreach (var teams in teamFilesWithDeferredActiveLines)
        {
            AddTeamActiveLineSlots(context, teams);
        }
    }

    private static void InsertBaselineFile(
        FhmSaveSqliteContext context,
        string relativePath,
        SaveFileKind kind)
    {
        using var command = CreateBaselineCommand(
            context,
            """
            INSERT INTO "SaveFiles" ("RelativePath", "Kind", "Content")
            VALUES ($relativePath, $kind, X'');
            """);
        AddParameter(command, "$relativePath", relativePath);
        AddParameter(command, "$kind", (int)kind);
        command.ExecuteNonQuery();
    }

    private static void UpdateBaselineFileContent(
        FhmSaveSqliteContext context,
        string relativePath,
        byte[] content)
    {
        using var command = CreateBaselineCommand(
            context,
            """
            UPDATE "SaveFiles"
            SET "Content" = $content
            WHERE "RelativePath" = $relativePath;
            """);
        AddParameter(command, "$relativePath", relativePath);
        AddParameter(command, "$content", content);
        if (command.ExecuteNonQuery() != 1)
        {
            throw new InvalidDataException($"Missing baseline save file '{relativePath}'.");
        }
    }

    private static void InsertBaselineChunk(
        FhmSaveSqliteContext context,
        string relativePath,
        int ordinal,
        byte[] content)
    {
        using var command = CreateBaselineCommand(
            context,
            """
            INSERT INTO "SaveFileChunks" ("RelativePath", "Ordinal", "Content")
            VALUES ($relativePath, $ordinal, $content);
            """);
        AddParameter(command, "$relativePath", relativePath);
        AddParameter(command, "$ordinal", ordinal);
        AddParameter(command, "$content", content);
        command.ExecuteNonQuery();
    }

    private static System.Data.Common.DbCommand CreateBaselineCommand(
        FhmSaveSqliteContext context,
        string commandText)
    {
        var transaction = context.Database.CurrentTransaction?.GetDbTransaction()
            ?? throw new InvalidOperationException("A SQLite baseline write requires an active transaction.");
        var command = context.Database.GetDbConnection().CreateCommand();
        command.Transaction = transaction;
        command.CommandText = commandText;
        return command;
    }

    private static void AddParameter(
        System.Data.Common.DbCommand command,
        string name,
        object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }

    private static int GetProjectionPriority(string relativePath) =>
        relativePath.Equals("names.dat", StringComparison.OrdinalIgnoreCase) ? 0 :
        relativePath.Equals("player_roles.dat", StringComparison.OrdinalIgnoreCase) ? 1 :
        relativePath.Equals("teams.dat", StringComparison.OrdinalIgnoreCase) ? 2 :
        relativePath.Equals("players.dat", StringComparison.OrdinalIgnoreCase) ? 3 : 4;

    private static void AddBaselineContent(
        FhmSaveSqliteContext context,
        string relativePath,
        SaveFileKind kind,
        byte[] content)
    {
        context.Files.Add(new SaveFile
        {
            RelativePath = relativePath,
            Kind = kind,
            Content = content.Length > MaximumInlineFileContentLength ? [] : content,
        });

        foreach (var chunk in SplitLargeContent(relativePath, content))
        {
            context.FileChunks.Add(chunk);
        }
    }

    private static IEnumerable<SaveFileChunk> SplitLargeContent(string relativePath, byte[] content)
    {
        if (content.Length <= MaximumInlineFileContentLength)
        {
            yield break;
        }

        for (int offset = 0, ordinal = 0; offset < content.Length; ordinal++)
        {
            var length = Math.Min(MaximumInlineFileContentLength, content.Length - offset);
            yield return new SaveFileChunk
            {
                RelativePath = relativePath,
                Ordinal = ordinal,
                Content = content.AsSpan(offset, length).ToArray(),
            };
            offset += length;
        }
    }

    private static List<SourceFile> GetSourceFiles(FhmSave save)
    {
        var result = new List<SourceFile>();
        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in save.Files.Values)
        {
            ArgumentNullException.ThrowIfNull(file);
            var relativePath = FhmSaveFileFactory.NormalizeRelativePath(file.RelativePath);
            if (FhmSaveAuxiliaryPaths.IsIgnored(relativePath))
            {
                continue;
            }

            if (!paths.Add(relativePath))
            {
                throw new InvalidDataException($"Duplicate save-file path '{relativePath}'.");
            }

            result.Add(new(relativePath, SaveFileKind.Documented, FhmSaveFileFactory.Write(file)));
        }

        foreach (var file in save.OpaqueFiles)
        {
            ArgumentNullException.ThrowIfNull(file);
            var relativePath = FhmSaveFileFactory.NormalizeRelativePath(file.RelativePath);
            if (FhmSaveAuxiliaryPaths.IsIgnored(relativePath))
            {
                continue;
            }

            if (!paths.Add(relativePath))
            {
                throw new InvalidDataException($"Duplicate save-file path '{relativePath}'.");
            }

            result.Add(new(relativePath, SaveFileKind.Opaque, file.Content.ToArray()));
        }

        return result;
    }

    private static void AddEntities(
        FhmSaveSqliteContext context,
        string relativePath,
        IFhmSaveFile? file,
        IReadOnlyDictionary<int, int?> playerTeamRecordOrdinals,
        IReadOnlyDictionary<int, int> teamRecordOrdinalsByRecordIndex)
    {
        switch (file)
        {
            case FhmNamesFile names:
                AddNames(context, names);
                break;
            case FhmPlayersFile players:
                AddPlayers(context, players, teamRecordOrdinalsByRecordIndex);
                break;
            case FhmPlayerRolesFile playerRoles:
                AddPlayerRoles(context, playerRoles);
                break;
            case FhmPersonnelFile personnel:
                context.Personnel.AddRange(CreatePersonnel(context, personnel, teamRecordOrdinalsByRecordIndex));
                break;
            case FhmTeamsFile teams:
                AddTeams(context, teams, teamRecordOrdinalsByRecordIndex);
                break;
            case FhmGameSettingsFile gameSettings:
                AddGameSettings(context, gameSettings);
                break;
            case FhmStoredLinesFile storedLines:
                AddStoredLines(context, storedLines, playerTeamRecordOrdinals);
                break;
            case FhmTeamTacticsFile tacticSystems:
                context.TacticFiles.Add(new TacticFile
                {
                    RelativePath = relativePath,
                    Kind = TacticFileKind.TacticSystems,
                    Version = tacticSystems.VersionTag,
                    RecordCount = tacticSystems.Records.Count,
                });
                foreach (var (record, ordinal) in tacticSystems.Records.Select((value, index) => (value, index)))
                {
                    context.TacticSystems.Add(new TacticSystem
                    {
                        RecordOrdinal = ordinal,
                        GlobalId = record.GlobalId,
                        ZoneGroupRaw = record.ZoneGroupRaw,
                        Name = record.Name,
                        RatingA = record.RatingA,
                        RatingB = record.RatingB,
                    });
                }

                break;
            case FhmTacticTemplatesFile templates:
                context.TacticFiles.Add(new TacticFile
                {
                    RelativePath = relativePath,
                    Kind = TacticFileKind.TacticTemplates,
                    Version = templates.Version,
                    RecordCount = templates.Templates.Count,
                });
                foreach (var (template, ordinal) in templates.Templates.Select((value, index) => (value, index)))
                {
                    context.TacticTemplates.Add(new TacticTemplate
                    {
                        RecordOrdinal = ordinal,
                        InternalKey = template.InternalKey,
                        TemplateIndex = template.TemplateIndex,
                        DisplayName = template.DisplayName,
                        SettingsBlob = template.SettingsBlob.ToArray(),
                    });
                }

                break;
            case FhmSetPlayFile setPlay:
                context.TacticFiles.Add(new TacticFile
                {
                    RelativePath = relativePath,
                    Kind = TacticFileKind.SetPlay,
                    Version = setPlay.Version,
                    RecordCount = setPlay.Formations.Count,
                });
                AddSetPlayBlocks(context, relativePath, false, setPlay.Formations);
                AddSetPlayBlocks(context, relativePath, true, setPlay.ExtraRecords);
                break;
            case FhmLengthPrefixedCatalogueFile catalogue:
                context.TacticFiles.Add(new TacticFile
                {
                    RelativePath = relativePath,
                    Kind = TacticFileKind.ModifierCatalogue,
                    Version = catalogue.Version,
                    RecordCount = catalogue.Blocks.Count,
                });
                AddModifierBlocks(context, relativePath, catalogue.Blocks);
                break;
            case FhmZoneEventModifiersFile zoneEvents:
                context.TacticFiles.Add(new TacticFile
                {
                    RelativePath = relativePath,
                    Kind = TacticFileKind.ZoneEventModifiers,
                    Version = zoneEvents.Version,
                    RecordCount = zoneEvents.ZoneCount,
                });
                AddModifierBlocks(context, relativePath, zoneEvents.ModifierGrids);
                break;
            case FhmTacticsFile tactics:
                context.Tactics.Add(new Tactics
                {
                    Version = tactics.Version,
                    TacticCount = tactics.TacticCount,
                    RecordsOpaque = tactics.RecordsOpaque.ToArray(),
                });
                break;
        }
    }

    private static void AddNames(FhmSaveSqliteContext context, FhmNamesFile file)
    {
        var nextListEntryId = 1;
        var nextScalarId = 1;
        foreach (var (entry, ordinal) in file.MasterNames.Select((value, index) => (value, index)))
        {
            context.Names.Add(new Name
            {
                NameId = entry.NameId,
                Ordinal = ordinal,
                Text = entry.Text,
                GroupId = entry.GroupId,
                CategoryWeight = entry.CategoryWeight,
                FlagA = entry.FlagA,
                FlagB = entry.FlagB,
                FlagC = entry.FlagC,
            });
        }

        AddNameLists(context, NameListKind.FirstName, file.FirstNameLists, ref nextListEntryId);
        AddNameLists(context, NameListKind.Surname, file.SurnameLists, ref nextListEntryId);
        AddNameScalars(context, NameScalarKind.ScalarA, file.ScalarArrayA, ref nextScalarId);
        AddNameScalars(context, NameScalarKind.ScalarB, file.ScalarArrayB, ref nextScalarId);
    }

    private static void AddNameLists(
        FhmSaveSqliteContext context,
        NameListKind kind,
        IEnumerable<IList<int>> values,
        ref int nextSyntheticId)
    {
        foreach (var (list, nation) in values.Select((value, index) => (value, index)))
        {
            foreach (var (nameId, ordinal) in list.Select((value, index) => (value, index)))
            {
                context.NameListEntries.Add(new NameListEntry
                {
                    Id = nextSyntheticId++,
                    Kind = kind,
                    NationIndex = nation,
                    Ordinal = ordinal,
                    NameId = ToNullableReference(nameId),
                });
            }
        }
    }

    private static void AddNameScalars(
        FhmSaveSqliteContext context,
        NameScalarKind kind,
        IEnumerable<int> values,
        ref int nextSyntheticId)
    {
        foreach (var (value, ordinal) in values.Select((item, index) => (item, index)))
        {
            context.NameScalars.Add(new NameScalar
            {
                Id = nextSyntheticId++,
                Kind = kind,
                Ordinal = ordinal,
                Value = ToNullableReference(value),
            });
        }
    }

    private static void AddPlayers(
        FhmSaveSqliteContext context,
        FhmPlayersFile file,
        IReadOnlyDictionary<int, int> teamRecordOrdinalsByRecordIndex)
    {
        foreach (var (player, ordinal) in file.Players.Select((value, index) => (value, index)))
        {
            AddPlayer(context, player, ordinal, teamRecordOrdinalsByRecordIndex);
        }
    }

    private static async Task AddStreamedPlayersAsync(
        FhmSaveSqliteContext context,
        Stream source,
        IDictionary<int, int?> playerTeamRecordOrdinals,
        IReadOnlyDictionary<int, int> teamRecordOrdinalsByRecordIndex,
        CancellationToken cancellationToken)
    {
        using var reader = new FhmPlayersFileReader(source);
        var ordinal = 0;
        foreach (var player in reader)
        {
            var teamRecordOrdinal = ResolveTeamRecordOrdinal(
                player.TeamId,
                teamRecordOrdinalsByRecordIndex,
                $"player {player.InternalIdentity}");
            if (!playerTeamRecordOrdinals.TryAdd(player.InternalIdentity, teamRecordOrdinal))
            {
                throw new InvalidDataException($"Players file contains duplicate internal identity {player.InternalIdentity}.");
            }

            AddPlayer(context, player, ordinal++, teamRecordOrdinalsByRecordIndex);
            if (ordinal % SqliteBulkInsertBatchSize == 0)
            {
                await BulkInsertAsync(context, cancellationToken).ConfigureAwait(false);
            }
        }

        await BulkInsertAsync(context, cancellationToken).ConfigureAwait(false);
    }

    private static void AddPlayer(
        FhmSaveSqliteContext context,
        FhmPlayerRecord player,
        int ordinal,
        IReadOnlyDictionary<int, int> teamRecordOrdinalsByRecordIndex)
    {
        context.Players.Add(new Player
        {
            RecordOrdinal = ordinal,
            InternalId = player.InternalIdentity,
            ExternalId = player.ExportedPlayerId,
            FirstNameId = ToNullableReference(player.FirstNameId),
            SurnameId = ToNullableReference(player.SurnameId),
            CommonNameId = ToNullableReference(player.CommonNameId),
            BirthDate = new DateOnly(player.BirthDate.Year, player.BirthDate.Month, player.BirthDate.Day),
            TeamRecordOrdinal = ResolveTeamRecordOrdinal(
                player.TeamId,
                teamRecordOrdinalsByRecordIndex,
                $"player {player.InternalIdentity}"),
            FranchiseId = ToNullableReference(player.FranchiseId),
            PrimaryContractRole = player.PrimaryContractRole.Value,
            SupplementaryContractRole = player.SupplementaryContractRole.Value,
            PositionAffinity = new PlayerPositionAffinity
            {
                Goalie = player.PositionRatings.Goalie,
                LeftDefense = player.PositionRatings.LeftDefenceman,
                RightDefense = player.PositionRatings.RightDefenceman,
                LeftWing = player.PositionRatings.LeftWing,
                Center = player.PositionRatings.Centre,
                RightWing = player.PositionRatings.RightWing,
            },
            SerializedRecord = player.ToBytes(),
        });
        context.PlayerAttributes.Add(ToPlayerAttributes(player.InternalIdentity, player.RatingAttributes));
        AddPlayerRoleAssignment(context, player.InternalIdentity, PlayerRoleSlot.Tactical, player.TacticalRole);
        AddPlayerRoleAssignment(context, player.InternalIdentity, PlayerRoleSlot.SecondaryTactical, player.SecondaryTacticalRole);
        foreach (var (contract, contractOrdinal) in player.Contracts.Select((value, index) => (value, index)))
        {
            context.PlayerContracts.Add(new PlayerContract
            {
                PlayerInternalId = player.InternalIdentity,
                ContractOrdinal = contractOrdinal,
            });
            foreach (var (salary, yearOrdinal) in contract.Salaries.Select((value, index) => (value, index)))
            {
                context.PlayerContractYears.Add(new PlayerContractYear
                {
                    PlayerInternalId = player.InternalIdentity,
                    ContractOrdinal = contractOrdinal,
                    YearNumber = yearOrdinal + 1,
                    MajorLeagueSalary = salary.MajorLeagueSalary,
                    MinorLeagueSalary = salary.MinorLeagueSalary,
                });
            }
        }
    }

    private static void AddPlayerRoles(FhmSaveSqliteContext context, FhmPlayerRolesFile file)
    {
        context.TacticalRoleCatalogues.Add(new PlayerRoleCatalogue { VersionTag = file.VersionTag });
        foreach (var (role, recordOrdinal) in file.Records.Select((value, index) => (value, index)))
        {
            context.TacticalRoles.Add(new PlayerRoleDefinition
            {
                RoleId = (InGameRole)role.RoleId,
                RecordOrdinal = recordOrdinal,
                Name = role.Name,
                AppliesToForwards = role.AppliesToForwards,
                AppliesToDefencemen = role.AppliesToDefencemen,
                AppliesToGoalies = role.AppliesToGoalies,
                RoleFlags = role.RoleFlags,
                PositionCategory = role.PositionCategory,
                ShortName = role.ShortName,
                TuningValueA = role.TuningValueA,
                TuningValueB = role.TuningValueB,
                Description = role.Description,
                TuningValueC = role.TuningValueC,
            });
            AddPlayerRoleWeights(context, (InGameRole)role.RoleId, PlayerRoleWeightGroup.A, role.WeightGroupA);
            AddPlayerRoleWeights(context, (InGameRole)role.RoleId, PlayerRoleWeightGroup.B, role.WeightGroupB);
            AddPlayerRoleWeights(context, (InGameRole)role.RoleId, PlayerRoleWeightGroup.C, role.WeightGroupC);
            AddPlayerRoleWeights(context, (InGameRole)role.RoleId, PlayerRoleWeightGroup.D, role.WeightGroupD);
            AddPlayerRoleWeights(context, (InGameRole)role.RoleId, PlayerRoleWeightGroup.E, role.WeightGroupE);
            AddPlayerRoleWeights(context, (InGameRole)role.RoleId, PlayerRoleWeightGroup.F, role.WeightGroupF);
            foreach (var (list, listIndex) in role.IndexLists.Select((value, index) => (value, index)))
            {
                foreach (var (value, ordinal) in list.Select((item, index) => (item, index)))
                {
                    context.TacticalRoleIndexEntries.Add(new PlayerRoleIndexEntry
                    {
                        RoleId = (InGameRole)role.RoleId,
                        List = (PlayerRoleIndexList)listIndex,
                        Ordinal = ordinal,
                        Value = value,
                    });
                }
            }
        }
    }

    private static void AddPlayerRoleWeights(
        FhmSaveSqliteContext context,
        InGameRole roleId,
        PlayerRoleWeightGroup group,
        IEnumerable<int> values)
    {
        foreach (var (value, ordinal) in values.Select((item, index) => (item, index)))
        {
            context.TacticalRoleWeights.Add(new PlayerRoleWeight
            {
                RoleId = roleId,
                Group = group,
                Ordinal = ordinal,
                Value = value,
            });
        }
    }

    private static void AddPlayerRoleAssignment(
        FhmSaveSqliteContext context,
        int playerInternalId,
        PlayerRoleSlot slot,
        FhmPlayerRoleInstance? role)
    {
        if (role is null)
        {
            return;
        }

        context.PlayerTacticalRoleAssignments.Add(new PlayerRoleAssignment
        {
            PlayerInternalId = playerInternalId,
            Slot = slot,
            RoleId = (InGameRole)role.RoleId,
        });
        foreach (var tendency in Enum.GetValues<PlayerRoleTendency>())
        {
            var ordinal = (int)tendency;
            context.PlayerTacticalRoleTendencyValues.Add(new PlayerRoleTendencyValue
            {
                PlayerInternalId = playerInternalId,
                Slot = slot,
                Tendency = tendency,
                UseOverride = role.UseOverride[ordinal],
                Value = role.TendencyValue[ordinal],
            });
        }
    }

    private static async Task AddPersonnelInBatchesAsync(
        FhmSaveSqliteContext context,
        IEnumerable<FhmPersonnelRecord> records,
        IReadOnlyDictionary<int, int> teamRecordOrdinalsByRecordIndex,
        CancellationToken cancellationToken)
    {
        var knownNameIds = GetKnownNameIds(context);
        var batchCount = 0;
        foreach (var record in records)
        {
            context.Personnel.Add(CreatePersonnel(record, knownNameIds, teamRecordOrdinalsByRecordIndex));
            batchCount++;
            if (batchCount < SqliteBulkInsertBatchSize)
            {
                continue;
            }

            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            context.ChangeTracker.Clear();
            batchCount = 0;
        }

        if (batchCount > 0)
        {
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            context.ChangeTracker.Clear();
        }
    }

    private static IEnumerable<Personnel> CreatePersonnel(
        FhmSaveSqliteContext context,
        FhmPersonnelFile file,
        IReadOnlyDictionary<int, int> teamRecordOrdinalsByRecordIndex)
    {
        var knownNameIds = GetKnownNameIds(context);
        foreach (var record in file.Records)
        {
            yield return CreatePersonnel(record, knownNameIds, teamRecordOrdinalsByRecordIndex);
        }
    }

    private static HashSet<int> GetKnownNameIds(FhmSaveSqliteContext context)
    {
        var knownNameIds = context.Names
            .AsNoTracking()
            .Select(value => value.NameId)
            .ToHashSet();
        knownNameIds.UnionWith(context.ChangeTracker.Entries<Name>()
            .Where(entry => entry.State == EntityState.Added)
            .Select(entry => entry.Entity.NameId));
        return knownNameIds;
    }

    private static Personnel CreatePersonnel(
        FhmPersonnelRecord record,
        IReadOnlySet<int> knownNameIds,
        IReadOnlyDictionary<int, int> teamRecordOrdinalsByRecordIndex)
    {
        int? teamRecordOrdinal = null;
        int? unresolvedTeamRecordIndex = null;
        if (record.TeamRecordIndex is int teamRecordIndex)
        {
            if (!teamRecordOrdinalsByRecordIndex.TryGetValue(teamRecordIndex, out var resolvedTeamRecordOrdinal))
            {
                unresolvedTeamRecordIndex = teamRecordIndex;
            }
            else
            {
                teamRecordOrdinal = resolvedTeamRecordOrdinal;
            }
        }

        return new()
        {
            PersonnelId = record.PersonnelId,
            FirstNameNameId = record.FirstNameNameId,
            FirstNameLookupNameId = knownNameIds.Contains(record.FirstNameNameId)
                ? record.FirstNameNameId
                : null,
            SurnameNameId = record.SurnameNameId,
            SurnameLookupNameId = knownNameIds.Contains(record.SurnameNameId)
                ? record.SurnameNameId
                : null,
            NicknameNameId = record.NicknameNameId,
            NicknameLookupNameId = record.NicknameNameId is int nicknameNameId &&
                knownNameIds.Contains(nicknameNameId)
                ? nicknameNameId
                : null,
            BirthYear = record.BirthDate.Year,
            BirthMonth = record.BirthDate.Month,
            BirthDay = record.BirthDate.Day,
            NationalityId = record.NationalityId,
            BirthCityId = record.BirthCityId,
            TeamRecordOrdinal = teamRecordOrdinal,
            UnresolvedTeamRecordIndex = unresolvedTeamRecordIndex,
            Job = record.Job,
            Negotiating = record.Negotiating,
            OffensivePreference = record.OffensivePreference,
            PlayerManagement = record.PlayerManagement,
            PhysicalPreference = record.PhysicalPreference,
            CoachingDefense = record.CoachingDefense,
            CoachingForwards = record.CoachingForwards,
            CoachingGoalies = record.CoachingGoalies,
            CoachingProspects = record.CoachingProspects,
            EvaluateAbilities = record.EvaluateAbilities,
            EvaluatePotential = record.EvaluatePotential,
            Reputation = record.Reputation,
            LineMatchingTendency = record.LineMatchingTendency,
            GoalieHandlingTendency = record.GoalieHandlingTendency,
            VeteranPreference = record.VeteranPreference,
            InnovationTendency = record.InnovationTendency,
            LoyaltyTendency = record.LoyaltyTendency,
            Salary = record.Salary,
            ContractLength = record.ContractLength,
            Retired = record.Retired,
            DefensiveSkills = record.DefensiveSkills,
            OffensiveSkills = record.OffensiveSkills,
            BasedInLocationId = record.BasedInLocationId,
            PhysicalTraining = record.PhysicalTraining,
            Tactics = record.Tactics,
            Discipline = record.Discipline,
            SelfPreservation = record.SelfPreservation,
            Motivation = record.Motivation,
            IngameTactics = record.IngameTactics,
            TrainerSkill = record.TrainerSkill,
            SerializedRecord = record.GetSourceBytes(),
        };
    }

    private static void AddTeams(
        FhmSaveSqliteContext context,
        FhmTeamsFile file,
        IReadOnlyDictionary<int, int> teamRecordOrdinalsByRecordIndex,
        bool includeActiveLineSlots = true)
    {
        foreach (var (team, ordinal) in file.Teams.Select((value, index) => (value, index)))
        {
            context.Teams.Add(new Team
            {
                RecordOrdinal = ordinal,
                RecordIndex = team.RecordIndex,
                TeamId = team.TeamId,
                InternalCode = team.InternalCode,
                InternalCode2 = team.InternalCode2,
                Flag1 = team.Flag1,
                City = team.City,
                Nickname = team.Nickname,
                NicknamePlacement = team.NicknamePlacement,
                AffiliateParentRecordOrdinal = ResolveTeamRecordOrdinal(
                    team.AffiliateParentId,
                    teamRecordOrdinalsByRecordIndex,
                    $"team {ordinal} affiliate parent"),
                SecondaryAffiliateParentRecordOrdinal = ResolveTeamRecordOrdinal(
                    team.AffiliateParentId2,
                    teamRecordOrdinalsByRecordIndex,
                    $"team {ordinal} secondary affiliate parent"),
                LeagueId = team.LeagueId,
                ConferenceId = team.ConferenceId,
                DivisionId = team.DivisionId,
                LocationId = team.LocationId,
                MarketSize = team.MarketSize,
                FanLoyalty = team.FanLoyalty,
                Finance1 = team.Finance1,
                Finance2 = team.Finance2,
                Finance3 = team.Finance3,
                Finance4 = team.Finance4,
            });
            context.TeamTactics.Add(new TeamTactic
            {
                TeamRecordOrdinal = ordinal,
                SerializedSettings = SerializeTeamTactics(team.Tail.Tactics),
            });
        }

        if (includeActiveLineSlots)
        {
            AddTeamActiveLineSlots(context, file);
        }
    }

    private static void AddTeamActiveLineSlots(FhmSaveSqliteContext context, FhmTeamsFile file)
    {
        foreach (var (team, ordinal) in file.Teams.Select((value, index) => (value, index)))
        {
            foreach (var line in team.ActiveLines.Lists)
            {
                foreach (var (playerId, slotOrdinal) in line.PlayerReferences.Select((value, index) => (value, index)))
                {
                    context.TeamActiveLineSlots.Add(new TeamActiveLineSlot
                    {
                        TeamRecordOrdinal = ordinal,
                        Group = line.Group,
                        SlotOrdinal = slotOrdinal,
                        PlayerInternalId = ToNullableReference(playerId),
                    });
                }
            }
        }
    }

    private static void AddGameSettings(FhmSaveSqliteContext context, FhmGameSettingsFile file)
    {
        for (var ordinal = 0; ordinal < file.Values.Count; ordinal++)
        {
            context.GameSettings.Add(ToGameSetting(ordinal, file.Values[ordinal]));
        }
    }

    private static void AddStoredLines(
        FhmSaveSqliteContext context,
        FhmStoredLinesFile file,
        IReadOnlyDictionary<int, int?> playerTeamRecordOrdinals)
    {
        foreach (var (line, lineOrdinal) in file.StoredLines.Select((value, index) => (value, index)))
        {
            var teamRecordOrdinals = line.PlayerGroups
                .SelectMany(value => value)
                .Where(value => value != FhmNullConstants.Null)
                .Select(value => playerTeamRecordOrdinals.GetValueOrDefault(value))
                .Where(value => value.HasValue)
                .Select(value => value!.Value)
                .Distinct()
                .ToArray();
            context.StoredLines.Add(new StoredLine
            {
                LineOrdinal = lineOrdinal,
                Name = line.Name,
                TeamRecordOrdinal = teamRecordOrdinals.Length == 1 ? teamRecordOrdinals[0] : null,
            });
            for (var groupOrdinal = 0; groupOrdinal < line.PlayerGroups.Count; groupOrdinal++)
            {
                var players = line.PlayerGroups[groupOrdinal];
                var locks = line.UnitLocks[groupOrdinal];
                for (var slotOrdinal = 0; slotOrdinal < Math.Max(players.Count, locks.Count); slotOrdinal++)
                {
                    context.StoredLineSlots.Add(new StoredLineSlot
                    {
                        LineOrdinal = lineOrdinal,
                        GroupOrdinal = groupOrdinal,
                        SlotOrdinal = slotOrdinal,
                        PlayerInternalId = slotOrdinal < players.Count
                            ? ToNullableReference(players[slotOrdinal])
                            : null,
                        UnitLock = slotOrdinal < locks.Count ? locks[slotOrdinal] : null,
                    });
                }
            }
        }
    }

    private static void AddSetPlayBlocks(
        FhmSaveSqliteContext context,
        string relativePath,
        bool isExtraRecord,
        IEnumerable<FhmLengthPrefixedBlock> blocks)
    {
        foreach (var (block, ordinal) in blocks.Select((value, index) => (value, index)))
        {
            context.SetPlays.Add(new SetPlay
            {
                RelativePath = relativePath,
                IsExtraRecord = isExtraRecord,
                RecordOrdinal = ordinal,
                Data = block.Data.ToArray(),
            });
        }
    }

    private static void AddModifierBlocks(
        FhmSaveSqliteContext context,
        string relativePath,
        IEnumerable<FhmLengthPrefixedBlock> blocks)
    {
        foreach (var (block, ordinal) in blocks.Select((value, index) => (value, index)))
        {
            context.ModifierCatalogues.Add(new ModifierCatalogue
            {
                RelativePath = relativePath,
                RecordOrdinal = ordinal,
                Data = block.Data.ToArray(),
            });
        }
    }

    internal static PlayerAttributes ToPlayerAttributes(int playerId, FhmPlayerAttributes source)
    {
        var result = new PlayerAttributes { PlayerId = playerId };
        foreach (var target in typeof(PlayerAttributes).GetProperties())
        {
            if (target.Name == nameof(PlayerAttributes.PlayerId) ||
                target.PropertyType != typeof(int))
            {
                continue;
            }

            var sourceName = target.Name switch
            {
                nameof(PlayerAttributes.DevelopmentRate) => nameof(FhmPlayerAttributes.DevRate),
                nameof(PlayerAttributes.TeamPlayer) => nameof(FhmPlayerAttributes.Teamplayer),
                nameof(PlayerAttributes.Puckhandling) => nameof(FhmPlayerAttributes.PuckHandling),
                nameof(PlayerAttributes.GoaliePokeCheck) => nameof(FhmPlayerAttributes.GoaliePokecheck),
                nameof(PlayerAttributes.Reflexes) => nameof(FhmPlayerAttributes.GoalieReflexes),
                nameof(PlayerAttributes.Skating) => nameof(FhmPlayerAttributes.GoalieSkating),
                _ => target.Name,
            };
            target.SetValue(result, (int)(byte)typeof(FhmPlayerAttributes).GetProperty(sourceName)!.GetValue(source)!);
        }

        return result;
    }

    internal static GameSetting ToGameSetting(int ordinal, object? value) => value switch
    {
        byte number => new ByteGameSetting { SettingOrdinal = ordinal, Value = number },
        ushort number => new UInt16GameSetting { SettingOrdinal = ordinal, Value = number },
        int number => new Int32GameSetting { SettingOrdinal = ordinal, Value = number },
        double number => new DoubleGameSetting { SettingOrdinal = ordinal, Value = number },
        string or null => new QStringGameSetting { SettingOrdinal = ordinal, Value = (string?)value },
        _ => throw new InvalidDataException($"Unsupported game-settings value type '{value.GetType().Name}'."),
    };

    internal static byte[] SerializeTeamTactics(FhmTeamTacticsSettings tactics)
    {
        using var stream = new MemoryStream();
        tactics.WriteTo(stream);
        return stream.ToArray();
    }

    private static int? ToNullableReference(int value) =>
        value == FhmNullConstants.Null ? null : value;

    private static int? ResolveTeamRecordOrdinal(
        int teamRecordIndex,
        IReadOnlyDictionary<int, int> teamRecordOrdinalsByRecordIndex,
        string owner)
    {
        if (teamRecordIndex == FhmNullConstants.Null)
        {
            return null;
        }

        if (!teamRecordOrdinalsByRecordIndex.TryGetValue(teamRecordIndex, out var teamRecordOrdinal))
        {
            throw new InvalidDataException($"{owner} references missing team record index {teamRecordIndex}.");
        }

        return teamRecordOrdinal;
    }

    private sealed record SourceFile(string RelativePath, SaveFileKind Kind, byte[] Content);

    private sealed record SourcePath(string RelativePath, string FilePath);

    private sealed record TeamAffiliateReferences(
        Team Team,
        int? AffiliateParentRecordOrdinal,
        int? SecondaryAffiliateParentRecordOrdinal);
    private sealed record CapturedFileContent(byte[]? InlineContent);

    private sealed class CapturingReadStream : Stream
    {
        private const int InitialBufferLength = 64 * 1024;

        private readonly Stream source;
        private readonly int chunkLength;
        private readonly Action<byte[]> onChunk;
        private byte[]? buffer;
        private int bufferedLength;
        private bool hasChunks;
        private bool reachedEnd;
        private bool completed;

        public CapturingReadStream(Stream source, int chunkLength, Action<byte[]> onChunk)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentOutOfRangeException.ThrowIfLessThan(chunkLength, 1);
            ArgumentNullException.ThrowIfNull(onChunk);
            if (!source.CanRead)
            {
                throw new ArgumentException("The source stream must be readable.", nameof(source));
            }

            this.source = source;
            this.chunkLength = chunkLength;
            this.onChunk = onChunk;
        }

        public override bool CanRead => !completed && source.CanRead;

        public override bool CanSeek => source.CanSeek;

        public override bool CanWrite => false;

        public override long Length => source.Length;

        public override long Position
        {
            get => source.Position;
            set => throw new NotSupportedException("Captured source streams do not support repositioning.");
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count) =>
            Read(buffer.AsSpan(offset, count));

        public override int Read(Span<byte> destination)
        {
            ObjectDisposedException.ThrowIf(completed, this);
            var count = source.Read(destination);
            if (count == 0)
            {
                reachedEnd = true;
            }
            else
            {
                Capture(destination[..count]);
            }

            return count;
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public void Drain()
        {
            Span<byte> buffer = stackalloc byte[8192];
            while (Read(buffer) != 0)
            {
            }
        }

        public CapturedFileContent Complete()
        {
            ObjectDisposedException.ThrowIf(completed, this);
            if (!reachedEnd)
            {
                throw new InvalidOperationException("The source stream must be fully consumed before completing capture.");
            }

            completed = true;
            if (hasChunks)
            {
                if (bufferedLength > 0)
                {
                    var finalChunk = new byte[bufferedLength];
                    buffer!.AsSpan(0, bufferedLength).CopyTo(finalChunk);
                    onChunk(finalChunk);
                }

                return new CapturedFileContent(null);
            }

            var inlineContent = new byte[bufferedLength];
            buffer!.AsSpan(0, bufferedLength).CopyTo(inlineContent);
            return new CapturedFileContent(inlineContent);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                completed = true;
                buffer = null;
            }

            base.Dispose(disposing);
        }

        private void Capture(ReadOnlySpan<byte> sourceContent)
        {
            while (!sourceContent.IsEmpty)
            {
                EnsureBuffer();
                var copiedLength = Math.Min(buffer!.Length - bufferedLength, sourceContent.Length);
                sourceContent[..copiedLength].CopyTo(buffer.AsSpan(bufferedLength));
                bufferedLength += copiedLength;
                sourceContent = sourceContent[copiedLength..];

                if (bufferedLength == chunkLength)
                {
                    onChunk(buffer);
                    hasChunks = true;
                    buffer = null;
                    bufferedLength = 0;
                }
            }
        }

        private void EnsureBuffer()
        {
            if (buffer is null)
            {
                buffer = new byte[Math.Min(InitialBufferLength, chunkLength)];
            }
            else if (bufferedLength == buffer.Length && buffer.Length < chunkLength)
            {
                var expanded = new byte[Math.Min(buffer.Length * 2, chunkLength)];
                buffer.CopyTo(expanded, 0);
                buffer = expanded;
            }
        }
    }
}
