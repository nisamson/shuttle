using Microsoft.EntityFrameworkCore;
using Shuttle.Fhm.Serde.Domain.Binary;
using Shuttle.Fhm.Serde.Domain.Files;
using Shuttle.Fhm.Serde.Domain.Model;
using Shuttle.Fhm.Serde.Domain.SaveFolder;

namespace Shuttle.Fhm.Serde.Sqlite;

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
        var manifest = await context.Manifests.AsNoTracking().SingleOrDefaultAsync(cancellationToken)
            ?? throw new InvalidDataException("SQLite save database does not contain a manifest.");
        if (manifest.Id != 1 || manifest.SchemaVersion != FhmSaveSqliteWriter.SchemaVersion ||
            manifest.SourceFormatVersion != "FHM 10 save folder")
        {
            throw new InvalidDataException(
                $"SQLite save database has unsupported manifest schema {manifest.SchemaVersion} or source format '{manifest.SourceFormatVersion}'.");
        }

        var files = await context.Files.AsNoTracking().ToListAsync(cancellationToken);
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
        var playerRoleIds = new HashSet<int>();

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
            if (ApplyPersonnel(personnelFile, personnel, teams, nameIds))
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
            if (ApplyTeams(teamsFile, teams, teamTactics, teamActiveLineSlots, personnelFile, playerIds))
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
        ISet<int> roleIds,
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
        var teamRecordIndicesById = teams.ToDictionary(value => value.TeamId, value => value.RecordIndex);
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
            var teamRecordIndex = ResolveTeamRecordIndex(row.TeamId, teamRecordIndicesById, $"player {row.InternalId}");
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
        ISet<int> roleIds)
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
                RoleId = row.RoleId,
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
            ApplyPlayerRoleWeights(role.RoleId, role.WeightGroupA, PlayerRoleWeightGroup.A, 8, weightsByRoleAndGroup);
            ApplyPlayerRoleWeights(role.RoleId, role.WeightGroupB, PlayerRoleWeightGroup.B, 13, weightsByRoleAndGroup);
            ApplyPlayerRoleWeights(role.RoleId, role.WeightGroupC, PlayerRoleWeightGroup.C, 17, weightsByRoleAndGroup);
            ApplyPlayerRoleWeights(role.RoleId, role.WeightGroupD, PlayerRoleWeightGroup.D, 4, weightsByRoleAndGroup);
            ApplyPlayerRoleWeights(role.RoleId, role.WeightGroupE, PlayerRoleWeightGroup.E, 19, weightsByRoleAndGroup);
            ApplyPlayerRoleWeights(role.RoleId, role.WeightGroupF, PlayerRoleWeightGroup.F, 9, weightsByRoleAndGroup);
            foreach (var list in Enum.GetValues<PlayerRoleIndexList>())
            {
                var rows = indicesByRoleAndList.GetValueOrDefault((role.RoleId, list)) ?? [];
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
        int roleId,
        IList<int> destination,
        PlayerRoleWeightGroup group,
        int expectedCount,
        IReadOnlyDictionary<(int RoleId, PlayerRoleWeightGroup Group), PlayerRoleWeight[]> weights)
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
        ISet<int> roleIds,
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
            replacement = new FhmPlayerRoleInstance { RoleId = assignment.RoleId };
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
        int? teamId,
        IReadOnlyDictionary<int, int> teamRecordIndicesById,
        string owner)
    {
        if (teamId is null)
        {
            return FhmNullConstants.Null;
        }

        if (!teamRecordIndicesById.TryGetValue(teamId.Value, out var recordIndex))
        {
            throw new InvalidDataException($"{owner} references missing stable team ID {teamId.Value}.");
        }

        return recordIndex;
    }

    private static bool ApplyTeams(
        FhmTeamsFile file,
        IReadOnlyCollection<Team> entities,
        IReadOnlyCollection<TeamTactic> tactics,
        IReadOnlyCollection<TeamActiveLineSlot> activeLineSlots,
        FhmPersonnelFile? personnelFile,
        ISet<int> playerIds)
    {
        RequireExactKeys(entities, Enumerable.Range(0, file.Teams.Count).ToHashSet(), value => value.RecordOrdinal, nameof(Team));
        RequireExactKeys(
            tactics,
            file.Teams.Select(value => value.TeamId).ToHashSet(),
            value => value.TeamId,
            nameof(TeamTactic));
        var rows = entities.ToDictionary(value => value.RecordOrdinal);
        var tacticRows = tactics.ToDictionary(value => value.TeamId);
        var expectedActiveLineSlotKeys = GetTeamActiveLineSlotKeys(file);
        RequireExactKeys(
            activeLineSlots,
            expectedActiveLineSlotKeys,
            value => (value.TeamId, value.Group, value.SlotOrdinal),
            nameof(TeamActiveLineSlot));
        var activeLineSlotRows = activeLineSlots.ToDictionary(
            value => (value.TeamId, value.Group, value.SlotOrdinal));
        var teamIds = new HashSet<int>();
        var changed = false;

        foreach (var (team, ordinal) in file.Teams.Select((value, index) => (value, index)).ToArray())
        {
            var row = rows[ordinal];
            if (row.RecordIndex != team.RecordIndex || row.TeamId != team.TeamId || !teamIds.Add(row.TeamId))
            {
                throw new InvalidDataException($"Teams entity record {ordinal} changes an immutable identity or duplicates TeamId {row.TeamId}.");
            }

            ValidateTeam(row, ordinal);
            changed |= ApplyTeam(team, row);
            if (personnelFile is not null)
            {
                changed |= ApplyTeamPersonnel(team, personnelFile.Records);
            }
            changed |= ApplyTeamActiveLines(team, activeLineSlotRows, playerIds);

            var tacticRow = tacticRows[team.TeamId];
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
        foreach (var team in file.Teams)
        {
            foreach (var line in team.ActiveLines.Lists)
            {
                for (var slotOrdinal = 0; slotOrdinal < line.PlayerReferences.Count; slotOrdinal++)
                {
                    result.Add((team.TeamId, line.Group, slotOrdinal));
                }
            }
        }

        return result;
    }

    private static bool ApplyTeamActiveLines(
        FhmTeamRecord team,
        IReadOnlyDictionary<(int, FhmLineGroup, int), TeamActiveLineSlot> rows,
        ISet<int> playerIds)
    {
        var changed = false;
        foreach (var line in team.ActiveLines.Lists)
        {
            for (var slotOrdinal = 0; slotOrdinal < line.PlayerReferences.Count; slotOrdinal++)
            {
                var row = rows[(team.TeamId, line.Group, slotOrdinal)];
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
        IReadOnlyCollection<Team> teams,
        ISet<int> nameIds)
    {
        RequireExactKeys(
            entities,
            file.Records.Select(value => value.PersonnelId).ToHashSet(),
            value => value.PersonnelId,
            nameof(Personnel));
        var rows = entities.ToDictionary(value => value.PersonnelId);
        var teamsById = teams.ToDictionary(value => value.TeamId);
        var changed = false;

        foreach (var record in file.Records)
        {
            var row = rows[record.PersonnelId];
            if (!row.SerializedRecord.SequenceEqual(record.GetSourceBytes()))
            {
                throw new InvalidDataException(
                    $"Personnel entity {record.PersonnelId} changes its immutable serialized backing.");
            }

            if (!nameIds.Contains(row.FirstNameNameId))
            {
                throw new InvalidDataException(
                    $"Personnel entity {record.PersonnelId} references missing first-name ID {row.FirstNameNameId}.");
            }

            if (!nameIds.Contains(row.SurnameNameId))
            {
                throw new InvalidDataException(
                    $"Personnel entity {record.PersonnelId} references missing surname name ID {row.SurnameNameId}.");
            }

            if (row.NicknameNameId is int nicknameNameId && !nameIds.Contains(nicknameNameId))
            {
                throw new InvalidDataException(
                    $"Personnel entity {record.PersonnelId} references missing nickname ID {nicknameNameId}.");
            }

            int? teamRecordIndex = null;
            if (row.TeamId is int teamId)
            {
                if (!teamsById.TryGetValue(teamId, out var team))
                {
                    throw new InvalidDataException(
                        $"Personnel entity {record.PersonnelId} references missing team ID {teamId}.");
                }

                teamRecordIndex = team.RecordIndex;
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
            || destination.BirthDate != new FhmDate(source.BirthDate.Year, source.BirthDate.Month, source.BirthDate.Day)
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
        destination.BirthDate = new(source.BirthDate.Year, source.BirthDate.Month, source.BirthDate.Day);
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
        if (!Enum.IsDefined(row.Job)
            || !Enum.IsDefined(row.OffensivePreference)
            || !Enum.IsDefined(row.PhysicalPreference)
            || !Enum.IsDefined(row.LineMatchingTendency)
            || !Enum.IsDefined(row.GoalieHandlingTendency)
            || !Enum.IsDefined(row.VeteranPreference)
            || !Enum.IsDefined(row.InnovationTendency)
            || !Enum.IsDefined(row.LoyaltyTendency)
            || row.ContractLength is < 1 or > byte.MaxValue
            || row.NationalityId is < 0 or > ushort.MaxValue
            || row.Reputation is < 0 or > 100
            || row.BasedInLocationId is < 0 or > ushort.MaxValue)
        {
            throw new InvalidDataException($"Personnel entity {row.PersonnelId} contains an invalid enum, range, or contract length.");
        }

        _ = row.BirthDate;

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

    private static bool ApplyTeamPersonnel(FhmTeamRecord team, IEnumerable<FhmPersonnelRecord> personnel)
    {
        var staff = personnel.Where(value => value.TeamRecordIndex == team.RecordIndex).ToArray();
        var generalManagerId = GetSingleStaffId(
            staff,
            value => value.Job is FhmPersonnelJob.GeneralManager or FhmPersonnelJob.GmHeadCoach,
            team,
            "general manager");
        var headCoachId = GetSingleStaffId(
            staff,
            value => value.Job is FhmPersonnelJob.HeadCoach or FhmPersonnelJob.GmHeadCoach,
            team,
            "head coach");
        var changed = team.Tail.GeneralManagerPersonnelId != generalManagerId
            || team.Tail.HeadCoachPersonnelId != headCoachId;
        team.Tail.GeneralManagerPersonnelId = generalManagerId;
        team.Tail.HeadCoachPersonnelId = headCoachId;
        return changed;
    }

    private static int GetSingleStaffId(
        IEnumerable<FhmPersonnelRecord> staff,
        Func<FhmPersonnelRecord, bool> predicate,
        FhmTeamRecord team,
        string role)
    {
        var matches = staff.Where(predicate).Select(value => value.PersonnelId).ToArray();
        return matches.Length switch
        {
            0 => -1,
            1 => matches[0],
            _ => throw new InvalidDataException(
                $"Team {team.TeamId} has multiple personnel records assigned as {role}: {string.Join(", ", matches)}."),
        };
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

    private static bool ApplyTeam(FhmTeamRecord destination, Team source)
    {
        var changed =
            destination.InternalCode != source.InternalCode ||
            destination.InternalCode2 != source.InternalCode2 ||
            destination.Flag1 != source.Flag1 ||
            destination.City != source.City ||
            destination.Nickname != source.Nickname ||
            destination.NicknamePlacement != source.NicknamePlacement ||
            destination.AffiliateParentId != ToWireReference(source.AffiliateParentId) ||
            destination.AffiliateParentId2 != ToWireReference(source.AffiliateParentId2) ||
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
        destination.AffiliateParentId = ToWireReference(source.AffiliateParentId);
        destination.AffiliateParentId2 = ToWireReference(source.AffiliateParentId2);
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

    private sealed record TacticFileExpectation(TacticFileKind Kind, int Version, int RecordCount);
}
