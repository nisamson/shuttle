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
        var players = await context.Players.AsNoTracking().ToListAsync(cancellationToken);
        var attributes = await context.PlayerAttributes.AsNoTracking().ToListAsync(cancellationToken);
        var teams = await context.Teams.AsNoTracking().ToListAsync(cancellationToken);
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

        var namesFile = GetDocumented<FhmNamesFile>(baseline, "names.dat");
        if (namesFile is not null)
        {
            if (ApplyNames(namesFile, names, nameIds))
            {
                SetDocumented(output, namesFile);
            }
        }
        else
        {
            RequireEmpty(names, nameof(NameProjection));
        }

        var playersFile = GetDocumented<FhmPlayersFile>(baseline, "players.dat");
        if (playersFile is not null)
        {
            if (ApplyPlayers(playersFile, players, attributes, playerIds))
            {
                SetDocumented(output, playersFile);
            }
        }
        else
        {
            RequireEmpty(players, nameof(PlayerProjection));
            RequireEmpty(attributes, nameof(PlayerAttributesProjection));
        }

        ValidatePlayerReferences(playersFile, nameIds);

        var teamsFile = GetDocumented<FhmTeamsFile>(baseline, "teams.dat");
        if (teamsFile is not null)
        {
            if (ApplyTeams(teamsFile, teams, teamTactics))
            {
                SetDocumented(output, teamsFile);
            }
        }
        else
        {
            RequireEmpty(teams, nameof(TeamProjection));
            RequireEmpty(teamTactics, nameof(TeamTacticProjection));
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
            RequireEmpty(gameSettings, nameof(GameSettingProjection));
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
            RequireEmpty(storedLines, nameof(StoredLineProjection));
            RequireEmpty(storedLineSlots, nameof(StoredLineSlotProjection));
        }

        var expectedTacticFiles = new Dictionary<string, TacticFileExpectation>(StringComparer.OrdinalIgnoreCase);
        var tacticSystemFile = GetDocumented<FhmTeamTacticsFile>(baseline, "team_tactics.dat");
        if (tacticSystemFile is not null)
        {
            expectedTacticFiles.Add(tacticSystemFile.RelativePath, new("TacticSystems", tacticSystemFile.VersionTag, tacticSystemFile.Records.Count));
            if (ApplyTacticSystems(tacticSystemFile, tacticSystems))
            {
                SetDocumented(output, tacticSystemFile);
            }
        }
        else
        {
            RequireEmpty(tacticSystems, nameof(TacticSystemProjection));
        }

        var templatesFile = GetDocumented<FhmTacticTemplatesFile>(baseline, "tactic_templates.dat");
        if (templatesFile is not null)
        {
            expectedTacticFiles.Add(templatesFile.RelativePath, new("TacticTemplates", templatesFile.Version, templatesFile.Templates.Count));
            if (ApplyTacticTemplates(templatesFile, tacticTemplates))
            {
                SetDocumented(output, templatesFile);
            }
        }
        else
        {
            RequireEmpty(tacticTemplates, nameof(TacticTemplateProjection));
        }

        var setPlayFiles = GetDocuments<FhmSetPlayFile>(baseline).ToList();
        foreach (var file in setPlayFiles)
        {
            expectedTacticFiles.Add(file.RelativePath, new("SetPlay", file.Version, file.Formations.Count));
            if (ApplySetPlay(file, setPlays))
            {
                SetDocumented(output, file);
            }
        }

        var modifierCatalogueFiles = GetDocuments<FhmLengthPrefixedCatalogueFile>(baseline).ToList();
        foreach (var file in modifierCatalogueFiles)
        {
            expectedTacticFiles.Add(file.RelativePath, new("ModifierCatalogue", file.Version, file.Blocks.Count));
            if (ApplyModifierCatalogue(file, modifierCatalogues))
            {
                SetDocumented(output, file);
            }
        }

        var zoneEventsFile = GetDocumented<FhmZoneEventModifiersFile>(baseline, "zone_event_mod.dat");
        if (zoneEventsFile is not null)
        {
            expectedTacticFiles.Add(zoneEventsFile.RelativePath, new("ZoneEventModifiers", zoneEventsFile.Version, zoneEventsFile.ZoneCount));
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
            RequireEmpty(tactics, nameof(TacticsProjection));
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

    private static bool ApplyNames(FhmNamesFile file, IReadOnlyCollection<NameProjection> projections, ISet<int> nameIds)
    {
        var expectedKeys = new HashSet<(string, int, int)>();
        foreach (var ordinal in Enumerable.Range(0, file.MasterNames.Count))
        {
            expectedKeys.Add(("Master", -1, ordinal));
        }

        AddNameListKeys(expectedKeys, "FirstName", file.FirstNameLists);
        AddNameListKeys(expectedKeys, "Surname", file.SurnameLists);
        foreach (var ordinal in Enumerable.Range(0, FhmNamesFile.NationCount))
        {
            expectedKeys.Add(("ScalarA", -1, ordinal));
            expectedKeys.Add(("ScalarB", -1, ordinal));
        }

        RequireExactKeys(projections, expectedKeys, value => (value.CollectionKind, value.NationIndex, value.Ordinal), nameof(NameProjection));
        var rows = projections.ToDictionary(value => (value.CollectionKind, value.NationIndex, value.Ordinal));
        var changed = false;

        foreach (var (entry, ordinal) in file.MasterNames.Select((value, index) => (value, index)).ToArray())
        {
            var row = rows[("Master", -1, ordinal)];
            ValidateMasterName(row, entry, ordinal);
            if (!nameIds.Add(row.NameId))
            {
                throw new InvalidDataException($"Names projection has duplicate master NameId {row.NameId}.");
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

        changed |= ApplyNameLists(file.FirstNameLists, rows, "FirstName", nameIds);
        changed |= ApplyNameLists(file.SurnameLists, rows, "Surname", nameIds);
        changed |= ApplyNameScalars(file.ScalarArrayA, rows, "ScalarA");
        changed |= ApplyNameScalars(file.ScalarArrayB, rows, "ScalarB");
        return changed;
    }

    private static void AddNameListKeys(
        ISet<(string, int, int)> keys,
        string kind,
        IEnumerable<IList<int>> source)
    {
        foreach (var (list, nation) in source.Select((value, index) => (value, index)).ToArray())
        {
            foreach (var ordinal in Enumerable.Range(0, list.Count))
            {
                keys.Add((kind, nation, ordinal));
            }
        }
    }

    private static void ValidateMasterName(NameProjection row, FhmNameEntry source, int ordinal)
    {
        if (row.NameId != source.NameId || row.IntegerValue != 0 ||
            row.CategoryWeight is < short.MinValue or > short.MaxValue ||
            row.FlagA is < byte.MinValue or > byte.MaxValue ||
            row.FlagB is < byte.MinValue or > byte.MaxValue ||
            row.FlagC is < byte.MinValue or > byte.MaxValue)
        {
            throw new InvalidDataException($"Names projection Master/{ordinal} has an invalid immutable identity or raw value.");
        }
    }

    private static bool ApplyNameLists(
        IList<IList<int>> destination,
        IReadOnlyDictionary<(string, int, int), NameProjection> rows,
        string kind,
        ISet<int> nameIds)
    {
        var changed = false;
        foreach (var (list, nation) in destination.Select((value, index) => (value, index)).ToArray())
        {
            foreach (var ordinal in Enumerable.Range(0, list.Count))
            {
                var row = rows[(kind, nation, ordinal)];
                if (row.NameId != 0 || row.Text is not null || row.GroupId != 0 || row.CategoryWeight != 0 ||
                    row.FlagA != 0 || row.FlagB != 0 || row.FlagC != 0)
                {
                    throw new InvalidDataException($"Names projection {kind}/{nation}/{ordinal} changes a non-applicable column.");
                }

                if (row.IntegerValue != FhmNullConstants.Null && !nameIds.Contains(row.IntegerValue))
                {
                    throw new InvalidDataException($"Names projection {kind}/{nation}/{ordinal} references unknown NameId {row.IntegerValue}.");
                }

                changed |= list[ordinal] != row.IntegerValue;
                list[ordinal] = row.IntegerValue;
            }
        }

        return changed;
    }

    private static bool ApplyNameScalars(
        IList<int> destination,
        IReadOnlyDictionary<(string, int, int), NameProjection> rows,
        string kind)
    {
        var changed = false;
        foreach (var ordinal in Enumerable.Range(0, destination.Count))
        {
            var row = rows[(kind, -1, ordinal)];
            if (row.NameId != 0 || row.Text is not null || row.GroupId != 0 || row.CategoryWeight != 0 ||
                row.FlagA != 0 || row.FlagB != 0 || row.FlagC != 0)
            {
                throw new InvalidDataException($"Names projection {kind}/{ordinal} changes a non-applicable column.");
            }

            changed |= destination[ordinal] != row.IntegerValue;
            destination[ordinal] = row.IntegerValue;
        }

        return changed;
    }

    private static bool ApplyPlayers(
        FhmPlayersFile file,
        IReadOnlyCollection<PlayerProjection> projections,
        IReadOnlyCollection<PlayerAttributesProjection> attributeProjections,
        ISet<int> playerIds)
    {
        RequireExactKeys(projections, Enumerable.Range(0, file.Players.Count).ToHashSet(), value => value.RecordOrdinal, nameof(PlayerProjection));
        RequireExactKeys(attributeProjections, Enumerable.Range(0, file.Players.Count).ToHashSet(), value => value.RecordOrdinal, nameof(PlayerAttributesProjection));
        var profiles = projections.ToDictionary(value => value.RecordOrdinal);
        var attributes = attributeProjections.ToDictionary(value => value.RecordOrdinal);
        var changed = false;

        foreach (var (player, ordinal) in file.Players.Select((value, index) => (value, index)).ToArray())
        {
            var row = profiles[ordinal];
            var ratingRow = attributes[ordinal];
            if (row.InternalId != player.InternalIdentity || !row.SerializedRecord.SequenceEqual(player.ToBytes()))
            {
                throw new InvalidDataException($"Players projection record {ordinal} changes its immutable identity or serialized backing.");
            }

            if (!playerIds.Add(row.InternalId))
            {
                throw new InvalidDataException($"Players projection has duplicate InternalId {row.InternalId}.");
            }

            ValidatePlayer(row, ordinal);
            changed |= ApplyPlayer(player, row);
            changed |= ApplyAttributes(player.RatingAttributes, ratingRow, ordinal);
        }

        return changed;
    }

    private static void ValidatePlayer(PlayerProjection row, int ordinal)
    {
        try
        {
            _ = new DateOnly(row.BirthYear, row.BirthMonth, row.BirthDay);
        }
        catch (ArgumentOutOfRangeException exception)
        {
            throw new InvalidDataException($"Players projection record {ordinal} has an invalid birth date.", exception);
        }

        var ratings = new[] { row.Goalie, row.LeftDefenceman, row.RightDefenceman, row.LeftWing, row.Centre, row.RightWing };
        if (ratings.Any(value => value is < 0 or > 20))
        {
            throw new InvalidDataException($"Players projection record {ordinal} has a position rating outside 0..20.");
        }
    }

    private static bool ApplyPlayer(FhmPlayerRecord destination, PlayerProjection source)
    {
        var changed =
            destination.ExportedPlayerId != source.ExternalId ||
            destination.FirstNameId != source.FirstNameId ||
            destination.SurnameId != source.SurnameId ||
            destination.CommonNameId != source.CommonNameId ||
            destination.BirthDate != new FhmDate(source.BirthYear, source.BirthMonth, source.BirthDay) ||
            destination.TeamId != source.TeamId ||
            destination.FranchiseId != source.FranchiseId ||
            destination.PositionRatings.Goalie != source.Goalie ||
            destination.PositionRatings.LeftDefenceman != source.LeftDefenceman ||
            destination.PositionRatings.RightDefenceman != source.RightDefenceman ||
            destination.PositionRatings.LeftWing != source.LeftWing ||
            destination.PositionRatings.Centre != source.Centre ||
            destination.PositionRatings.RightWing != source.RightWing;

        destination.ExportedPlayerId = source.ExternalId;
        destination.FirstNameId = source.FirstNameId;
        destination.SurnameId = source.SurnameId;
        destination.CommonNameId = source.CommonNameId;
        destination.BirthDate = new(source.BirthYear, source.BirthMonth, source.BirthDay);
        destination.TeamId = source.TeamId;
        destination.FranchiseId = source.FranchiseId;
        destination.PositionRatings.Goalie = checked((ushort)source.Goalie);
        destination.PositionRatings.LeftDefenceman = checked((ushort)source.LeftDefenceman);
        destination.PositionRatings.RightDefenceman = checked((ushort)source.RightDefenceman);
        destination.PositionRatings.LeftWing = checked((ushort)source.LeftWing);
        destination.PositionRatings.Centre = checked((ushort)source.Centre);
        destination.PositionRatings.RightWing = checked((ushort)source.RightWing);
        return changed;
    }

    private static bool ApplyAttributes(FhmPlayerAttributes destination, PlayerAttributesProjection source, int ordinal)
    {
        var changed = false;
        foreach (var projectionProperty in typeof(PlayerAttributesProjection).GetProperties())
        {
            if (projectionProperty.Name == nameof(PlayerAttributesProjection.RecordOrdinal))
            {
                continue;
            }

            var value = (int)projectionProperty.GetValue(source)!;
            if (value is < byte.MinValue or > byte.MaxValue)
            {
                throw new InvalidDataException($"Players projection record {ordinal} has rating {projectionProperty.Name} outside 0..255.");
            }

            var destinationProperty = typeof(FhmPlayerAttributes).GetProperty(projectionProperty.Name)!;
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

    private static bool ApplyTeams(
        FhmTeamsFile file,
        IReadOnlyCollection<TeamProjection> projections,
        IReadOnlyCollection<TeamTacticProjection> tactics)
    {
        RequireExactKeys(projections, Enumerable.Range(0, file.Teams.Count).ToHashSet(), value => value.RecordOrdinal, nameof(TeamProjection));
        RequireExactKeys(tactics, Enumerable.Range(0, file.Teams.Count).ToHashSet(), value => value.TeamRecordOrdinal, nameof(TeamTacticProjection));
        var rows = projections.ToDictionary(value => value.RecordOrdinal);
        var tacticRows = tactics.ToDictionary(value => value.TeamRecordOrdinal);
        var teamIds = new HashSet<int>();
        var changed = false;

        foreach (var (team, ordinal) in file.Teams.Select((value, index) => (value, index)).ToArray())
        {
            var row = rows[ordinal];
            if (row.RecordIndex != team.RecordIndex || row.TeamId != team.TeamId || !teamIds.Add(row.TeamId))
            {
                throw new InvalidDataException($"Teams projection record {ordinal} changes an immutable identity or duplicates TeamId {row.TeamId}.");
            }

            ValidateTeam(row, ordinal);
            changed |= ApplyTeam(team, row);

            var tacticRow = tacticRows[ordinal];
            var sourceSettings = FhmSaveSqliteWriter.SerializeTeamTactics(team.Tail.Tactics);
            if (tacticRow.SerializedSettings.Length != sourceSettings.Length)
            {
                throw new InvalidDataException($"Team tactics projection for team record {ordinal} must contain {sourceSettings.Length} bytes.");
            }

            if (!tacticRow.SerializedSettings.SequenceEqual(sourceSettings))
            {
                using var stream = new MemoryStream(tacticRow.SerializedSettings, writable: false);
                team.Tail.Tactics = FhmTeamTacticsSettings.ReadFrom(stream);
                if (stream.Position != stream.Length)
                {
                    throw new InvalidDataException($"Team tactics projection for team record {ordinal} contains trailing bytes.");
                }

                changed = true;
            }
        }

        return changed;
    }

    private static void ValidateTeam(TeamProjection row, int ordinal)
    {
        if (row.Flag1 is < byte.MinValue or > byte.MaxValue ||
            row.NicknamePlacement is < byte.MinValue or > byte.MaxValue ||
            row.MarketSize is < ushort.MinValue or > ushort.MaxValue ||
            row.FanLoyalty is < ushort.MinValue or > ushort.MaxValue)
        {
            throw new InvalidDataException($"Teams projection record {ordinal} contains a value outside its FHM wire range.");
        }
    }

    private static bool ApplyTeam(FhmTeamRecord destination, TeamProjection source)
    {
        var changed =
            destination.InternalCode != source.InternalCode ||
            destination.InternalCode2 != source.InternalCode2 ||
            destination.Flag1 != source.Flag1 ||
            destination.City != source.City ||
            destination.Nickname != source.Nickname ||
            destination.NicknamePlacement != source.NicknamePlacement ||
            destination.AffiliateParentId != source.AffiliateParentId ||
            destination.AffiliateParentId2 != source.AffiliateParentId2 ||
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
        destination.AffiliateParentId = source.AffiliateParentId;
        destination.AffiliateParentId2 = source.AffiliateParentId2;
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

    private static bool ApplyGameSettings(FhmGameSettingsFile file, IReadOnlyCollection<GameSettingProjection> projections)
    {
        RequireExactKeys(projections, Enumerable.Range(0, file.Values.Count).ToHashSet(), value => value.SettingOrdinal, nameof(GameSettingProjection));
        var rows = projections.ToDictionary(value => value.SettingOrdinal);
        var changed = false;
        foreach (var ordinal in Enumerable.Range(0, file.Values.Count))
        {
            var row = rows[ordinal];
            var expected = FhmSaveSqliteWriter.ToGameSettingProjection(ordinal, file.Values[ordinal]);
            if (!string.Equals(row.ValueKind, expected.ValueKind, StringComparison.Ordinal))
            {
                throw new InvalidDataException($"Game setting {ordinal} has incompatible value kind '{row.ValueKind}'.");
            }

            object? value;
            switch (row.ValueKind)
            {
                case "Byte":
                    RequireUnusedSettingColumns(row, hasInteger: true, hasReal: false, hasText: false);
                    value = CheckedSettingInteger<byte>(row, ordinal);
                    break;
                case "UInt16":
                    RequireUnusedSettingColumns(row, hasInteger: true, hasReal: false, hasText: false);
                    value = CheckedSettingInteger<ushort>(row, ordinal);
                    break;
                case "Int32":
                    RequireUnusedSettingColumns(row, hasInteger: true, hasReal: false, hasText: false);
                    value = CheckedSettingInteger<int>(row, ordinal);
                    break;
                case "Double":
                    RequireUnusedSettingColumns(row, hasInteger: false, hasReal: true, hasText: false);
                    value = row.RealValue ?? throw new InvalidDataException($"Game setting {ordinal} requires a real value.");
                    if (double.IsNaN((double)value) || double.IsInfinity((double)value))
                    {
                        throw new InvalidDataException($"Game setting {ordinal} requires a finite real value.");
                    }

                    break;
                case "QString":
                    RequireUnusedSettingColumns(row, hasInteger: false, hasReal: false, hasText: true);
                    value = row.TextValue;
                    break;
                default:
                    throw new InvalidDataException($"Game setting {ordinal} has unsupported value kind '{row.ValueKind}'.");
            }

            changed |= !Equals(file.Values[ordinal], value);
            file.Set((FhmGameSetting)ordinal, value);
        }

        return changed;
    }

    private static void RequireUnusedSettingColumns(
        GameSettingProjection row,
        bool hasInteger,
        bool hasReal,
        bool hasText)
    {
        if ((hasInteger != row.IntegerValue.HasValue) ||
            (hasReal != row.RealValue.HasValue) ||
            (!hasText && row.TextValue is not null))
        {
            throw new InvalidDataException($"Game setting {row.SettingOrdinal} contains a value in an incompatible column.");
        }
    }

    private static T CheckedSettingInteger<T>(GameSettingProjection row, int ordinal)
        where T : struct, IConvertible
    {
        var value = row.IntegerValue ?? throw new InvalidDataException($"Game setting {ordinal} requires an integer value.");
        try
        {
            return (T)Convert.ChangeType(value, typeof(T), System.Globalization.CultureInfo.InvariantCulture);
        }
        catch (OverflowException exception)
        {
            throw new InvalidDataException($"Game setting {ordinal} is outside the {typeof(T).Name} range.", exception);
        }
    }

    private static bool ApplyStoredLines(
        FhmStoredLinesFile file,
        IReadOnlyCollection<StoredLineProjection> projections,
        IReadOnlyCollection<StoredLineSlotProjection> slots,
        ISet<int> playerIds)
    {
        RequireExactKeys(projections, Enumerable.Range(0, file.StoredLines.Count).ToHashSet(), value => value.LineOrdinal, nameof(StoredLineProjection));
        var expectedSlotKeys = GetStoredLineSlotKeys(file);
        RequireExactKeys(
            slots,
            expectedSlotKeys,
            value => (value.LineOrdinal, value.GroupOrdinal, value.SlotOrdinal),
            nameof(StoredLineSlotProjection));
        var rows = projections.ToDictionary(value => value.LineOrdinal);
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
                    if (row.PlayerInternalId.HasValue != sourcePlayer.HasValue || row.UnitLock.HasValue != sourceLock.HasValue)
                    {
                        throw new InvalidDataException(
                            $"Stored-line projection {lineOrdinal}/{groupOrdinal}/{slotOrdinal} changes a fixed slot cardinality.");
                    }

                    if (row.PlayerInternalId is { } playerId)
                    {
                        ValidateReference(playerId, playerIds, $"stored-line {lineOrdinal}/{groupOrdinal}/{slotOrdinal}");
                        changed |= playerId != sourcePlayer;
                        players[slotOrdinal] = playerId;
                    }

                    if (row.UnitLock is { } lockValue)
                    {
                        if (lockValue is < byte.MinValue or > byte.MaxValue)
                        {
                            throw new InvalidDataException(
                                $"Stored-line projection {lineOrdinal}/{groupOrdinal}/{slotOrdinal} has lock value outside 0..255.");
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

    private static bool ApplyTacticSystems(FhmTeamTacticsFile file, IReadOnlyCollection<TacticSystemProjection> projections)
    {
        RequireExactKeys(projections, Enumerable.Range(0, file.Records.Count).ToHashSet(), value => value.RecordOrdinal, nameof(TacticSystemProjection));
        var rows = projections.ToDictionary(value => value.RecordOrdinal);
        var globalIds = new HashSet<int>();
        var changed = false;
        foreach (var (record, ordinal) in file.Records.Select((value, index) => (value, index)).ToArray())
        {
            var row = rows[ordinal];
            if (row.GlobalId != record.GlobalId || !globalIds.Add(row.GlobalId))
            {
                throw new InvalidDataException($"Tactic-systems projection record {ordinal} changes or duplicates GlobalId {row.GlobalId}.");
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

    private static bool ApplyTacticTemplates(FhmTacticTemplatesFile file, IReadOnlyCollection<TacticTemplateProjection> projections)
    {
        RequireExactKeys(projections, Enumerable.Range(0, file.Templates.Count).ToHashSet(), value => value.RecordOrdinal, nameof(TacticTemplateProjection));
        var rows = projections.ToDictionary(value => value.RecordOrdinal);
        var changed = false;
        foreach (var (template, ordinal) in file.Templates.Select((value, index) => (value, index)).ToArray())
        {
            var row = rows[ordinal];
            if (row.TemplateIndex != template.TemplateIndex || row.SettingsBlob.Length != FhmTacticTemplatesFile.SettingsBlobLength)
            {
                throw new InvalidDataException(
                    $"Tactic-template projection record {ordinal} changes its immutable identity or has an invalid settings-blob length.");
            }

            var updated = new FhmTacticTemplate(row.InternalKey, row.TemplateIndex, row.DisplayName, row.SettingsBlob.ToArray());
            changed |= !Equals(updated, template);
            file.Templates[ordinal] = updated;
        }

        return changed;
    }

    private static bool ApplySetPlay(FhmSetPlayFile file, IReadOnlyCollection<SetPlayProjection> projections)
    {
        var fileRows = projections.Where(value => string.Equals(value.RelativePath, file.RelativePath, StringComparison.OrdinalIgnoreCase)).ToArray();
        var expected = new HashSet<(bool, int)>();
        foreach (var ordinal in Enumerable.Range(0, file.Formations.Count))
        {
            expected.Add((false, ordinal));
        }

        foreach (var ordinal in Enumerable.Range(0, file.ExtraRecords.Count))
        {
            expected.Add((true, ordinal));
        }

        RequireExactKeys(fileRows, expected, value => (value.IsExtraRecord, value.RecordOrdinal), $"SetPlayProjection ({file.RelativePath})");
        var rows = fileRows.ToDictionary(value => (value.IsExtraRecord, value.RecordOrdinal));
        var changed = ApplyBlocks(file.Formations, rows, false) | ApplyBlocks(file.ExtraRecords, rows, true);
        return changed;
    }

    private static bool ApplyModifierCatalogue(
        FhmLengthPrefixedCatalogueFile file,
        IReadOnlyCollection<ModifierCatalogueProjection> projections)
    {
        var rows = projections.Where(value => string.Equals(value.RelativePath, file.RelativePath, StringComparison.OrdinalIgnoreCase)).ToArray();
        RequireExactKeys(rows, Enumerable.Range(0, file.Blocks.Count).ToHashSet(), value => value.RecordOrdinal, $"ModifierCatalogueProjection ({file.RelativePath})");
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
        IReadOnlyCollection<ModifierCatalogueProjection> projections)
    {
        var rows = projections.Where(value => string.Equals(value.RelativePath, file.RelativePath, StringComparison.OrdinalIgnoreCase)).ToArray();
        RequireExactKeys(rows, Enumerable.Range(0, file.ModifierGrids.Count).ToHashSet(), value => value.RecordOrdinal, $"ModifierCatalogueProjection ({file.RelativePath})");
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
        IReadOnlyDictionary<(bool, int), SetPlayProjection> rows,
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

    private static bool ApplyTactics(FhmTacticsFile file, IReadOnlyCollection<TacticsProjection> projections)
    {
        RequireExactKeys(projections, new HashSet<int> { 1 }, value => value.Id, nameof(TacticsProjection));
        var row = projections.Single();
        if (row.TacticCount is < 0 or > 10_000_000)
        {
            throw new InvalidDataException($"Tactics projection has invalid tactic count {row.TacticCount}.");
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
        IReadOnlyCollection<TacticFileProjection> actual,
        FhmTeamTacticsFile? tacticSystems,
        FhmTacticTemplatesFile? templates,
        IEnumerable<FhmSetPlayFile> setPlays,
        IEnumerable<FhmLengthPrefixedCatalogueFile> catalogues,
        FhmZoneEventModifiersFile? zoneEvents)
    {
        RequireExactKeys(actual, expected.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase), value => value.RelativePath, nameof(TacticFileProjection), StringComparer.OrdinalIgnoreCase);
        var rows = actual.ToDictionary(value => value.RelativePath, StringComparer.OrdinalIgnoreCase);
        var changedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (path, source) in expected)
        {
            var row = rows[path];
            if (!string.Equals(row.Kind, source.Kind, StringComparison.Ordinal))
            {
                throw new InvalidDataException($"Tactic-file projection '{path}' has incompatible kind '{row.Kind}'.");
            }

            switch (source.Kind)
            {
                case "TacticSystems":
                    if (row.RecordCount != tacticSystems!.Records.Count)
                    {
                        throw new InvalidDataException($"Tactic-file projection '{path}' changes its fixed systems count.");
                    }

                    if (tacticSystems.VersionTag != row.Version)
                    {
                        tacticSystems.VersionTag = row.Version;
                        changedPaths.Add(path);
                    }

                    break;
                case "TacticTemplates":
                    if (row.RecordCount != templates!.Templates.Count)
                    {
                        throw new InvalidDataException($"Tactic-file projection '{path}' changes its fixed template count.");
                    }

                    if (templates.Version != row.Version)
                    {
                        templates.Version = row.Version;
                        changedPaths.Add(path);
                    }

                    break;
                case "SetPlay":
                {
                    var file = setPlays.Single(value => string.Equals(value.RelativePath, path, StringComparison.OrdinalIgnoreCase));
                    if (row.RecordCount != file.Formations.Count)
                    {
                        throw new InvalidDataException($"Tactic-file projection '{path}' changes its fixed formation count.");
                    }

                    if (file.Version != row.Version)
                    {
                        file.Version = row.Version;
                        changedPaths.Add(path);
                    }

                    break;
                }
                case "ModifierCatalogue":
                {
                    var file = catalogues.Single(value => string.Equals(value.RelativePath, path, StringComparison.OrdinalIgnoreCase));
                    if (row.RecordCount != file.Blocks.Count)
                    {
                        throw new InvalidDataException($"Tactic-file projection '{path}' changes an implicit block count.");
                    }

                    if (file.Version != row.Version)
                    {
                        file.Version = row.Version;
                        changedPaths.Add(path);
                    }

                    break;
                }
                case "ZoneEventModifiers":
                    if (row.RecordCount is < 0 or > 10_000_000)
                    {
                        throw new InvalidDataException($"Tactic-file projection '{path}' has an invalid zone count.");
                    }

                    if (zoneEvents!.Version != row.Version || zoneEvents.ZoneCount != row.RecordCount)
                    {
                        zoneEvents.Version = row.Version;
                        zoneEvents.ZoneCount = row.RecordCount;
                        changedPaths.Add(path);
                    }

                    break;
                default:
                    throw new InvalidDataException($"Tactic-file projection '{path}' has unknown kind '{source.Kind}'.");
            }
        }

        return changedPaths;
    }

    private static void ValidateNoUnexpectedSetPlays(
        IEnumerable<SetPlayProjection> rows,
        IReadOnlyDictionary<string, TacticFileExpectation> headers)
    {
        foreach (var path in rows.Select(row => row.RelativePath).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (!headers.TryGetValue(path, out var header) || header.Kind != "SetPlay")
            {
                throw new InvalidDataException($"Set-play projection references unexpected file '{path}'.");
            }
        }
    }

    private static void ValidateNoUnexpectedModifiers(
        IEnumerable<ModifierCatalogueProjection> rows,
        IReadOnlyDictionary<string, TacticFileExpectation> headers)
    {
        foreach (var path in rows.Select(row => row.RelativePath).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (!headers.TryGetValue(path, out var header) ||
                (header.Kind != "ModifierCatalogue" && header.Kind != "ZoneEventModifiers"))
            {
                throw new InvalidDataException($"Modifier-catalogue projection references unexpected file '{path}'.");
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

    private sealed record TacticFileExpectation(string Kind, int Version, int RecordCount);
}
