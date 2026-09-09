using Microsoft.EntityFrameworkCore;
using Shuttle.Fhm.Serde.Domain.Binary;
using Shuttle.Fhm.Serde.Domain.Files;
using Shuttle.Fhm.Serde.Domain.Model;
using Shuttle.Fhm.Serde.Domain.SaveFolder;

namespace Shuttle.Fhm.Serde.Sqlite;

/// <summary>Options governing direct SQLite-to-save-folder exports.</summary>
public sealed class FhmSaveSqliteExportOptions
{
    /// <summary>Gets or sets an optional recipient of completed export-phase timings.</summary>
    public IProgress<FhmSaveSqliteExportProgress>? Progress { get; init; }
}

/// <summary>Reports elapsed time for a completed direct SQLite export phase.</summary>
public sealed record FhmSaveSqliteExportProgress(string Phase, TimeSpan Elapsed);

/// <summary>Validates and exports an FHM save from the SQLite adapter format.</summary>
public sealed class FhmSaveSqliteReader
{
    /// <summary>Reads and validates a save database into a lossless <see cref="FhmSave"/>.</summary>
    public async Task<FhmSave> ReadAsync(string databasePath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);
        var fullPath = Path.GetFullPath(databasePath);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"SQLite save database '{fullPath}' does not exist.", fullPath);
        }

        await using var context = await FhmSaveSqliteContext.OpenAsync(fullPath, cancellationToken);
        await context.Database.OpenConnectionAsync(cancellationToken);
        await FhmSaveSqliteContext.ConfigureReadConnectionAsync(context, cancellationToken);
        var manifest = await context.Manifests.AsNoTracking().SingleOrDefaultAsync(cancellationToken)
            ?? throw new InvalidDataException("SQLite save database does not contain a manifest.");
        if (manifest.Id != 1 || manifest.SchemaVersion != FhmSaveSqliteWriter.SchemaVersion ||
            manifest.SourceFormatVersion != "FHM 10 save folder")
        {
            throw new InvalidDataException(
                $"SQLite save database has unsupported manifest schema {manifest.SchemaVersion} or source format '{manifest.SourceFormatVersion}'.");
        }

        var files = await context.Files.AsNoTracking().ToListAsync(cancellationToken);
        var fileChunks = await context.FileChunks.AsNoTracking()
            .OrderBy(value => value.RelativePath)
            .ThenBy(value => value.Ordinal)
            .ToListAsync(cancellationToken);
        files = RehydrateFileContents(files, fileChunks);
        ValidateBaselineFiles(files, manifest);

        var names = await context.Names.AsNoTracking().ToListAsync(cancellationToken);
        var nameListEntries = await context.NameListEntries.AsNoTracking().ToListAsync(cancellationToken);
        var nameScalars = await context.NameScalars.AsNoTracking().ToListAsync(cancellationToken);
        var players = await context.Players.AsNoTracking().IgnoreAutoIncludes().ToListAsync(cancellationToken);
        var attributes = await context.PlayerAttributes.AsNoTracking().ToListAsync(cancellationToken);
        var contracts = await context.PlayerContracts.AsNoTracking().IgnoreAutoIncludes().ToListAsync(cancellationToken);
        var contractYears = await context.PlayerContractYears.AsNoTracking().ToListAsync(cancellationToken);
        var playerRoleCatalogues = await context.TacticalRoleCatalogues.AsNoTracking().ToListAsync(cancellationToken);
        var playerRoles = await context.TacticalRoles.AsNoTracking().IgnoreAutoIncludes().ToListAsync(cancellationToken);
        var playerRoleWeights = await context.TacticalRoleWeights.AsNoTracking().ToListAsync(cancellationToken);
        var playerRoleIndexEntries = await context.TacticalRoleIndexEntries.AsNoTracking().ToListAsync(cancellationToken);
        var playerRoleAssignments = await context.PlayerTacticalRoleAssignments.AsNoTracking().IgnoreAutoIncludes().ToListAsync(cancellationToken);
        var playerRoleTendencies = await context.PlayerTacticalRoleTendencyValues.AsNoTracking().ToListAsync(cancellationToken);
        var personnel = await context.Personnel.AsNoTracking().IgnoreAutoIncludes().ToListAsync(cancellationToken);
        var teams = await context.Teams.AsNoTracking().ToListAsync(cancellationToken);
        var teamActiveLineSlots = await context.TeamActiveLineSlots.AsNoTracking().IgnoreAutoIncludes().ToListAsync(cancellationToken);
        var gameSettings = await context.GameSettings.AsNoTracking().ToListAsync(cancellationToken);
        var storedLines = await context.StoredLines.AsNoTracking().ToListAsync(cancellationToken);
        var storedLineSlots = await context.StoredLineSlots.AsNoTracking().ToListAsync(cancellationToken);
        var tacticSystems = await context.TacticSystems.AsNoTracking().ToListAsync(cancellationToken);
        var teamTactics = await context.TeamTactics.AsNoTracking().ToListAsync(cancellationToken);
        var tacticFiles = await context.TacticFiles.AsNoTracking().ToListAsync(cancellationToken);
        var tacticTemplates = await context.TacticTemplates.AsNoTracking().ToListAsync(cancellationToken);
        var setPlays = await context.SetPlays.AsNoTracking().ToListAsync(cancellationToken);
        var modifierCatalogues = await context.ModifierCatalogues.AsNoTracking().ToListAsync(cancellationToken);
        var tactics = await context.Tactics.AsNoTracking().ToListAsync(cancellationToken);

        var baseline = files.ToDictionary(file => file.RelativePath, StringComparer.OrdinalIgnoreCase);
        var output = CreateRawSave(files);
        var nameIds = new HashSet<int>();
        var playerIds = new HashSet<int>();
        var playerRoleIds = new HashSet<InGameRole>();

        var namesFile = GetDocumented<FhmNamesFile>(baseline, "names.dat");
        if (namesFile is not null)
        {
            if (ApplyNames(namesFile, names, nameListEntries, nameScalars, nameIds))
            {
                SetDocumented(output, namesFile);
            }
        }
        else
        {
            RequireEmpty(names, nameof(Name));
            RequireEmpty(nameListEntries, nameof(NameListEntry));
            RequireEmpty(nameScalars, nameof(NameScalar));
        }

        var playerRolesFile = GetDocumented<FhmPlayerRolesFile>(baseline, "player_roles.dat");
        if (playerRolesFile is not null)
        {
            if (ApplyPlayerRoles(
                playerRolesFile,
                playerRoleCatalogues,
                playerRoles,
                playerRoleWeights,
                playerRoleIndexEntries,
                playerRoleIds))
            {
                SetDocumented(output, playerRolesFile);
            }
        }
        else
        {
            RequireEmpty(playerRoleCatalogues, nameof(PlayerRoleCatalogue));
            RequireEmpty(playerRoles, nameof(PlayerRoleDefinition));
            RequireEmpty(playerRoleWeights, nameof(PlayerRoleWeight));
            RequireEmpty(playerRoleIndexEntries, nameof(PlayerRoleIndexEntry));
        }

        var playersFile = GetDocumented<FhmPlayersFile>(baseline, "players.dat");
        if (playersFile is not null)
        {
            if (ApplyPlayers(
                playersFile,
                players,
                attributes,
                contracts,
                contractYears,
                playerRoleAssignments,
                playerRoleTendencies,
                teams,
                playerRoleIds,
                playerIds))
            {
                SetDocumented(output, playersFile);
            }
        }
        else
        {
            RequireEmpty(players, nameof(Player));
            RequireEmpty(attributes, nameof(PlayerAttributes));
            RequireEmpty(contracts, nameof(PlayerContract));
            RequireEmpty(contractYears, nameof(PlayerContractYear));
            RequireEmpty(playerRoleAssignments, nameof(PlayerRoleAssignment));
            RequireEmpty(playerRoleTendencies, nameof(PlayerRoleTendencyValue));
        }

        ValidatePlayerReferences(playersFile, nameIds);

        var personnelFile = GetDocumented<FhmPersonnelFile>(baseline, "personal.dat");
        if (personnelFile is not null)
        {
            if (ApplyPersonnel(personnelFile, personnel, teams))
            {
                SetDocumented(output, personnelFile);
            }
        }
        else
        {
            RequireEmpty(personnel, nameof(Personnel));
        }

        var teamsFile = GetDocumented<FhmTeamsFile>(baseline, "teams.dat");
        if (teamsFile is not null)
        {
            if (ApplyTeams(teamsFile, teams, teamTactics, teamActiveLineSlots, playerIds))
            {
                SetDocumented(output, teamsFile);
            }
        }
        else
        {
            RequireEmpty(teams, nameof(Team));
            RequireEmpty(teamTactics, nameof(TeamTactic));
            RequireEmpty(teamActiveLineSlots, nameof(TeamActiveLineSlot));
        }

        var settingsFile = GetDocumented<FhmGameSettingsFile>(baseline, "game_settings.dat");
        if (settingsFile is not null)
        {
            if (ApplyGameSettings(settingsFile, gameSettings))
            {
                SetDocumented(output, settingsFile);
            }
        }
        else
        {
            RequireEmpty(gameSettings, nameof(GameSetting));
        }

        var storedLinesFile = GetDocumented<FhmStoredLinesFile>(baseline, "stored_lines.dat");
        if (storedLinesFile is not null)
        {
            if (ApplyStoredLines(storedLinesFile, storedLines, storedLineSlots, playerIds))
            {
                SetDocumented(output, storedLinesFile);
            }
        }
        else
        {
            RequireEmpty(storedLines, nameof(StoredLine));
            RequireEmpty(storedLineSlots, nameof(StoredLineSlot));
        }

        var expectedTacticFiles = new Dictionary<string, TacticFileExpectation>(StringComparer.OrdinalIgnoreCase);
        var tacticSystemFile = GetDocumented<FhmTeamTacticsFile>(baseline, "team_tactics.dat");
        if (tacticSystemFile is not null)
        {
            expectedTacticFiles.Add(tacticSystemFile.RelativePath, new(TacticFileKind.TacticSystems, tacticSystemFile.VersionTag, tacticSystemFile.Records.Count));
            if (ApplyTacticSystems(tacticSystemFile, tacticSystems))
            {
                SetDocumented(output, tacticSystemFile);
            }
        }
        else
        {
            RequireEmpty(tacticSystems, nameof(TacticSystem));
        }

        var templatesFile = GetDocumented<FhmTacticTemplatesFile>(baseline, "tactic_templates.dat");
        if (templatesFile is not null)
        {
            expectedTacticFiles.Add(templatesFile.RelativePath, new(TacticFileKind.TacticTemplates, templatesFile.Version, templatesFile.Templates.Count));
            if (ApplyTacticTemplates(templatesFile, tacticTemplates))
            {
                SetDocumented(output, templatesFile);
            }
        }
        else
        {
            RequireEmpty(tacticTemplates, nameof(TacticTemplate));
        }

        var setPlayFiles = GetDocuments<FhmSetPlayFile>(baseline).ToList();
        foreach (var file in setPlayFiles)
        {
            expectedTacticFiles.Add(file.RelativePath, new(TacticFileKind.SetPlay, file.Version, file.Formations.Count));
            if (ApplySetPlay(file, setPlays))
            {
                SetDocumented(output, file);
            }
        }

        var modifierCatalogueFiles = GetDocuments<FhmLengthPrefixedCatalogueFile>(baseline).ToList();
        foreach (var file in modifierCatalogueFiles)
        {
            expectedTacticFiles.Add(file.RelativePath, new(TacticFileKind.ModifierCatalogue, file.Version, file.Blocks.Count));
            if (ApplyModifierCatalogue(file, modifierCatalogues))
            {
                SetDocumented(output, file);
            }
        }

        var zoneEventsFile = GetDocumented<FhmZoneEventModifiersFile>(baseline, "zone_event_mod.dat");
        if (zoneEventsFile is not null)
        {
            expectedTacticFiles.Add(zoneEventsFile.RelativePath, new(TacticFileKind.ZoneEventModifiers, zoneEventsFile.Version, zoneEventsFile.ZoneCount));
            if (ApplyZoneEventModifiers(zoneEventsFile, modifierCatalogues))
            {
                SetDocumented(output, zoneEventsFile);
            }
        }

        var tacticsFile = GetDocumented<FhmTacticsFile>(baseline, "tactics.dat");
        if (tacticsFile is not null)
        {
            if (ApplyTactics(tacticsFile, tactics))
            {
                SetDocumented(output, tacticsFile);
            }
        }
        else
        {
            RequireEmpty(tactics, nameof(Tactics));
        }

        var headerChangedPaths = ValidateAndApplyTacticHeaders(
            expectedTacticFiles,
            tacticFiles,
            tacticSystemFile,
            templatesFile,
            setPlayFiles,
            modifierCatalogueFiles,
            zoneEventsFile);

        ValidateNoUnexpectedSetPlays(setPlays, expectedTacticFiles);
        ValidateNoUnexpectedModifiers(modifierCatalogues, expectedTacticFiles);
        foreach (var changedPath in headerChangedPaths)
        {
            if (tacticSystemFile is not null &&
                string.Equals(changedPath, tacticSystemFile.RelativePath, StringComparison.OrdinalIgnoreCase))
            {
                SetDocumented(output, tacticSystemFile);
            }
            else if (templatesFile is not null &&
                string.Equals(changedPath, templatesFile.RelativePath, StringComparison.OrdinalIgnoreCase))
            {
                SetDocumented(output, templatesFile);
            }
            else if (setPlayFiles.SingleOrDefault(file => string.Equals(changedPath, file.RelativePath, StringComparison.OrdinalIgnoreCase)) is { } setPlay)
            {
                SetDocumented(output, setPlay);
            }
            else if (modifierCatalogueFiles.SingleOrDefault(file => string.Equals(changedPath, file.RelativePath, StringComparison.OrdinalIgnoreCase)) is { } catalogue)
            {
                SetDocumented(output, catalogue);
            }
            else if (zoneEventsFile is not null &&
                string.Equals(changedPath, zoneEventsFile.RelativePath, StringComparison.OrdinalIgnoreCase))
            {
                SetDocumented(output, zoneEventsFile);
            }
        }

        return output;
    }

    /// <summary>
    /// Exports a SQLite adapter database directly to a new or empty FHM save folder without
    /// materializing a complete <see cref="FhmSave"/>.
    /// </summary>
    public Task ExportAsync(
        string databasePath,
        string destinationDirectory,
        CancellationToken cancellationToken = default) =>
        ExportAsync(
            databasePath,
            destinationDirectory,
            new FhmSaveSqliteExportOptions(),
            cancellationToken);

    /// <summary>
    /// Exports a SQLite adapter database directly to a new or empty FHM save folder using
    /// explicit progress options, without materializing a complete <see cref="FhmSave"/>.
    /// </summary>
    public async Task ExportAsync(
        string databasePath,
        string destinationDirectory,
        FhmSaveSqliteExportOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationDirectory);
        ArgumentNullException.ThrowIfNull(options);

        var fullDatabasePath = Path.GetFullPath(databasePath);
        if (!File.Exists(fullDatabasePath))
        {
            throw new FileNotFoundException(
                $"SQLite save database '{fullDatabasePath}' does not exist.",
                fullDatabasePath);
        }

        var fullDestinationDirectory = Path.GetFullPath(destinationDirectory);
        if (Directory.Exists(fullDestinationDirectory) &&
            Directory.EnumerateFileSystemEntries(fullDestinationDirectory).Any())
        {
            throw new IOException($"FHM save destination '{fullDestinationDirectory}' must be empty.");
        }

        var elapsed = System.Diagnostics.Stopwatch.StartNew();
        await using var context = await FhmSaveSqliteContext.OpenAsync(fullDatabasePath, cancellationToken);
        await context.Database.OpenConnectionAsync(cancellationToken);
        await FhmSaveSqliteContext.ConfigureReadConnectionAsync(context, cancellationToken);
        var manifest = await context.Manifests.AsNoTracking().SingleOrDefaultAsync(cancellationToken)
            ?? throw new InvalidDataException("SQLite save database does not contain a manifest.");
        ValidateManifest(manifest);
        var baselineFiles = await ReadAndValidateBaselineFilesAsync(context, manifest, cancellationToken)
            .ConfigureAwait(false);
        var baselineByPath = baselineFiles.ToDictionary(
            file => file.RelativePath,
            StringComparer.OrdinalIgnoreCase);
        ReportExportElapsed(options, "SQLite validation", elapsed);

        Directory.CreateDirectory(fullDestinationDirectory);
        await WriteBaselineFilesAsync(context, baselineFiles, fullDestinationDirectory, cancellationToken)
            .ConfigureAwait(false);
        ReportExportElapsed(options, "Baseline streaming to save folder", elapsed);

        await ReconstructDocumentedFilesAsync(
            context,
            baselineByPath,
            fullDestinationDirectory,
            cancellationToken).ConfigureAwait(false);
        ReportExportElapsed(options, "Documented-file reconstruction and writing", elapsed);
    }

    private static void ValidateManifest(SaveManifest manifest)
    {
        if (manifest.Id != 1 || manifest.SchemaVersion != FhmSaveSqliteWriter.SchemaVersion ||
            manifest.SourceFormatVersion != "FHM 10 save folder")
        {
            throw new InvalidDataException(
                $"SQLite save database has unsupported manifest schema {manifest.SchemaVersion} or source format '{manifest.SourceFormatVersion}'.");
        }
    }

    private static async Task<List<BaselineFile>> ReadAndValidateBaselineFilesAsync(
        FhmSaveSqliteContext context,
        SaveManifest manifest,
        CancellationToken cancellationToken)
    {
        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var files = new List<BaselineFile>();
        await foreach (var file in context.Files
            .AsNoTracking()
            .Select(value => new BaselineFileProjection(
                value.RelativePath,
                value.Kind,
                value.Content == null ? -1 : value.Content.Length))
            .AsAsyncEnumerable()
            .WithCancellation(cancellationToken))
        {
            var normalized = FhmSaveFileFactory.NormalizeRelativePath(file.RelativePath);
            if (!string.Equals(normalized, file.RelativePath, StringComparison.Ordinal) ||
                !paths.Add(normalized) ||
                file.ContentLength < 0)
            {
                throw new InvalidDataException($"Invalid or duplicate baseline save-file path '{file.RelativePath}'.");
            }

            if (file.Kind is not SaveFileKind.Documented and not SaveFileKind.Opaque)
            {
                throw new InvalidDataException($"Unknown source-file kind '{file.Kind}'.");
            }

            files.Add(new BaselineFile(file.RelativePath, file.Kind, file.ContentLength));
        }

        if (manifest.SourceFileCount != files.Count)
        {
            throw new InvalidDataException(
                $"Manifest declares {manifest.SourceFileCount} source files but the database contains {files.Count}.");
        }

        var filesByPath = files.ToDictionary(file => file.RelativePath, StringComparer.OrdinalIgnoreCase);
        string? chunkPath = null;
        var expectedOrdinal = 0;
        await foreach (var chunk in context.FileChunks
            .AsNoTracking()
            .OrderBy(value => value.RelativePath)
            .ThenBy(value => value.Ordinal)
            .Select(value => new SaveFileChunkProjection(
                value.RelativePath,
                value.Ordinal,
                value.Content == null ? -1 : value.Content.Length))
            .AsAsyncEnumerable()
            .WithCancellation(cancellationToken))
        {
            if (!filesByPath.TryGetValue(chunk.RelativePath, out var file))
            {
                throw new InvalidDataException("Baseline content chunks reference missing source files.");
            }

            if (!string.Equals(chunkPath, chunk.RelativePath, StringComparison.OrdinalIgnoreCase))
            {
                chunkPath = chunk.RelativePath;
                expectedOrdinal = 0;
            }

            if (file.ContentLength != 0)
            {
                throw new InvalidDataException($"Chunked baseline save file '{file.RelativePath}' also has inline content.");
            }

            if (chunk.ContentLength < 0 || chunk.Ordinal != expectedOrdinal)
            {
                throw new InvalidDataException($"Chunked baseline save file '{file.RelativePath}' has non-contiguous chunk ordinals.");
            }

            expectedOrdinal++;
            file.ChunkCount++;
        }

        return files;
    }

    private static async Task WriteBaselineFilesAsync(
        FhmSaveSqliteContext context,
        IEnumerable<BaselineFile> files,
        string destinationDirectory,
        CancellationToken cancellationToken)
    {
        foreach (var file in files.Where(value => value.Kind == SaveFileKind.Documented))
        {
            await WriteBaselineFileAsync(context, file, destinationDirectory, cancellationToken)
                .ConfigureAwait(false);
        }

        foreach (var file in files.Where(value => value.Kind == SaveFileKind.Opaque))
        {
            await WriteBaselineFileAsync(context, file, destinationDirectory, cancellationToken)
                .ConfigureAwait(false);
        }
    }

    private static async Task WriteBaselineFileAsync(
        FhmSaveSqliteContext context,
        BaselineFile file,
        string destinationDirectory,
        CancellationToken cancellationToken)
    {
        if (FhmSaveAuxiliaryPaths.IsIgnored(file.RelativePath))
        {
            return;
        }

        var destinationPath = FhmSaveFileFactory.ResolvePath(destinationDirectory, file.RelativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
        await using var output = new FileStream(
            destinationPath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 1024 * 1024,
            FileOptions.Asynchronous | FileOptions.SequentialScan);

        if (file.ChunkCount == 0)
        {
            var content = await context.Files
                .AsNoTracking()
                .Where(value => value.RelativePath == file.RelativePath)
                .Select(value => value.Content)
                .SingleAsync(cancellationToken)
                .ConfigureAwait(false);
            if (content.Length != file.ContentLength)
            {
                throw new InvalidDataException($"Baseline save file '{file.RelativePath}' changed during export.");
            }

            await output.WriteAsync(content, cancellationToken).ConfigureAwait(false);
            return;
        }

        var expectedOrdinal = 0;
        await foreach (var chunk in context.FileChunks
            .AsNoTracking()
            .Where(value => value.RelativePath == file.RelativePath)
            .OrderBy(value => value.Ordinal)
            .Select(value => new SaveFileChunkContent(value.Ordinal, value.Content))
            .AsAsyncEnumerable()
            .WithCancellation(cancellationToken))
        {
            if (chunk.Ordinal != expectedOrdinal++)
            {
                throw new InvalidDataException($"Chunked baseline save file '{file.RelativePath}' changed during export.");
            }

            await output.WriteAsync(chunk.Content, cancellationToken).ConfigureAwait(false);
        }

        if (expectedOrdinal != file.ChunkCount)
        {
            throw new InvalidDataException($"Chunked baseline save file '{file.RelativePath}' changed during export.");
        }
    }

    private static async Task ReconstructDocumentedFilesAsync(
        FhmSaveSqliteContext context,
        IReadOnlyDictionary<string, BaselineFile> baseline,
        string destinationDirectory,
        CancellationToken cancellationToken)
    {
        var nameIds = await ReconstructNamesAsync(context, baseline, destinationDirectory, cancellationToken)
            .ConfigureAwait(false);
        var playerRoleIds = await ReconstructPlayerRolesAsync(context, baseline, destinationDirectory, cancellationToken)
            .ConfigureAwait(false);
        var playerIds = await ReconstructPlayersAsync(
            context,
            baseline,
            destinationDirectory,
            playerRoleIds,
            nameIds,
            cancellationToken).ConfigureAwait(false);
        await ReconstructPersonnelAndTeamsAsync(
            context,
            baseline,
            destinationDirectory,
            nameIds,
            playerIds,
            cancellationToken).ConfigureAwait(false);
        await ReconstructGameSettingsAsync(context, baseline, destinationDirectory, cancellationToken)
            .ConfigureAwait(false);
        await ReconstructStoredLinesAsync(context, baseline, destinationDirectory, playerIds, cancellationToken)
            .ConfigureAwait(false);
        await ReconstructTacticFilesAsync(context, baseline, destinationDirectory, cancellationToken)
            .ConfigureAwait(false);
    }

    private static async Task<HashSet<int>> ReconstructNamesAsync(
        FhmSaveSqliteContext context,
        IReadOnlyDictionary<string, BaselineFile> baseline,
        string destinationDirectory,
        CancellationToken cancellationToken)
    {
        var names = await context.Names.AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false);
        var listEntries = await context.NameListEntries.AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false);
        var scalars = await context.NameScalars.AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false);
        var nameIds = new HashSet<int>();
        var file = GetExportedDocumented<FhmNamesFile>(baseline, destinationDirectory, "names.dat");
        if (file is not null)
        {
            WriteIfChanged(
                destinationDirectory,
                file,
                ApplyNames(file, names, listEntries, scalars, nameIds));
        }
        else
        {
            RequireEmpty(names, nameof(Name));
            RequireEmpty(listEntries, nameof(NameListEntry));
            RequireEmpty(scalars, nameof(NameScalar));
        }

        return nameIds;
    }

    private static async Task<HashSet<InGameRole>> ReconstructPlayerRolesAsync(
        FhmSaveSqliteContext context,
        IReadOnlyDictionary<string, BaselineFile> baseline,
        string destinationDirectory,
        CancellationToken cancellationToken)
    {
        var catalogues = await context.TacticalRoleCatalogues.AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false);
        var roles = await context.TacticalRoles.AsNoTracking().IgnoreAutoIncludes().ToListAsync(cancellationToken).ConfigureAwait(false);
        var weights = await context.TacticalRoleWeights.AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false);
        var indexEntries = await context.TacticalRoleIndexEntries.AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false);
        var roleIds = new HashSet<InGameRole>();
        var file = GetExportedDocumented<FhmPlayerRolesFile>(baseline, destinationDirectory, "player_roles.dat");
        if (file is not null)
        {
            WriteIfChanged(
                destinationDirectory,
                file,
                ApplyPlayerRoles(file, catalogues, roles, weights, indexEntries, roleIds));
        }
        else
        {
            RequireEmpty(catalogues, nameof(PlayerRoleCatalogue));
            RequireEmpty(roles, nameof(PlayerRoleDefinition));
            RequireEmpty(weights, nameof(PlayerRoleWeight));
            RequireEmpty(indexEntries, nameof(PlayerRoleIndexEntry));
        }

        return roleIds;
    }

    private static async Task<HashSet<int>> ReconstructPlayersAsync(
        FhmSaveSqliteContext context,
        IReadOnlyDictionary<string, BaselineFile> baseline,
        string destinationDirectory,
        ISet<InGameRole> playerRoleIds,
        ISet<int> nameIds,
        CancellationToken cancellationToken)
    {
        var players = await context.Players.AsNoTracking().IgnoreAutoIncludes().ToListAsync(cancellationToken).ConfigureAwait(false);
        var attributes = await context.PlayerAttributes.AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false);
        var contracts = await context.PlayerContracts.AsNoTracking().IgnoreAutoIncludes().ToListAsync(cancellationToken).ConfigureAwait(false);
        var contractYears = await context.PlayerContractYears.AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false);
        var assignments = await context.PlayerTacticalRoleAssignments.AsNoTracking().IgnoreAutoIncludes().ToListAsync(cancellationToken).ConfigureAwait(false);
        var tendencies = await context.PlayerTacticalRoleTendencyValues.AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false);
        var teams = await context.Teams.AsNoTracking().IgnoreAutoIncludes().ToListAsync(cancellationToken).ConfigureAwait(false);
        var playerIds = new HashSet<int>();
        var file = GetExportedDocumented<FhmPlayersFile>(baseline, destinationDirectory, "players.dat");
        if (file is not null)
        {
            WriteIfChanged(
                destinationDirectory,
                file,
                ApplyPlayers(
                    file,
                    players,
                    attributes,
                    contracts,
                    contractYears,
                    assignments,
                    tendencies,
                    teams,
                    playerRoleIds,
                    playerIds));
        }
        else
        {
            RequireEmpty(players, nameof(Player));
            RequireEmpty(attributes, nameof(PlayerAttributes));
            RequireEmpty(contracts, nameof(PlayerContract));
            RequireEmpty(contractYears, nameof(PlayerContractYear));
            RequireEmpty(assignments, nameof(PlayerRoleAssignment));
            RequireEmpty(tendencies, nameof(PlayerRoleTendencyValue));
        }

        ValidatePlayerReferences(file, nameIds);
        return playerIds;
    }

    private static async Task ReconstructPersonnelAndTeamsAsync(
        FhmSaveSqliteContext context,
        IReadOnlyDictionary<string, BaselineFile> baseline,
        string destinationDirectory,
        ISet<int> nameIds,
        ISet<int> playerIds,
        CancellationToken cancellationToken)
    {
        var personnel = await context.Personnel.AsNoTracking().IgnoreAutoIncludes().ToListAsync(cancellationToken).ConfigureAwait(false);
        var teams = await context.Teams.AsNoTracking().IgnoreAutoIncludes().ToListAsync(cancellationToken).ConfigureAwait(false);
        var personnelFile = GetExportedDocumented<FhmPersonnelFile>(baseline, destinationDirectory, "personal.dat");
        if (personnelFile is not null)
        {
            WriteIfChanged(destinationDirectory, personnelFile, ApplyPersonnel(personnelFile, personnel, teams));
        }
        else
        {
            RequireEmpty(personnel, nameof(Personnel));
        }

        var teamTactics = await context.TeamTactics.AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false);
        var activeLineSlots = await context.TeamActiveLineSlots.AsNoTracking().IgnoreAutoIncludes().ToListAsync(cancellationToken).ConfigureAwait(false);
        var teamsFile = GetExportedDocumented<FhmTeamsFile>(baseline, destinationDirectory, "teams.dat");
        if (teamsFile is not null)
        {
            WriteIfChanged(
                destinationDirectory,
                teamsFile,
                ApplyTeams(teamsFile, teams, teamTactics, activeLineSlots, playerIds));
        }
        else
        {
            RequireEmpty(teams, nameof(Team));
            RequireEmpty(teamTactics, nameof(TeamTactic));
            RequireEmpty(activeLineSlots, nameof(TeamActiveLineSlot));
        }
    }

    private static async Task ReconstructGameSettingsAsync(
        FhmSaveSqliteContext context,
        IReadOnlyDictionary<string, BaselineFile> baseline,
        string destinationDirectory,
        CancellationToken cancellationToken)
    {
        var settings = await context.GameSettings.AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false);
        var file = GetExportedDocumented<FhmGameSettingsFile>(baseline, destinationDirectory, "game_settings.dat");
        if (file is not null)
        {
            WriteIfChanged(destinationDirectory, file, ApplyGameSettings(file, settings));
        }
        else
        {
            RequireEmpty(settings, nameof(GameSetting));
        }
    }

    private static async Task ReconstructStoredLinesAsync(
        FhmSaveSqliteContext context,
        IReadOnlyDictionary<string, BaselineFile> baseline,
        string destinationDirectory,
        ISet<int> playerIds,
        CancellationToken cancellationToken)
    {
        var lines = await context.StoredLines.AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false);
        var slots = await context.StoredLineSlots.AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false);
        var file = GetExportedDocumented<FhmStoredLinesFile>(baseline, destinationDirectory, "stored_lines.dat");
        if (file is not null)
        {
            WriteIfChanged(destinationDirectory, file, ApplyStoredLines(file, lines, slots, playerIds));
        }
        else
        {
            RequireEmpty(lines, nameof(StoredLine));
            RequireEmpty(slots, nameof(StoredLineSlot));
        }
    }

    private static async Task ReconstructTacticFilesAsync(
        FhmSaveSqliteContext context,
        IReadOnlyDictionary<string, BaselineFile> baseline,
        string destinationDirectory,
        CancellationToken cancellationToken)
    {
        var systems = await context.TacticSystems.AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false);
        var templates = await context.TacticTemplates.AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false);
        var setPlays = await context.SetPlays.AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false);
        var modifiers = await context.ModifierCatalogues.AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false);
        var tactics = await context.Tactics.AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false);
        var tacticFiles = await context.TacticFiles.AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false);

        var expectedFiles = new Dictionary<string, TacticFileExpectation>(StringComparer.OrdinalIgnoreCase);
        var systemsFile = GetExportedDocumented<FhmTeamTacticsFile>(baseline, destinationDirectory, "team_tactics.dat");
        if (systemsFile is not null)
        {
            expectedFiles.Add(systemsFile.RelativePath, new(TacticFileKind.TacticSystems, systemsFile.VersionTag, systemsFile.Records.Count));
            WriteIfChanged(destinationDirectory, systemsFile, ApplyTacticSystems(systemsFile, systems));
        }
        else
        {
            RequireEmpty(systems, nameof(TacticSystem));
        }

        var templatesFile = GetExportedDocumented<FhmTacticTemplatesFile>(baseline, destinationDirectory, "tactic_templates.dat");
        if (templatesFile is not null)
        {
            expectedFiles.Add(templatesFile.RelativePath, new(TacticFileKind.TacticTemplates, templatesFile.Version, templatesFile.Templates.Count));
            WriteIfChanged(destinationDirectory, templatesFile, ApplyTacticTemplates(templatesFile, templates));
        }
        else
        {
            RequireEmpty(templates, nameof(TacticTemplate));
        }

        var setPlayFiles = GetExportedDocuments<FhmSetPlayFile>(
            baseline,
            destinationDirectory,
            static path => path.StartsWith("set_play_", StringComparison.OrdinalIgnoreCase) &&
                path.EndsWith(".dat", StringComparison.OrdinalIgnoreCase)).ToList();
        foreach (var file in setPlayFiles)
        {
            expectedFiles.Add(file.RelativePath, new(TacticFileKind.SetPlay, file.Version, file.Formations.Count));
            WriteIfChanged(destinationDirectory, file, ApplySetPlay(file, setPlays));
        }

        var modifierFiles = GetExportedDocuments<FhmLengthPrefixedCatalogueFile>(
            baseline,
            destinationDirectory,
            static path => path.Equals("shot_type_mod.dat", StringComparison.OrdinalIgnoreCase) ||
                path.Equals("tactical_settings_mod.dat", StringComparison.OrdinalIgnoreCase)).ToList();
        foreach (var file in modifierFiles)
        {
            expectedFiles.Add(file.RelativePath, new(TacticFileKind.ModifierCatalogue, file.Version, file.Blocks.Count));
            WriteIfChanged(destinationDirectory, file, ApplyModifierCatalogue(file, modifiers));
        }

        var zoneEventsFile = GetExportedDocumented<FhmZoneEventModifiersFile>(baseline, destinationDirectory, "zone_event_mod.dat");
        if (zoneEventsFile is not null)
        {
            expectedFiles.Add(zoneEventsFile.RelativePath, new(TacticFileKind.ZoneEventModifiers, zoneEventsFile.Version, zoneEventsFile.ZoneCount));
            WriteIfChanged(destinationDirectory, zoneEventsFile, ApplyZoneEventModifiers(zoneEventsFile, modifiers));
        }

        var tacticsFile = GetExportedDocumented<FhmTacticsFile>(baseline, destinationDirectory, "tactics.dat");
        if (tacticsFile is not null)
        {
            WriteIfChanged(destinationDirectory, tacticsFile, ApplyTactics(tacticsFile, tactics));
        }
        else
        {
            RequireEmpty(tactics, nameof(Tactics));
        }

        var headerChangedPaths = ValidateAndApplyTacticHeaders(
            expectedFiles,
            tacticFiles,
            systemsFile,
            templatesFile,
            setPlayFiles,
            modifierFiles,
            zoneEventsFile);
        ValidateNoUnexpectedSetPlays(setPlays, expectedFiles);
        ValidateNoUnexpectedModifiers(modifiers, expectedFiles);
        foreach (var changedPath in headerChangedPaths)
        {
            if (systemsFile is not null &&
                string.Equals(changedPath, systemsFile.RelativePath, StringComparison.OrdinalIgnoreCase))
            {
                WriteDocumentedFile(destinationDirectory, systemsFile);
            }
            else if (templatesFile is not null &&
                string.Equals(changedPath, templatesFile.RelativePath, StringComparison.OrdinalIgnoreCase))
            {
                WriteDocumentedFile(destinationDirectory, templatesFile);
            }
            else if (setPlayFiles.SingleOrDefault(file => string.Equals(changedPath, file.RelativePath, StringComparison.OrdinalIgnoreCase)) is { } setPlay)
            {
                WriteDocumentedFile(destinationDirectory, setPlay);
            }
            else if (modifierFiles.SingleOrDefault(file => string.Equals(changedPath, file.RelativePath, StringComparison.OrdinalIgnoreCase)) is { } modifier)
            {
                WriteDocumentedFile(destinationDirectory, modifier);
            }
            else if (zoneEventsFile is not null &&
                string.Equals(changedPath, zoneEventsFile.RelativePath, StringComparison.OrdinalIgnoreCase))
            {
                WriteDocumentedFile(destinationDirectory, zoneEventsFile);
            }
        }
    }

    private static T? GetExportedDocumented<T>(
        IReadOnlyDictionary<string, BaselineFile> files,
        string destinationDirectory,
        string path)
        where T : class, IFhmSaveFile
    {
        if (!files.TryGetValue(path, out var baseline))
        {
            return null;
        }

        if (baseline.Kind != SaveFileKind.Documented)
        {
            throw new InvalidDataException($"Projected file '{path}' is incorrectly classified as opaque.");
        }

        return ReadExportedDocument(baseline, destinationDirectory) as T
            ?? throw new InvalidDataException($"Projected file '{path}' cannot be parsed by its documented codec.");
    }

    private static IEnumerable<T> GetExportedDocuments<T>(
        IReadOnlyDictionary<string, BaselineFile> files,
        string destinationDirectory,
        Func<string, bool> isCandidate)
        where T : class, IFhmSaveFile
    {
        foreach (var file in files.Values)
        {
            if (file.Kind != SaveFileKind.Documented ||
                FhmSaveAuxiliaryPaths.IsIgnored(file.RelativePath) ||
                !isCandidate(file.RelativePath))
            {
                continue;
            }

            if (ReadExportedDocument(file, destinationDirectory) is T document)
            {
                yield return document;
            }
        }
    }

    private static IFhmSaveFile? ReadExportedDocument(BaselineFile file, string destinationDirectory)
    {
        var path = FhmSaveFileFactory.ResolvePath(destinationDirectory, file.RelativePath);
        using var input = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 1024 * 1024,
            FileOptions.SequentialScan);
        return FhmSaveFileFactory.TryRead(file.RelativePath, input);
    }

    private static void WriteIfChanged(string destinationDirectory, IFhmSaveFile file, bool changed)
    {
        if (changed)
        {
            WriteDocumentedFile(destinationDirectory, file);
        }
    }

    private static void WriteDocumentedFile(string destinationDirectory, IFhmSaveFile file)
    {
        var path = FhmSaveFileFactory.ResolvePath(destinationDirectory, file.RelativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        using var output = new FileStream(
            path,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 1024 * 1024,
            FileOptions.SequentialScan);
        file.WriteTo(output);
    }

    private static void ReportExportElapsed(
        FhmSaveSqliteExportOptions options,
        string phase,
        System.Diagnostics.Stopwatch elapsed)
    {
        options.Progress?.Report(new FhmSaveSqliteExportProgress(phase, elapsed.Elapsed));
        elapsed.Restart();
    }

    private static void ValidateBaselineFiles(IReadOnlyCollection<SaveFile> files, SaveManifest manifest)
    {
        if (manifest.SourceFileCount != files.Count)
        {
            throw new InvalidDataException(
                $"Manifest declares {manifest.SourceFileCount} source files but the database contains {files.Count}.");
        }

        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in files)
        {
            var normalized = FhmSaveFileFactory.NormalizeRelativePath(file.RelativePath);
            if (!string.Equals(normalized, file.RelativePath, StringComparison.Ordinal) ||
                !paths.Add(normalized) ||
                file.Content is null)
            {
                throw new InvalidDataException($"Invalid or duplicate baseline save-file path '{file.RelativePath}'.");
            }
        }
    }

    private static List<SaveFile> RehydrateFileContents(
        IReadOnlyCollection<SaveFile> files,
        IReadOnlyCollection<SaveFileChunk> chunks)
    {
        var chunksByPath = chunks.GroupBy(value => value.RelativePath, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, StringComparer.OrdinalIgnoreCase);
        var result = new List<SaveFile>(files.Count);

        foreach (var file in files)
        {
            if (!chunksByPath.Remove(file.RelativePath, out var fileChunks))
            {
                result.Add(file);
                continue;
            }

            if (file.Content.Length != 0)
            {
                throw new InvalidDataException($"Chunked baseline save file '{file.RelativePath}' also has inline content.");
            }

            var orderedChunks = fileChunks.OrderBy(value => value.Ordinal).ToArray();
            if (!orderedChunks.Select(value => value.Ordinal).SequenceEqual(Enumerable.Range(0, orderedChunks.Length)))
            {
                throw new InvalidDataException($"Chunked baseline save file '{file.RelativePath}' has non-contiguous chunk ordinals.");
            }

            var length = orderedChunks.Aggregate(
                0L,
                (total, chunk) => checked(total + chunk.Content.Length));
            if (length > int.MaxValue)
            {
                throw new InvalidDataException($"Chunked baseline save file '{file.RelativePath}' exceeds the supported size.");
            }

            var content = new byte[(int)length];
            var offset = 0;
            foreach (var chunk in orderedChunks)
            {
                Buffer.BlockCopy(chunk.Content, 0, content, offset, chunk.Content.Length);
                offset += chunk.Content.Length;
            }

            file.Content = content;
            result.Add(file);
        }

        if (chunksByPath.Count > 0)
        {
            throw new InvalidDataException("Baseline content chunks reference missing source files.");
        }

        return result;
    }

    private static FhmSave CreateRawSave(IEnumerable<SaveFile> files)
    {
        var result = new FhmSave();
        foreach (var file in files)
        {
            if (file.Kind == SaveFileKind.Documented)
            {
                result.Files.Add(file.RelativePath, FhmSaveFileFactory.CreateRaw(file.RelativePath, file.Content.ToArray()));
            }
            else if (file.Kind == SaveFileKind.Opaque)
            {
                result.OpaqueFiles.Add(new FhmOpaqueFile(file.RelativePath, file.Content.ToArray()));
            }
            else
            {
                throw new InvalidDataException($"Unknown source-file kind '{file.Kind}'.");
            }
        }

        return result;
    }

    private static T? GetDocumented<T>(IReadOnlyDictionary<string, SaveFile> files, string path)
        where T : class, IFhmSaveFile
    {
        if (!files.TryGetValue(path, out var baseline))
        {
            return null;
        }

        if (baseline.Kind != SaveFileKind.Documented)
        {
            throw new InvalidDataException($"Projected file '{path}' is incorrectly classified as opaque.");
        }

        return FhmSaveFileFactory.TryRead(baseline.RelativePath, baseline.Content) as T
            ?? throw new InvalidDataException($"Projected file '{path}' cannot be parsed by its documented codec.");
    }

    private static IEnumerable<T> GetDocuments<T>(IReadOnlyDictionary<string, SaveFile> files)
        where T : class, IFhmSaveFile =>
        files.Values
            .Where(file => file.Kind == SaveFileKind.Documented)
            .Select(file => FhmSaveFileFactory.TryRead(file.RelativePath, file.Content))
            .OfType<T>();

    private static void SetDocumented(FhmSave save, IFhmSaveFile file) => save.Files[file.RelativePath] = file;

    private static bool ApplyNames(
        FhmNamesFile file,
        IReadOnlyCollection<Name> names,
        IReadOnlyCollection<NameListEntry> listEntries,
        IReadOnlyCollection<NameScalar> scalars,
        ISet<int> nameIds)
    {
        RequireExactKeys(
            names,
            Enumerable.Range(0, file.MasterNames.Count).ToHashSet(),
            value => value.Ordinal,
            nameof(Name));
        var nameRows = names.ToDictionary(value => value.Ordinal);
        var changed = false;

        foreach (var (entry, ordinal) in file.MasterNames.Select((value, index) => (value, index)).ToArray())
        {
            var row = nameRows[ordinal];
            ValidateMasterName(row, entry, ordinal);
            if (!nameIds.Add(row.NameId))
            {
                throw new InvalidDataException($"Names entity has duplicate master NameId {row.NameId}.");
            }

            var updated = new FhmNameEntry(
                row.Text,
                row.NameId,
                row.GroupId,
                checked((short)row.CategoryWeight),
                checked((byte)row.FlagA),
                checked((byte)row.FlagB),
                checked((byte)row.FlagC));
            changed |= updated != entry;
            file.MasterNames[ordinal] = updated;
        }

        changed |= ApplyNameLists(file.FirstNameLists, listEntries, NameListKind.FirstName, nameIds);
        changed |= ApplyNameLists(file.SurnameLists, listEntries, NameListKind.Surname, nameIds);
        changed |= ApplyNameScalars(file.ScalarArrayA, scalars, NameScalarKind.ScalarA);
        changed |= ApplyNameScalars(file.ScalarArrayB, scalars, NameScalarKind.ScalarB);
        return changed;
    }

    private static void ValidateMasterName(Name row, FhmNameEntry source, int ordinal)
    {
        if (row.NameId != source.NameId ||
            row.CategoryWeight is < short.MinValue or > short.MaxValue ||
            row.FlagA is < byte.MinValue or > byte.MaxValue ||
            row.FlagB is < byte.MinValue or > byte.MaxValue ||
            row.FlagC is < byte.MinValue or > byte.MaxValue)
        {
            throw new InvalidDataException($"Names entity Master/{ordinal} has an invalid immutable identity or raw value.");
        }
    }

    private static bool ApplyNameLists(
        IList<IList<int>> destination,
        IReadOnlyCollection<NameListEntry> entities,
        NameListKind kind,
        ISet<int> nameIds)
    {
        var rowsForKind = entities.Where(value => value.Kind == kind).ToArray();
        var expectedKeys = new HashSet<(int, int)>();
        foreach (var (list, nation) in destination.Select((value, index) => (value, index)))
        {
            foreach (var ordinal in Enumerable.Range(0, list.Count))
            {
                expectedKeys.Add((nation, ordinal));
            }
        }
        RequireExactKeys(rowsForKind, expectedKeys, value => (value.NationIndex, value.Ordinal), $"{kind} name-list entries");
        var rows = rowsForKind.ToDictionary(value => (value.NationIndex, value.Ordinal));
        var changed = false;
        foreach (var (list, nation) in destination.Select((value, index) => (value, index)).ToArray())
        {
            foreach (var ordinal in Enumerable.Range(0, list.Count))
            {
                var row = rows[(nation, ordinal)];
                if (row.NameId is { } nameId && !nameIds.Contains(nameId))
                {
                    throw new InvalidDataException($"Names entity {kind}/{nation}/{ordinal} references unknown NameId {nameId}.");
                }

                var value = row.NameId ?? FhmNullConstants.Null;
                changed |= list[ordinal] != value;
                list[ordinal] = value;
            }
        }

        return changed;
    }

    private static bool ApplyNameScalars(
        IList<int> destination,
        IReadOnlyCollection<NameScalar> entities,
        NameScalarKind kind)
    {
        var rowsForKind = entities.Where(value => value.Kind == kind).ToArray();
        RequireExactKeys(
            rowsForKind,
            Enumerable.Range(0, destination.Count).ToHashSet(),
            value => value.Ordinal,
            $"{kind} name scalars");
        var rows = rowsForKind.ToDictionary(value => value.Ordinal);
        var changed = false;
        foreach (var ordinal in Enumerable.Range(0, destination.Count))
        {
            var value = rows[ordinal].Value ?? FhmNullConstants.Null;
            changed |= destination[ordinal] != value;
            destination[ordinal] = value;
        }

        return changed;
    }

    private static bool ApplyPlayers(
        FhmPlayersFile file,
        IReadOnlyCollection<Player> entities,
        IReadOnlyCollection<PlayerAttributes> attributeEntities,
        IReadOnlyCollection<PlayerContract> contractEntities,
        IReadOnlyCollection<PlayerContractYear> contractYearEntities,
        IReadOnlyCollection<PlayerRoleAssignment> roleAssignments,
        IReadOnlyCollection<PlayerRoleTendencyValue> roleTendencies,
        IReadOnlyCollection<Team> teams,
        ISet<InGameRole> roleIds,
        ISet<int> playerIds)
    {
        RequireExactKeys(entities, Enumerable.Range(0, file.Players.Count).ToHashSet(), value => value.RecordOrdinal, nameof(Player));
        RequireExactKeys(
            attributeEntities,
            file.Players.Select(value => value.InternalIdentity).ToHashSet(),
            value => value.PlayerId,
            nameof(PlayerAttributes));
        var profiles = entities.ToDictionary(value => value.RecordOrdinal);
        var attributes = attributeEntities.ToDictionary(value => value.PlayerId);
        var contracts = contractEntities
            .GroupBy(value => value.PlayerInternalId)
            .ToDictionary(group => group.Key, group => group.OrderBy(value => value.ContractOrdinal).ToArray());
        var contractYears = contractYearEntities
            .GroupBy(value => (value.PlayerInternalId, value.ContractOrdinal))
            .ToDictionary(group => group.Key, group => group.OrderBy(value => value.YearNumber).ToArray());
        var assignments = roleAssignments.ToDictionary(value => (value.PlayerInternalId, value.Slot));
        var tendencies = roleTendencies
            .GroupBy(value => (value.PlayerInternalId, value.Slot))
            .ToDictionary(group => group.Key, group => group.ToArray());
        var teamRecordIndicesByOrdinal = teams.ToDictionary(value => value.RecordOrdinal, value => value.RecordIndex);
        var expectedPlayerIds = file.Players.Select(value => value.InternalIdentity).ToHashSet();
        if (roleAssignments.Any(value => !expectedPlayerIds.Contains(value.PlayerInternalId)))
        {
            throw new InvalidDataException("Player role assignments contain an unknown player identity.");
        }

        var changed = false;

        foreach (var (player, ordinal) in file.Players.Select((value, index) => (value, index)).ToArray())
        {
            var row = profiles[ordinal];
            var ratingRow = attributes[player.InternalIdentity];
            if (row.InternalId != player.InternalIdentity || !row.SerializedRecord.SequenceEqual(player.ToBytes()))
            {
                throw new InvalidDataException($"Players entity record {ordinal} changes its immutable identity or serialized backing.");
            }

            if (!playerIds.Add(row.InternalId))
            {
                throw new InvalidDataException($"Players entity has duplicate InternalId {row.InternalId}.");
            }

            ValidatePlayer(row, ordinal);
            var teamRecordIndex = ResolveTeamRecordIndex(
                row.TeamRecordOrdinal,
                teamRecordIndicesByOrdinal,
                $"player {row.InternalId}");
            changed |= ApplyPlayer(player, row, teamRecordIndex);
            changed |= ApplyAttributes(player.RatingAttributes, ratingRow, ordinal);
            changed |= ApplyContracts(
                player,
                contracts.GetValueOrDefault(player.InternalIdentity) ?? [],
                contractYears,
                ordinal);
            changed |= ApplyPlayerRoleAssignment(
                player,
                PlayerRoleSlot.Tactical,
                assignments.GetValueOrDefault((player.InternalIdentity, PlayerRoleSlot.Tactical)),
                tendencies,
                roleIds,
                ordinal);
            changed |= ApplyPlayerRoleAssignment(
                player,
                PlayerRoleSlot.SecondaryTactical,
                assignments.GetValueOrDefault((player.InternalIdentity, PlayerRoleSlot.SecondaryTactical)),
                tendencies,
                roleIds,
                ordinal);
        }

        return changed;
    }

    private static bool ApplyPlayerRoles(
        FhmPlayerRolesFile file,
        IReadOnlyCollection<PlayerRoleCatalogue> catalogues,
        IReadOnlyCollection<PlayerRoleDefinition> definitions,
        IReadOnlyCollection<PlayerRoleWeight> weights,
        IReadOnlyCollection<PlayerRoleIndexEntry> indexEntries,
        ISet<InGameRole> roleIds)
    {
        RequireExactKeys(catalogues, new HashSet<int> { 1 }, value => value.Id, nameof(PlayerRoleCatalogue));
        var catalogue = catalogues.Single();
        var orderedDefinitions = definitions.OrderBy(value => value.RecordOrdinal).ToArray();
        RequireExactKeys(
            orderedDefinitions,
            Enumerable.Range(0, orderedDefinitions.Length).ToHashSet(),
            value => value.RecordOrdinal,
            nameof(PlayerRoleDefinition));
        if (orderedDefinitions.Any(value => !roleIds.Add(value.RoleId)))
        {
            throw new InvalidDataException("Player role definitions contain duplicate role IDs.");
        }

        var weightsByRoleAndGroup = weights
            .GroupBy(value => (value.RoleId, value.Group))
            .ToDictionary(group => group.Key, group => group.OrderBy(value => value.Ordinal).ToArray());
        var indicesByRoleAndList = indexEntries
            .GroupBy(value => (value.RoleId, value.List))
            .ToDictionary(group => group.Key, group => group.OrderBy(value => value.Ordinal).ToArray());
        if (weights.Any(value => !roleIds.Contains(value.RoleId) || !Enum.IsDefined(value.Group))
            || indexEntries.Any(value => !roleIds.Contains(value.RoleId) || !Enum.IsDefined(value.List)))
        {
            throw new InvalidDataException("Player role vectors contain an unknown role or vector group.");
        }

        using var before = new MemoryStream();
        file.WriteTo(before);
        file.VersionTag = catalogue.VersionTag;
        file.Records.Clear();
        foreach (var row in orderedDefinitions)
        {
            ValidatePlayerRoleDefinition(row);
            var role = new FhmPlayerRoleDefinition
            {
                RoleId = (int)row.RoleId,
                Name = row.Name,
                AppliesToForwards = checked((byte)row.AppliesToForwards),
                AppliesToDefencemen = checked((byte)row.AppliesToDefencemen),
                AppliesToGoalies = checked((byte)row.AppliesToGoalies),
                RoleFlags = checked((byte)row.RoleFlags),
                PositionCategory = checked((ushort)row.PositionCategory),
                ShortName = row.ShortName,
                TuningValueA = checked((ushort)row.TuningValueA),
                TuningValueB = checked((ushort)row.TuningValueB),
                Description = row.Description,
                TuningValueC = checked((ushort)row.TuningValueC),
            };
            ApplyPlayerRoleWeights((InGameRole)role.RoleId, role.WeightGroupA, PlayerRoleWeightGroup.A, 8, weightsByRoleAndGroup);
            ApplyPlayerRoleWeights((InGameRole)role.RoleId, role.WeightGroupB, PlayerRoleWeightGroup.B, 13, weightsByRoleAndGroup);
            ApplyPlayerRoleWeights((InGameRole)role.RoleId, role.WeightGroupC, PlayerRoleWeightGroup.C, 17, weightsByRoleAndGroup);
            ApplyPlayerRoleWeights((InGameRole)role.RoleId, role.WeightGroupD, PlayerRoleWeightGroup.D, 4, weightsByRoleAndGroup);
            ApplyPlayerRoleWeights((InGameRole)role.RoleId, role.WeightGroupE, PlayerRoleWeightGroup.E, 19, weightsByRoleAndGroup);
            ApplyPlayerRoleWeights((InGameRole)role.RoleId, role.WeightGroupF, PlayerRoleWeightGroup.F, 9, weightsByRoleAndGroup);
            foreach (var list in Enum.GetValues<PlayerRoleIndexList>())
            {
                var rows = indicesByRoleAndList.GetValueOrDefault(((InGameRole)role.RoleId, list)) ?? [];
                RequireExactKeys(
                    rows,
                    Enumerable.Range(0, rows.Length).ToHashSet(),
                    value => value.Ordinal,
                    $"player role {role.RoleId} index list {list}");
                foreach (var value in rows)
                {
                    role.IndexLists[(int)list].Add(checked((byte)value.Value));
                }
            }

            file.Records.Add(role);
        }

        using var after = new MemoryStream();
        file.WriteTo(after);
        return !before.ToArray().SequenceEqual(after.ToArray());
    }

    private static void ValidatePlayerRoleDefinition(PlayerRoleDefinition row)
    {
        int[] byteValues =
        [
            row.AppliesToForwards,
            row.AppliesToDefencemen,
            row.AppliesToGoalies,
            row.RoleFlags,
        ];
        int[] ushortValues =
        [
            row.PositionCategory,
            row.TuningValueA,
            row.TuningValueB,
            row.TuningValueC,
        ];
        if (byteValues.Any(value => value is < byte.MinValue or > byte.MaxValue)
            || ushortValues.Any(value => value is < ushort.MinValue or > ushort.MaxValue))
        {
            throw new InvalidDataException($"Player role {row.RoleId} contains a value outside its serialized range.");
        }
    }

    private static void ApplyPlayerRoleWeights(
        InGameRole roleId,
        IList<int> destination,
        PlayerRoleWeightGroup group,
        int expectedCount,
        IReadOnlyDictionary<(InGameRole RoleId, PlayerRoleWeightGroup Group), PlayerRoleWeight[]> weights)
    {
        var rows = weights.GetValueOrDefault((roleId, group)) ?? [];
        RequireExactKeys(
            rows,
            Enumerable.Range(0, expectedCount).ToHashSet(),
            value => value.Ordinal,
            $"player role {roleId} weight group {group}");
        for (var ordinal = 0; ordinal < rows.Length; ordinal++)
        {
            destination[ordinal] = rows[ordinal].Value;
        }
    }

    private static bool ApplyPlayerRoleAssignment(
        FhmPlayerRecord player,
        PlayerRoleSlot slot,
        PlayerRoleAssignment? assignment,
        IReadOnlyDictionary<(int PlayerInternalId, PlayerRoleSlot Slot), PlayerRoleTendencyValue[]> tendencies,
        ISet<InGameRole> roleIds,
        int playerOrdinal)
    {
        var destination = slot == PlayerRoleSlot.Tactical ? player.TacticalRole : player.SecondaryTacticalRole;
        FhmPlayerRoleInstance? replacement = null;
        if (assignment is not null)
        {
            if (!Enum.IsDefined(assignment.Slot) || !roleIds.Contains(assignment.RoleId))
            {
                throw new InvalidDataException(
                    $"Players entity record {playerOrdinal} has an invalid {slot} role assignment.");
            }

            var rows = tendencies.GetValueOrDefault((player.InternalIdentity, slot)) ?? [];
            RequireExactKeys(
                rows,
                Enum.GetValues<PlayerRoleTendency>().ToHashSet(),
                value => value.Tendency,
                $"player record {playerOrdinal} {slot} role tendencies");
            replacement = new FhmPlayerRoleInstance { RoleId = (int)assignment.RoleId };
            foreach (var row in rows)
            {
                if (row.UseOverride is < byte.MinValue or > byte.MaxValue
                    || row.Value is < ushort.MinValue or > ushort.MaxValue)
                {
                    throw new InvalidDataException(
                        $"Players entity record {playerOrdinal} has a {slot} role tendency outside its serialized range.");
                }

                var ordinal = (int)row.Tendency;
                replacement.UseOverride[ordinal] = checked((byte)row.UseOverride);
                replacement.TendencyValue[ordinal] = checked((ushort)row.Value);
            }
        }
        else if (tendencies.ContainsKey((player.InternalIdentity, slot)))
        {
            throw new InvalidDataException(
                $"Players entity record {playerOrdinal} has {slot} tendency rows without a role assignment.");
        }

        var changed = !PlayerRoleInstancesEqual(destination, replacement);
        if (slot == PlayerRoleSlot.Tactical)
        {
            player.TacticalRole = replacement;
        }
        else
        {
            player.SecondaryTacticalRole = replacement;
        }

        return changed;
    }

    private static bool PlayerRoleInstancesEqual(FhmPlayerRoleInstance? left, FhmPlayerRoleInstance? right) =>
        left is null
            ? right is null
            : right is not null
                && left.RoleId == right.RoleId
                && left.UseOverride.SequenceEqual(right.UseOverride)
                && left.TendencyValue.SequenceEqual(right.TendencyValue);

    private static bool ApplyContracts(
        FhmPlayerRecord player,
        IReadOnlyList<PlayerContract> contracts,
        IReadOnlyDictionary<(int PlayerInternalId, int ContractOrdinal), PlayerContractYear[]> contractYears,
        int playerOrdinal)
    {
        if (contracts.Count != player.Contracts.Count)
        {
            throw new InvalidDataException(
                $"Players entity record {playerOrdinal} has {contracts.Count} contracts; expected {player.Contracts.Count}.");
        }

        var changed = false;
        for (var contractOrdinal = 0; contractOrdinal < contracts.Count; contractOrdinal++)
        {
            if (contracts[contractOrdinal].ContractOrdinal != contractOrdinal)
            {
                throw new InvalidDataException(
                    $"Players entity record {playerOrdinal} has contract ordinal {contracts[contractOrdinal].ContractOrdinal}; expected {contractOrdinal}.");
            }

            if (!contractYears.TryGetValue((player.InternalIdentity, contractOrdinal), out var years)
                || years.Length != FhmPlayerContract.MaximumYears)
            {
                throw new InvalidDataException(
                    $"Players entity record {playerOrdinal} contract {contractOrdinal} must have {FhmPlayerContract.MaximumYears} salary years.");
            }

            for (var yearOrdinal = 0; yearOrdinal < years.Length; yearOrdinal++)
            {
                var year = years[yearOrdinal];
                if (year.YearNumber != yearOrdinal + 1)
                {
                    throw new InvalidDataException(
                        $"Players entity record {playerOrdinal} contract {contractOrdinal} has year {year.YearNumber}; expected {yearOrdinal + 1}.");
                }

                var salary = player.Contracts[contractOrdinal].Salaries[yearOrdinal];
                changed |= salary.MajorLeagueSalary != year.MajorLeagueSalary
                    || salary.MinorLeagueSalary != year.MinorLeagueSalary;
                salary.MajorLeagueSalary = year.MajorLeagueSalary;
                salary.MinorLeagueSalary = year.MinorLeagueSalary;
            }
        }

        return changed;
    }

    private static void ValidatePlayer(Player row, int ordinal)
    {
        try
        {
            _ = row.BirthDate;
        }
        catch (ArgumentOutOfRangeException exception)
        {
            throw new InvalidDataException($"Players entity record {ordinal} has an invalid birth date.", exception);
        }

        var ratings = new[]
        {
            row.PositionAffinity.Goalie,
            row.PositionAffinity.LeftDefense,
            row.PositionAffinity.RightDefense,
            row.PositionAffinity.LeftWing,
            row.PositionAffinity.Center,
            row.PositionAffinity.RightWing,
        };
        if (ratings.Any(value => value is < 0 or > 20))
        {
            throw new InvalidDataException($"Players entity record {ordinal} has a position rating outside 0..20.");
        }
    }

    private static bool ApplyPlayer(FhmPlayerRecord destination, Player source, int teamRecordIndex)
    {
        var changed =
            destination.ExportedPlayerId != source.ExternalId ||
            destination.FirstNameId != ToWireReference(source.FirstNameId) ||
            destination.SurnameId != ToWireReference(source.SurnameId) ||
            destination.CommonNameId != ToWireReference(source.CommonNameId) ||
            destination.BirthDate != new FhmDate(source.BirthDate.Year, source.BirthDate.Month, source.BirthDate.Day) ||
            destination.TeamId != teamRecordIndex ||
            destination.FranchiseId != ToWireReference(source.FranchiseId) ||
            destination.PrimaryContractRole.RawValue != checked((ushort)source.PrimaryContractRole) ||
            destination.SupplementaryContractRole.RawValue != checked((ushort)source.SupplementaryContractRole) ||
            destination.PositionRatings.Goalie != source.PositionAffinity.Goalie ||
            destination.PositionRatings.LeftDefenceman != source.PositionAffinity.LeftDefense ||
            destination.PositionRatings.RightDefenceman != source.PositionAffinity.RightDefense ||
            destination.PositionRatings.LeftWing != source.PositionAffinity.LeftWing ||
            destination.PositionRatings.Centre != source.PositionAffinity.Center ||
            destination.PositionRatings.RightWing != source.PositionAffinity.RightWing;

        destination.ExportedPlayerId = source.ExternalId;
        destination.FirstNameId = ToWireReference(source.FirstNameId);
        destination.SurnameId = ToWireReference(source.SurnameId);
        destination.CommonNameId = ToWireReference(source.CommonNameId);
        destination.BirthDate = new(source.BirthDate.Year, source.BirthDate.Month, source.BirthDate.Day);
        destination.TeamId = teamRecordIndex;
        destination.FranchiseId = ToWireReference(source.FranchiseId);
        destination.PrimaryContractRole = new(checked((ushort)source.PrimaryContractRole));
        destination.SupplementaryContractRole = new(checked((ushort)source.SupplementaryContractRole));
        destination.PositionRatings.Goalie = checked((ushort)source.PositionAffinity.Goalie);
        destination.PositionRatings.LeftDefenceman = checked((ushort)source.PositionAffinity.LeftDefense);
        destination.PositionRatings.RightDefenceman = checked((ushort)source.PositionAffinity.RightDefense);
        destination.PositionRatings.LeftWing = checked((ushort)source.PositionAffinity.LeftWing);
        destination.PositionRatings.Centre = checked((ushort)source.PositionAffinity.Center);
        destination.PositionRatings.RightWing = checked((ushort)source.PositionAffinity.RightWing);
        return changed;
    }

    private static bool ApplyAttributes(FhmPlayerAttributes destination, PlayerAttributes source, int ordinal)
    {
        var changed = false;
        foreach (var entityProperty in typeof(PlayerAttributes).GetProperties())
        {
            if (entityProperty.Name == nameof(PlayerAttributes.PlayerId) ||
                entityProperty.PropertyType != typeof(int))
            {
                continue;
            }

            var value = (int)entityProperty.GetValue(source)!;
            if (value is < byte.MinValue or > byte.MaxValue)
            {
                throw new InvalidDataException($"Players entity record {ordinal} has rating {entityProperty.Name} outside 0..255.");
            }

            var destinationName = entityProperty.Name switch
            {
                nameof(PlayerAttributes.DevelopmentRate) => nameof(FhmPlayerAttributes.DevRate),
                nameof(PlayerAttributes.TeamPlayer) => nameof(FhmPlayerAttributes.Teamplayer),
                nameof(PlayerAttributes.Puckhandling) => nameof(FhmPlayerAttributes.PuckHandling),
                nameof(PlayerAttributes.GoaliePokeCheck) => nameof(FhmPlayerAttributes.GoaliePokecheck),
                nameof(PlayerAttributes.Reflexes) => nameof(FhmPlayerAttributes.GoalieReflexes),
                nameof(PlayerAttributes.Skating) => nameof(FhmPlayerAttributes.GoalieSkating),
                _ => entityProperty.Name,
            };
            var destinationProperty = typeof(FhmPlayerAttributes).GetProperty(destinationName)!;
            changed |= (byte)destinationProperty.GetValue(destination)! != value;
            destinationProperty.SetValue(destination, checked((byte)value));
        }

        return changed;
    }

    private static void ValidatePlayerReferences(FhmPlayersFile? players, ISet<int> nameIds)
    {
        if (players is null)
        {
            return;
        }

        foreach (var (player, ordinal) in players.Players.Select((value, index) => (value, index)).ToArray())
        {
            ValidateReference(player.FirstNameId, nameIds, $"player record {ordinal} FirstNameId");
            ValidateReference(player.SurnameId, nameIds, $"player record {ordinal} SurnameId");
            ValidateReference(player.CommonNameId, nameIds, $"player record {ordinal} CommonNameId");
        }
    }

    private static void ValidateReference(int reference, ISet<int> validIds, string description)
    {
        if (reference != FhmNullConstants.Null && !validIds.Contains(reference))
        {
            throw new InvalidDataException($"{description} references unknown identity {reference}.");
        }
    }

    private static int ToWireReference(int? reference) => reference ?? FhmNullConstants.Null;

    private static int ResolveTeamRecordIndex(
        int? teamRecordOrdinal,
        IReadOnlyDictionary<int, int> teamRecordIndicesByOrdinal,
        string owner)
    {
        if (teamRecordOrdinal is null)
        {
            return FhmNullConstants.Null;
        }

        if (!teamRecordIndicesByOrdinal.TryGetValue(teamRecordOrdinal.Value, out var recordIndex))
        {
            throw new InvalidDataException($"{owner} references missing team record ordinal {teamRecordOrdinal.Value}.");
        }

        return recordIndex;
    }

    private static bool ApplyTeams(
        FhmTeamsFile file,
        IReadOnlyCollection<Team> entities,
        IReadOnlyCollection<TeamTactic> tactics,
        IReadOnlyCollection<TeamActiveLineSlot> activeLineSlots,
        ISet<int> playerIds)
    {
        RequireExactKeys(entities, Enumerable.Range(0, file.Teams.Count).ToHashSet(), value => value.RecordOrdinal, nameof(Team));
        RequireExactKeys(
            tactics,
            Enumerable.Range(0, file.Teams.Count).ToHashSet(),
            value => value.TeamRecordOrdinal,
            nameof(TeamTactic));
        var rows = entities.ToDictionary(value => value.RecordOrdinal);
        var tacticRows = tactics.ToDictionary(value => value.TeamRecordOrdinal);
        var expectedActiveLineSlotKeys = GetTeamActiveLineSlotKeys(file);
        RequireExactKeys(
            activeLineSlots,
            expectedActiveLineSlotKeys,
            value => (value.TeamRecordOrdinal, value.Group, value.SlotOrdinal),
            nameof(TeamActiveLineSlot));
        var activeLineSlotRows = activeLineSlots.ToDictionary(
            value => (value.TeamRecordOrdinal, value.Group, value.SlotOrdinal));
        var teamRecordIndicesByOrdinal = entities.ToDictionary(value => value.RecordOrdinal, value => value.RecordIndex);
        var changed = false;

        foreach (var (team, ordinal) in file.Teams.Select((value, index) => (value, index)).ToArray())
        {
            var row = rows[ordinal];
            if (row.RecordIndex != team.RecordIndex || row.TeamId != team.TeamId)
            {
                throw new InvalidDataException($"Teams entity record {ordinal} changes an immutable identity.");
            }

            ValidateTeam(row, ordinal);
            changed |= ApplyTeam(team, row, teamRecordIndicesByOrdinal);
            changed |= ApplyTeamActiveLines(team, ordinal, activeLineSlotRows, playerIds);

            var tacticRow = tacticRows[ordinal];
            var sourceSettings = FhmSaveSqliteWriter.SerializeTeamTactics(team.Tail.Tactics);
            if (tacticRow.SerializedSettings.Length != sourceSettings.Length)
            {
                throw new InvalidDataException($"Team tactics entity for team record {ordinal} must contain {sourceSettings.Length} bytes.");
            }

            if (!tacticRow.SerializedSettings.SequenceEqual(sourceSettings))
            {
                using var stream = new MemoryStream(tacticRow.SerializedSettings, writable: false);
                team.Tail.Tactics = FhmTeamTacticsSettings.ReadFrom(stream);
                if (stream.Position != stream.Length)
                {
                    throw new InvalidDataException($"Team tactics entity for team record {ordinal} contains trailing bytes.");
                }

                changed = true;
            }
        }

        return changed;
    }

    private static HashSet<(int, FhmLineGroup, int)> GetTeamActiveLineSlotKeys(FhmTeamsFile file)
    {
        var result = new HashSet<(int, FhmLineGroup, int)>();
        foreach (var (team, teamRecordOrdinal) in file.Teams.Select((value, index) => (value, index)))
        {
            foreach (var line in team.ActiveLines.Lists)
            {
                for (var slotOrdinal = 0; slotOrdinal < line.PlayerReferences.Count; slotOrdinal++)
                {
                    result.Add((teamRecordOrdinal, line.Group, slotOrdinal));
                }
            }
        }

        return result;
    }

    private static bool ApplyTeamActiveLines(
        FhmTeamRecord team,
        int teamRecordOrdinal,
        IReadOnlyDictionary<(int, FhmLineGroup, int), TeamActiveLineSlot> rows,
        ISet<int> playerIds)
    {
        var changed = false;
        foreach (var line in team.ActiveLines.Lists)
        {
            for (var slotOrdinal = 0; slotOrdinal < line.PlayerReferences.Count; slotOrdinal++)
            {
                var row = rows[(teamRecordOrdinal, line.Group, slotOrdinal)];
                if (row.PlayerInternalId is { } playerId)
                {
                    ValidateReference(
                        playerId,
                        playerIds,
                        $"team {team.TeamId} active line {line.Group}/{slotOrdinal}");
                    changed |= line.PlayerReferences[slotOrdinal] != playerId;
                    line.PlayerReferences[slotOrdinal] = playerId;
                }
                else
                {
                    changed |= line.PlayerReferences[slotOrdinal] != FhmNullConstants.Null;
                    line.PlayerReferences[slotOrdinal] = FhmNullConstants.Null;
                }
            }
        }

        return changed;
    }

    private static bool ApplyPersonnel(
        FhmPersonnelFile file,
        IReadOnlyCollection<Personnel> entities,
        IReadOnlyCollection<Team> teams)
    {
        RequireExactKeys(
            entities,
            file.Records.Select(value => value.PersonnelId).ToHashSet(),
            value => value.PersonnelId,
            nameof(Personnel));
        var rows = entities.ToDictionary(value => value.PersonnelId);
        var teamsByRecordOrdinal = teams.ToDictionary(value => value.RecordOrdinal);
        var changed = false;

        foreach (var record in file.Records)
        {
            var row = rows[record.PersonnelId];
            if (!row.SerializedRecord.SequenceEqual(record.GetSourceBytes()))
            {
                throw new InvalidDataException(
                    $"Personnel entity {record.PersonnelId} changes its immutable serialized backing.");
            }

            int? teamRecordIndex = null;
            if (row.TeamRecordOrdinal is int teamRecordOrdinal)
            {
                if (row.UnresolvedTeamRecordIndex is not null)
                {
                    throw new InvalidDataException(
                        $"Personnel entity {record.PersonnelId} has both a resolved and unresolved team reference.");
                }

                if (!teamsByRecordOrdinal.TryGetValue(teamRecordOrdinal, out var team))
                {
                    throw new InvalidDataException(
                        $"Personnel entity {record.PersonnelId} references missing team record ordinal {teamRecordOrdinal}.");
                }

                teamRecordIndex = team.RecordIndex;
            }
            else
            {
                teamRecordIndex = row.UnresolvedTeamRecordIndex;
            }

            ValidatePersonnel(row);
            changed |= ApplyPersonnelRecord(record, row, teamRecordIndex);
        }

        return changed;
    }

    private static bool ApplyPersonnelRecord(FhmPersonnelRecord destination, Personnel source, int? teamRecordIndex)
    {
        var changed =
            destination.FirstNameNameId != source.FirstNameNameId
            || destination.SurnameNameId != source.SurnameNameId
            || destination.NicknameNameId != source.NicknameNameId
            || destination.BirthDate != new FhmDate(source.BirthYear, source.BirthMonth, source.BirthDay)
            || destination.NationalityId != source.NationalityId
            || destination.BirthCityId != source.BirthCityId
            || destination.TeamRecordIndex != teamRecordIndex
            || destination.Job != source.Job
            || destination.Negotiating != source.Negotiating
            || destination.OffensivePreference != source.OffensivePreference
            || destination.PlayerManagement != source.PlayerManagement
            || destination.PhysicalPreference != source.PhysicalPreference
            || destination.CoachingDefense != source.CoachingDefense
            || destination.CoachingForwards != source.CoachingForwards
            || destination.CoachingGoalies != source.CoachingGoalies
            || destination.CoachingProspects != source.CoachingProspects
            || destination.EvaluateAbilities != source.EvaluateAbilities
            || destination.EvaluatePotential != source.EvaluatePotential
            || destination.Reputation != source.Reputation
            || destination.LineMatchingTendency != source.LineMatchingTendency
            || destination.GoalieHandlingTendency != source.GoalieHandlingTendency
            || destination.VeteranPreference != source.VeteranPreference
            || destination.InnovationTendency != source.InnovationTendency
            || destination.LoyaltyTendency != source.LoyaltyTendency
            || destination.Salary != source.Salary
            || destination.ContractLength != source.ContractLength
            || destination.Retired != source.Retired
            || destination.DefensiveSkills != source.DefensiveSkills
            || destination.OffensiveSkills != source.OffensiveSkills
            || destination.BasedInLocationId != source.BasedInLocationId
            || destination.PhysicalTraining != source.PhysicalTraining
            || destination.Tactics != source.Tactics
            || destination.Discipline != source.Discipline
            || destination.SelfPreservation != source.SelfPreservation
            || destination.Motivation != source.Motivation
            || destination.IngameTactics != source.IngameTactics
            || destination.TrainerSkill != source.TrainerSkill;

        destination.FirstNameNameId = source.FirstNameNameId;
        destination.SurnameNameId = source.SurnameNameId;
        destination.NicknameNameId = source.NicknameNameId;
        destination.BirthDate = new(source.BirthYear, source.BirthMonth, source.BirthDay);
        destination.NationalityId = checked((ushort)source.NationalityId);
        destination.BirthCityId = source.BirthCityId;
        destination.TeamRecordIndex = teamRecordIndex;
        destination.Job = source.Job;
        destination.Negotiating = source.Negotiating;
        destination.OffensivePreference = source.OffensivePreference;
        destination.PlayerManagement = source.PlayerManagement;
        destination.PhysicalPreference = source.PhysicalPreference;
        destination.CoachingDefense = source.CoachingDefense;
        destination.CoachingForwards = source.CoachingForwards;
        destination.CoachingGoalies = source.CoachingGoalies;
        destination.CoachingProspects = source.CoachingProspects;
        destination.EvaluateAbilities = source.EvaluateAbilities;
        destination.EvaluatePotential = source.EvaluatePotential;
        destination.Reputation = checked((ushort)source.Reputation);
        destination.LineMatchingTendency = source.LineMatchingTendency;
        destination.GoalieHandlingTendency = source.GoalieHandlingTendency;
        destination.VeteranPreference = source.VeteranPreference;
        destination.InnovationTendency = source.InnovationTendency;
        destination.LoyaltyTendency = source.LoyaltyTendency;
        destination.Salary = source.Salary;
        destination.ContractLength = source.ContractLength is null ? null : checked((byte)source.ContractLength.Value);
        destination.Retired = source.Retired;
        destination.DefensiveSkills = source.DefensiveSkills;
        destination.OffensiveSkills = source.OffensiveSkills;
        destination.BasedInLocationId = checked((ushort)source.BasedInLocationId);
        destination.PhysicalTraining = source.PhysicalTraining;
        destination.Tactics = source.Tactics;
        destination.Discipline = source.Discipline;
        destination.SelfPreservation = source.SelfPreservation;
        destination.Motivation = source.Motivation;
        destination.IngameTactics = source.IngameTactics;
        destination.TrainerSkill = source.TrainerSkill;
        return changed;
    }

    private static void ValidatePersonnel(Personnel row)
    {
        if (row.ContractLength is < 1 or > byte.MaxValue
            || row.NationalityId is < 0 or > ushort.MaxValue
            || row.Reputation is < 0 or > ushort.MaxValue
            || row.BasedInLocationId is < 0 or > ushort.MaxValue)
        {
            throw new InvalidDataException($"Personnel entity {row.PersonnelId} contains an invalid range or contract length.");
        }

        int?[] ratings =
        [
            row.Negotiating,
            row.PlayerManagement,
            row.CoachingDefense,
            row.CoachingForwards,
            row.CoachingGoalies,
            row.CoachingProspects,
            row.EvaluateAbilities,
            row.EvaluatePotential,
            row.DefensiveSkills,
            row.OffensiveSkills,
            row.PhysicalTraining,
            row.Tactics,
            row.Discipline,
            row.SelfPreservation,
            row.Motivation,
            row.IngameTactics,
            row.TrainerSkill,
        ];
        if (ratings.Any(value => value is < 0 or > 20))
        {
            throw new InvalidDataException($"Personnel entity {row.PersonnelId} contains a rating outside 0..20.");
        }
    }

    private static void ValidateTeam(Team row, int ordinal)
    {
        if (row.Flag1 is < byte.MinValue or > byte.MaxValue ||
            row.NicknamePlacement is < byte.MinValue or > byte.MaxValue ||
            row.MarketSize is < ushort.MinValue or > ushort.MaxValue ||
            row.FanLoyalty is < ushort.MinValue or > ushort.MaxValue)
        {
            throw new InvalidDataException($"Teams entity record {ordinal} contains a value outside its FHM wire range.");
        }
    }

    private static bool ApplyTeam(
        FhmTeamRecord destination,
        Team source,
        IReadOnlyDictionary<int, int> teamRecordIndicesByOrdinal)
    {
        var affiliateParentRecordIndex = ResolveTeamRecordIndex(
            source.AffiliateParentRecordOrdinal,
            teamRecordIndicesByOrdinal,
            "team affiliate parent");
        var secondaryAffiliateParentRecordIndex = ResolveTeamRecordIndex(
            source.SecondaryAffiliateParentRecordOrdinal,
            teamRecordIndicesByOrdinal,
            "team secondary affiliate parent");
        var changed =
            destination.InternalCode != source.InternalCode ||
            destination.InternalCode2 != source.InternalCode2 ||
            destination.Flag1 != source.Flag1 ||
            destination.City != source.City ||
            destination.Nickname != source.Nickname ||
            destination.NicknamePlacement != source.NicknamePlacement ||
            destination.AffiliateParentId != affiliateParentRecordIndex ||
            destination.AffiliateParentId2 != secondaryAffiliateParentRecordIndex ||
            destination.LeagueId != source.LeagueId ||
            destination.ConferenceId != source.ConferenceId ||
            destination.DivisionId != source.DivisionId ||
            destination.LocationId != source.LocationId ||
            destination.MarketSize != source.MarketSize ||
            destination.FanLoyalty != source.FanLoyalty ||
            destination.Finance1 != source.Finance1 ||
            destination.Finance2 != source.Finance2 ||
            destination.Finance3 != source.Finance3 ||
            destination.Finance4 != source.Finance4;

        destination.InternalCode = source.InternalCode;
        destination.InternalCode2 = source.InternalCode2;
        destination.Flag1 = checked((byte)source.Flag1);
        destination.City = source.City;
        destination.Nickname = source.Nickname;
        destination.NicknamePlacement = checked((byte)source.NicknamePlacement);
        destination.AffiliateParentId = affiliateParentRecordIndex;
        destination.AffiliateParentId2 = secondaryAffiliateParentRecordIndex;
        destination.LeagueId = source.LeagueId;
        destination.ConferenceId = source.ConferenceId;
        destination.DivisionId = source.DivisionId;
        destination.LocationId = source.LocationId;
        destination.MarketSize = checked((ushort)source.MarketSize);
        destination.FanLoyalty = checked((ushort)source.FanLoyalty);
        destination.Finance1 = source.Finance1;
        destination.Finance2 = source.Finance2;
        destination.Finance3 = source.Finance3;
        destination.Finance4 = source.Finance4;
        return changed;
    }

    private static bool ApplyGameSettings(FhmGameSettingsFile file, IReadOnlyCollection<GameSetting> entities)
    {
        RequireExactKeys(entities, Enumerable.Range(0, file.Values.Count).ToHashSet(), value => value.SettingOrdinal, nameof(GameSetting));
        var rows = entities.ToDictionary(value => value.SettingOrdinal);
        var changed = false;
        foreach (var ordinal in Enumerable.Range(0, file.Values.Count))
        {
            var row = rows[ordinal];
            var expected = FhmSaveSqliteWriter.ToGameSetting(ordinal, file.Values[ordinal]);
            if (row.GetType() != expected.GetType())
            {
                throw new InvalidDataException($"Game setting {ordinal} has incompatible entity type '{row.GetType().Name}'.");
            }

            object? value;
            switch (row)
            {
                case ByteGameSetting byteSetting:
                    value = byteSetting.Value;
                    break;
                case UInt16GameSetting uint16Setting:
                    value = uint16Setting.Value;
                    break;
                case Int32GameSetting int32Setting:
                    value = int32Setting.Value;
                    break;
                case DoubleGameSetting doubleSetting:
                    value = doubleSetting.Value;
                    if (double.IsNaN((double)value) || double.IsInfinity((double)value))
                    {
                        throw new InvalidDataException($"Game setting {ordinal} requires a finite real value.");
                    }

                    break;
                case QStringGameSetting stringSetting:
                    value = stringSetting.Value;
                    break;
                default:
                    throw new InvalidDataException($"Game setting {ordinal} has unsupported entity type '{row.GetType().Name}'.");
            }

            changed |= !Equals(file.Values[ordinal], value);
            file.Set((FhmGameSetting)ordinal, value);
        }

        return changed;
    }


    private static bool ApplyStoredLines(
        FhmStoredLinesFile file,
        IReadOnlyCollection<StoredLine> entities,
        IReadOnlyCollection<StoredLineSlot> slots,
        ISet<int> playerIds)
    {
        RequireExactKeys(entities, Enumerable.Range(0, file.StoredLines.Count).ToHashSet(), value => value.LineOrdinal, nameof(StoredLine));
        var expectedSlotKeys = GetStoredLineSlotKeys(file);
        RequireExactKeys(
            slots,
            expectedSlotKeys,
            value => (value.LineOrdinal, value.GroupOrdinal, value.SlotOrdinal),
            nameof(StoredLineSlot));
        var rows = entities.ToDictionary(value => value.LineOrdinal);
        var slotRows = slots.ToDictionary(value => (value.LineOrdinal, value.GroupOrdinal, value.SlotOrdinal));
        var changed = false;

        foreach (var (line, lineOrdinal) in file.StoredLines.Select((value, index) => (value, index)).ToArray())
        {
            var lineRow = rows[lineOrdinal];
            changed |= line.Name != lineRow.Name;
            line.Name = lineRow.Name;
            for (var groupOrdinal = 0; groupOrdinal < line.PlayerGroups.Count; groupOrdinal++)
            {
                var players = line.PlayerGroups[groupOrdinal];
                var locks = line.UnitLocks[groupOrdinal];
                var slotCount = Math.Max(players.Count, locks.Count);
                for (var slotOrdinal = 0; slotOrdinal < slotCount; slotOrdinal++)
                {
                    var row = slotRows[(lineOrdinal, groupOrdinal, slotOrdinal)];
                    var sourcePlayer = slotOrdinal < players.Count ? players[slotOrdinal] : (int?)null;
                    var sourceLock = slotOrdinal < locks.Count ? locks[slotOrdinal] : (int?)null;
                    if (row.UnitLock.HasValue != sourceLock.HasValue)
                    {
                        throw new InvalidDataException(
                            $"Stored-line entity {lineOrdinal}/{groupOrdinal}/{slotOrdinal} changes a fixed slot cardinality.");
                    }

                    if (row.PlayerInternalId is { } playerId)
                    {
                        ValidateReference(playerId, playerIds, $"stored-line {lineOrdinal}/{groupOrdinal}/{slotOrdinal}");
                        changed |= playerId != sourcePlayer;
                        players[slotOrdinal] = playerId;
                    }
                    else if (sourcePlayer.HasValue)
                    {
                        changed |= sourcePlayer != FhmNullConstants.Null;
                        players[slotOrdinal] = FhmNullConstants.Null;
                    }

                    if (row.UnitLock is { } lockValue)
                    {
                        if (lockValue is < byte.MinValue or > byte.MaxValue)
                        {
                            throw new InvalidDataException(
                                $"Stored-line entity {lineOrdinal}/{groupOrdinal}/{slotOrdinal} has lock value outside 0..255.");
                        }

                        changed |= lockValue != sourceLock;
                        locks[slotOrdinal] = checked((byte)lockValue);
                    }
                }
            }
        }

        return changed;
    }

    private static HashSet<(int, int, int)> GetStoredLineSlotKeys(FhmStoredLinesFile file)
    {
        var result = new HashSet<(int, int, int)>();
        foreach (var (line, lineOrdinal) in file.StoredLines.Select((value, index) => (value, index)).ToArray())
        {
            for (var groupOrdinal = 0; groupOrdinal < line.PlayerGroups.Count; groupOrdinal++)
            {
                var count = Math.Max(line.PlayerGroups[groupOrdinal].Count, line.UnitLocks[groupOrdinal].Count);
                foreach (var slotOrdinal in Enumerable.Range(0, count))
                {
                    result.Add((lineOrdinal, groupOrdinal, slotOrdinal));
                }
            }
        }

        return result;
    }

    private static bool ApplyTacticSystems(FhmTeamTacticsFile file, IReadOnlyCollection<TacticSystem> entities)
    {
        RequireExactKeys(entities, Enumerable.Range(0, file.Records.Count).ToHashSet(), value => value.RecordOrdinal, nameof(TacticSystem));
        var rows = entities.ToDictionary(value => value.RecordOrdinal);
        var globalIds = new HashSet<int>();
        var changed = false;
        foreach (var (record, ordinal) in file.Records.Select((value, index) => (value, index)).ToArray())
        {
            var row = rows[ordinal];
            if (row.GlobalId != record.GlobalId || !globalIds.Add(row.GlobalId))
            {
                throw new InvalidDataException($"Tactic-systems entity record {ordinal} changes or duplicates GlobalId {row.GlobalId}.");
            }

            changed |= record.ZoneGroupRaw != row.ZoneGroupRaw ||
                record.Name != row.Name ||
                record.RatingA != row.RatingA ||
                record.RatingB != row.RatingB;
            record.ZoneGroupRaw = row.ZoneGroupRaw;
            record.Name = row.Name;
            record.RatingA = row.RatingA;
            record.RatingB = row.RatingB;
        }

        return changed;
    }

    private static bool ApplyTacticTemplates(FhmTacticTemplatesFile file, IReadOnlyCollection<TacticTemplate> entities)
    {
        RequireExactKeys(entities, Enumerable.Range(0, file.Templates.Count).ToHashSet(), value => value.RecordOrdinal, nameof(TacticTemplate));
        var rows = entities.ToDictionary(value => value.RecordOrdinal);
        var changed = false;
        foreach (var (template, ordinal) in file.Templates.Select((value, index) => (value, index)).ToArray())
        {
            var row = rows[ordinal];
            if (row.TemplateIndex != template.TemplateIndex || row.SettingsBlob.Length != FhmTacticTemplatesFile.SettingsBlobLength)
            {
                throw new InvalidDataException(
                    $"Tactic-template entity record {ordinal} changes its immutable identity or has an invalid settings-blob length.");
            }

            var updated = new FhmTacticTemplate(row.InternalKey, row.TemplateIndex, row.DisplayName, row.SettingsBlob.ToArray());
            changed |= !Equals(updated, template);
            file.Templates[ordinal] = updated;
        }

        return changed;
    }

    private static bool ApplySetPlay(FhmSetPlayFile file, IReadOnlyCollection<SetPlay> entities)
    {
        var fileRows = entities.Where(value => string.Equals(value.RelativePath, file.RelativePath, StringComparison.OrdinalIgnoreCase)).ToArray();
        var expected = new HashSet<(bool, int)>();
        foreach (var ordinal in Enumerable.Range(0, file.Formations.Count))
        {
            expected.Add((false, ordinal));
        }

        foreach (var ordinal in Enumerable.Range(0, file.ExtraRecords.Count))
        {
            expected.Add((true, ordinal));
        }

        RequireExactKeys(fileRows, expected, value => (value.IsExtraRecord, value.RecordOrdinal), $"SetPlay ({file.RelativePath})");
        var rows = fileRows.ToDictionary(value => (value.IsExtraRecord, value.RecordOrdinal));
        var changed = ApplyBlocks(file.Formations, rows, false) | ApplyBlocks(file.ExtraRecords, rows, true);
        return changed;
    }

    private static bool ApplyModifierCatalogue(
        FhmLengthPrefixedCatalogueFile file,
        IReadOnlyCollection<ModifierCatalogue> entities)
    {
        var rows = entities.Where(value => string.Equals(value.RelativePath, file.RelativePath, StringComparison.OrdinalIgnoreCase)).ToArray();
        RequireExactKeys(rows, Enumerable.Range(0, file.Blocks.Count).ToHashSet(), value => value.RecordOrdinal, $"ModifierCatalogue ({file.RelativePath})");
        var changed = false;
        foreach (var (block, ordinal) in file.Blocks.Select((value, index) => (value, index)).ToArray())
        {
            var row = rows.Single(value => value.RecordOrdinal == ordinal);
            changed |= !block.Data.SequenceEqual(row.Data);
            file.Blocks[ordinal] = new FhmLengthPrefixedBlock(row.Data.ToArray());
        }

        return changed;
    }

    private static bool ApplyZoneEventModifiers(
        FhmZoneEventModifiersFile file,
        IReadOnlyCollection<ModifierCatalogue> entities)
    {
        var rows = entities.Where(value => string.Equals(value.RelativePath, file.RelativePath, StringComparison.OrdinalIgnoreCase)).ToArray();
        RequireExactKeys(rows, Enumerable.Range(0, file.ModifierGrids.Count).ToHashSet(), value => value.RecordOrdinal, $"ModifierCatalogue ({file.RelativePath})");
        var changed = false;
        foreach (var (block, ordinal) in file.ModifierGrids.Select((value, index) => (value, index)).ToArray())
        {
            var row = rows.Single(value => value.RecordOrdinal == ordinal);
            changed |= !block.Data.SequenceEqual(row.Data);
            file.ModifierGrids[ordinal] = new FhmLengthPrefixedBlock(row.Data.ToArray());
        }

        return changed;
    }

    private static bool ApplyBlocks(
        IList<FhmLengthPrefixedBlock> destination,
        IReadOnlyDictionary<(bool, int), SetPlay> rows,
        bool isExtraRecord)
    {
        var changed = false;
        foreach (var (block, ordinal) in destination.Select((value, index) => (value, index)).ToArray())
        {
            var row = rows[(isExtraRecord, ordinal)];
            changed |= !block.Data.SequenceEqual(row.Data);
            destination[ordinal] = new FhmLengthPrefixedBlock(row.Data.ToArray());
        }

        return changed;
    }

    private static bool ApplyTactics(FhmTacticsFile file, IReadOnlyCollection<Tactics> entities)
    {
        RequireExactKeys(entities, new HashSet<int> { 1 }, value => value.Id, nameof(Tactics));
        var row = entities.Single();
        if (row.TacticCount is < 0 or > 10_000_000)
        {
            throw new InvalidDataException($"Tactics entity has invalid tactic count {row.TacticCount}.");
        }

        var changed = file.Version != row.Version ||
            file.TacticCount != row.TacticCount ||
            !file.RecordsOpaque.SequenceEqual(row.RecordsOpaque);
        file.Version = row.Version;
        file.TacticCount = row.TacticCount;
        file.RecordsOpaque = row.RecordsOpaque.ToArray();
        return changed;
    }

    private static HashSet<string> ValidateAndApplyTacticHeaders(
        IReadOnlyDictionary<string, TacticFileExpectation> expected,
        IReadOnlyCollection<TacticFile> actual,
        FhmTeamTacticsFile? tacticSystems,
        FhmTacticTemplatesFile? templates,
        IEnumerable<FhmSetPlayFile> setPlays,
        IEnumerable<FhmLengthPrefixedCatalogueFile> catalogues,
        FhmZoneEventModifiersFile? zoneEvents)
    {
        RequireExactKeys(actual, expected.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase), value => value.RelativePath, nameof(TacticFile), StringComparer.OrdinalIgnoreCase);
        var rows = actual.ToDictionary(value => value.RelativePath, StringComparer.OrdinalIgnoreCase);
        var changedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (path, source) in expected)
        {
            var row = rows[path];
            if (row.Kind != source.Kind)
            {
                throw new InvalidDataException($"Tactic-file entity '{path}' has incompatible kind '{row.Kind}'.");
            }

            switch (source.Kind)
            {
                case TacticFileKind.TacticSystems:
                    if (row.RecordCount != tacticSystems!.Records.Count)
                    {
                        throw new InvalidDataException($"Tactic-file entity '{path}' changes its fixed systems count.");
                    }

                    if (tacticSystems.VersionTag != row.Version)
                    {
                        tacticSystems.VersionTag = row.Version;
                        changedPaths.Add(path);
                    }

                    break;
                case TacticFileKind.TacticTemplates:
                    if (row.RecordCount != templates!.Templates.Count)
                    {
                        throw new InvalidDataException($"Tactic-file entity '{path}' changes its fixed template count.");
                    }

                    if (templates.Version != row.Version)
                    {
                        templates.Version = row.Version;
                        changedPaths.Add(path);
                    }

                    break;
                case TacticFileKind.SetPlay:
                {
                    var file = setPlays.Single(value => string.Equals(value.RelativePath, path, StringComparison.OrdinalIgnoreCase));
                    if (row.RecordCount != file.Formations.Count)
                    {
                        throw new InvalidDataException($"Tactic-file entity '{path}' changes its fixed formation count.");
                    }

                    if (file.Version != row.Version)
                    {
                        file.Version = row.Version;
                        changedPaths.Add(path);
                    }

                    break;
                }
                case TacticFileKind.ModifierCatalogue:
                {
                    var file = catalogues.Single(value => string.Equals(value.RelativePath, path, StringComparison.OrdinalIgnoreCase));
                    if (row.RecordCount != file.Blocks.Count)
                    {
                        throw new InvalidDataException($"Tactic-file entity '{path}' changes an implicit block count.");
                    }

                    if (file.Version != row.Version)
                    {
                        file.Version = row.Version;
                        changedPaths.Add(path);
                    }

                    break;
                }
                case TacticFileKind.ZoneEventModifiers:
                    if (row.RecordCount is < 0 or > 10_000_000)
                    {
                        throw new InvalidDataException($"Tactic-file entity '{path}' has an invalid zone count.");
                    }

                    if (zoneEvents!.Version != row.Version || zoneEvents.ZoneCount != row.RecordCount)
                    {
                        zoneEvents.Version = row.Version;
                        zoneEvents.ZoneCount = row.RecordCount;
                        changedPaths.Add(path);
                    }

                    break;
                default:
                    throw new InvalidDataException($"Tactic-file entity '{path}' has unknown kind '{source.Kind}'.");
            }
        }

        return changedPaths;
    }

    private static void ValidateNoUnexpectedSetPlays(
        IEnumerable<SetPlay> rows,
        IReadOnlyDictionary<string, TacticFileExpectation> headers)
    {
        foreach (var path in rows.Select(row => row.RelativePath).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (!headers.TryGetValue(path, out var header) || header.Kind != TacticFileKind.SetPlay)
            {
                throw new InvalidDataException($"Set-play entity references unexpected file '{path}'.");
            }
        }
    }

    private static void ValidateNoUnexpectedModifiers(
        IEnumerable<ModifierCatalogue> rows,
        IReadOnlyDictionary<string, TacticFileExpectation> headers)
    {
        foreach (var path in rows.Select(row => row.RelativePath).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (!headers.TryGetValue(path, out var header) ||
                (header.Kind != TacticFileKind.ModifierCatalogue && header.Kind != TacticFileKind.ZoneEventModifiers))
            {
                throw new InvalidDataException($"Modifier-catalogue entity references unexpected file '{path}'.");
            }
        }
    }

    private static void RequireEmpty<T>(IReadOnlyCollection<T> rows, string description)
    {
        if (rows.Count != 0)
        {
            throw new InvalidDataException($"{description} rows exist without their projected source file.");
        }
    }

    private static void RequireExactKeys<T, TKey>(
        IEnumerable<T> values,
        ISet<TKey> expected,
        Func<T, TKey> keySelector,
        string description,
        IEqualityComparer<TKey>? comparer = null)
        where TKey : notnull
    {
        var actual = new HashSet<TKey>(comparer);
        foreach (var value in values)
        {
            if (!actual.Add(keySelector(value)))
            {
                throw new InvalidDataException($"{description} contains duplicate source keys.");
            }
        }

        if (!actual.SetEquals(expected))
        {
            throw new InvalidDataException($"{description} contains inserted, deleted, or reordered source rows.");
        }
    }

    private sealed class BaselineFile(string relativePath, SaveFileKind kind, int contentLength)
    {
        public string RelativePath { get; } = relativePath;
        public SaveFileKind Kind { get; } = kind;
        public int ContentLength { get; } = contentLength;
        public int ChunkCount { get; set; }
    }

    private sealed record BaselineFileProjection(
        string RelativePath,
        SaveFileKind Kind,
        int ContentLength);

    private sealed record SaveFileChunkProjection(
        string RelativePath,
        int Ordinal,
        int ContentLength);

    private sealed record SaveFileChunkContent(int Ordinal, byte[] Content);

    private sealed record TacticFileExpectation(TacticFileKind Kind, int Version, int RecordCount);
}
