using Microsoft.EntityFrameworkCore;
using Shuttle.Fhm.Serde.Domain.Files;
using Shuttle.Fhm.Serde.Domain.SaveFolder;

namespace Shuttle.Fhm.Serde.Sqlite;

/// <summary>Options governing creation of an FHM save SQLite database.</summary>
public sealed class FhmSaveSqliteWriteOptions
{
    /// <summary>Gets or sets whether an existing database file may be replaced.</summary>
    public bool Overwrite { get; init; }
}

/// <summary>Imports an FHM save into the lossless SQLite format.</summary>
public sealed class FhmSaveSqliteWriter
{
    /// <summary>Current compatible SQLite schema version.</summary>
    public const int SchemaVersion = 1;

    /// <summary>Writes a new database, refusing to replace an existing file.</summary>
    public Task WriteAsync(FhmSave save, string databasePath, CancellationToken cancellationToken = default) =>
        WriteAsync(save, databasePath, new FhmSaveSqliteWriteOptions(), cancellationToken);

    /// <summary>Writes a new database using explicit replacement options.</summary>
    public async Task WriteAsync(
        FhmSave save,
        string databasePath,
        FhmSaveSqliteWriteOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(save);
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);
        ArgumentNullException.ThrowIfNull(options);

        var fullPath = Path.GetFullPath(databasePath);
        var databaseExists = File.Exists(fullPath);
        if (databaseExists && !options.Overwrite)
        {
            throw new IOException($"SQLite save database '{fullPath}' already exists. Set Overwrite to replace it explicitly.");
        }

        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        await using var context = new FhmSaveSqliteContext(FhmSaveSqliteContext.CreateOptions(fullPath));
        if (databaseExists)
        {
            await EnsureExistingDatabaseIsAdapterFormatAsync(context, fullPath, cancellationToken);
        }

        await context.Database.MigrateAsync(cancellationToken);
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            if (await context.Manifests.AnyAsync(cancellationToken))
            {
                if (!options.Overwrite)
                {
                    throw new InvalidDataException("The SQLite save database is already populated.");
                }

                await ClearExistingSaveAsync(context, cancellationToken);
            }
            else if (databaseExists)
            {
                throw new InvalidDataException(
                    $"SQLite save database '{fullPath}' does not contain an adapter manifest and cannot be replaced safely.");
            }

            var sourceFiles = GetSourceFiles(save);
            context.Manifests.Add(new SaveManifest
            {
                SchemaVersion = SchemaVersion,
                SourceFormatVersion = "FHM 10 save folder",
                SourceFileCount = sourceFiles.Count,
            });

            foreach (var sourceFile in sourceFiles)
            {
                context.Files.Add(new SaveFile
                {
                    RelativePath = sourceFile.RelativePath,
                    Kind = sourceFile.Kind,
                    Content = sourceFile.Content,
                });

                if (sourceFile.Kind == SaveFileKind.Documented)
                {
                    AddProjections(context, sourceFile.RelativePath, FhmSaveFileFactory.TryRead(sourceFile.RelativePath, sourceFile.Content));
                }
            }

            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
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
        await context.PlayerAttributes.ExecuteDeleteAsync(cancellationToken);
        await context.Players.ExecuteDeleteAsync(cancellationToken);
        await context.TeamTactics.ExecuteDeleteAsync(cancellationToken);
        await context.Teams.ExecuteDeleteAsync(cancellationToken);
        await context.StoredLineSlots.ExecuteDeleteAsync(cancellationToken);
        await context.StoredLines.ExecuteDeleteAsync(cancellationToken);
        await context.Names.ExecuteDeleteAsync(cancellationToken);
        await context.GameSettings.ExecuteDeleteAsync(cancellationToken);
        await context.TacticSystems.ExecuteDeleteAsync(cancellationToken);
        await context.TacticTemplates.ExecuteDeleteAsync(cancellationToken);
        await context.SetPlays.ExecuteDeleteAsync(cancellationToken);
        await context.ModifierCatalogues.ExecuteDeleteAsync(cancellationToken);
        await context.Tactics.ExecuteDeleteAsync(cancellationToken);
        await context.TacticFiles.ExecuteDeleteAsync(cancellationToken);
        await context.Files.ExecuteDeleteAsync(cancellationToken);
        await context.Manifests.ExecuteDeleteAsync(cancellationToken);
    }

    private static List<SourceFile> GetSourceFiles(FhmSave save)
    {
        var result = new List<SourceFile>();
        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in save.Files.Values)
        {
            ArgumentNullException.ThrowIfNull(file);
            var relativePath = FhmSaveFileFactory.NormalizeRelativePath(file.RelativePath);
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
            if (!paths.Add(relativePath))
            {
                throw new InvalidDataException($"Duplicate save-file path '{relativePath}'.");
            }

            result.Add(new(relativePath, SaveFileKind.Opaque, file.Content.ToArray()));
        }

        return result;
    }

    private static void AddProjections(FhmSaveSqliteContext context, string relativePath, IFhmSaveFile? file)
    {
        switch (file)
        {
            case FhmNamesFile names:
                AddNames(context, names);
                break;
            case FhmPlayersFile players:
                AddPlayers(context, players);
                break;
            case FhmTeamsFile teams:
                AddTeams(context, teams);
                break;
            case FhmGameSettingsFile gameSettings:
                AddGameSettings(context, gameSettings);
                break;
            case FhmStoredLinesFile storedLines:
                AddStoredLines(context, storedLines);
                break;
            case FhmTeamTacticsFile tacticSystems:
                context.TacticFiles.Add(new TacticFileProjection
                {
                    RelativePath = relativePath,
                    Kind = "TacticSystems",
                    Version = tacticSystems.VersionTag,
                    RecordCount = tacticSystems.Records.Count,
                });
                foreach (var (record, ordinal) in tacticSystems.Records.Select((value, index) => (value, index)))
                {
                    context.TacticSystems.Add(new TacticSystemProjection
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
                context.TacticFiles.Add(new TacticFileProjection
                {
                    RelativePath = relativePath,
                    Kind = "TacticTemplates",
                    Version = templates.Version,
                    RecordCount = templates.Templates.Count,
                });
                foreach (var (template, ordinal) in templates.Templates.Select((value, index) => (value, index)))
                {
                    context.TacticTemplates.Add(new TacticTemplateProjection
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
                context.TacticFiles.Add(new TacticFileProjection
                {
                    RelativePath = relativePath,
                    Kind = "SetPlay",
                    Version = setPlay.Version,
                    RecordCount = setPlay.Formations.Count,
                });
                AddSetPlayBlocks(context, relativePath, false, setPlay.Formations);
                AddSetPlayBlocks(context, relativePath, true, setPlay.ExtraRecords);
                break;
            case FhmLengthPrefixedCatalogueFile catalogue:
                context.TacticFiles.Add(new TacticFileProjection
                {
                    RelativePath = relativePath,
                    Kind = "ModifierCatalogue",
                    Version = catalogue.Version,
                    RecordCount = catalogue.Blocks.Count,
                });
                AddModifierBlocks(context, relativePath, catalogue.Blocks);
                break;
            case FhmZoneEventModifiersFile zoneEvents:
                context.TacticFiles.Add(new TacticFileProjection
                {
                    RelativePath = relativePath,
                    Kind = "ZoneEventModifiers",
                    Version = zoneEvents.Version,
                    RecordCount = zoneEvents.ZoneCount,
                });
                AddModifierBlocks(context, relativePath, zoneEvents.ModifierGrids);
                break;
            case FhmTacticsFile tactics:
                context.Tactics.Add(new TacticsProjection
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
        foreach (var (entry, ordinal) in file.MasterNames.Select((value, index) => (value, index)))
        {
            context.Names.Add(new NameProjection
            {
                CollectionKind = "Master",
                NationIndex = -1,
                Ordinal = ordinal,
                NameId = entry.NameId,
                Text = entry.Text,
                GroupId = entry.GroupId,
                CategoryWeight = entry.CategoryWeight,
                FlagA = entry.FlagA,
                FlagB = entry.FlagB,
                FlagC = entry.FlagC,
            });
        }

        AddNameLists(context, "FirstName", file.FirstNameLists);
        AddNameLists(context, "Surname", file.SurnameLists);
        AddNameScalars(context, "ScalarA", file.ScalarArrayA);
        AddNameScalars(context, "ScalarB", file.ScalarArrayB);
    }

    private static void AddNameLists(FhmSaveSqliteContext context, string kind, IEnumerable<IList<int>> values)
    {
        foreach (var (list, nation) in values.Select((value, index) => (value, index)))
        {
            foreach (var (nameId, ordinal) in list.Select((value, index) => (value, index)))
            {
                context.Names.Add(new NameProjection
                {
                    CollectionKind = kind,
                    NationIndex = nation,
                    Ordinal = ordinal,
                    IntegerValue = nameId,
                });
            }
        }
    }

    private static void AddNameScalars(FhmSaveSqliteContext context, string kind, IEnumerable<int> values)
    {
        foreach (var (value, ordinal) in values.Select((item, index) => (item, index)))
        {
            context.Names.Add(new NameProjection
            {
                CollectionKind = kind,
                NationIndex = -1,
                Ordinal = ordinal,
                IntegerValue = value,
            });
        }
    }

    private static void AddPlayers(FhmSaveSqliteContext context, FhmPlayersFile file)
    {
        foreach (var (player, ordinal) in file.Players.Select((value, index) => (value, index)))
        {
            context.Players.Add(new PlayerProjection
            {
                RecordOrdinal = ordinal,
                InternalId = player.InternalIdentity,
                ExternalId = player.ExportedPlayerId,
                FirstNameId = player.FirstNameId,
                SurnameId = player.SurnameId,
                CommonNameId = player.CommonNameId,
                BirthYear = player.BirthDate.Year,
                BirthMonth = player.BirthDate.Month,
                BirthDay = player.BirthDate.Day,
                TeamId = player.TeamId,
                FranchiseId = player.FranchiseId,
                Goalie = player.PositionRatings.Goalie,
                LeftDefenceman = player.PositionRatings.LeftDefenceman,
                RightDefenceman = player.PositionRatings.RightDefenceman,
                LeftWing = player.PositionRatings.LeftWing,
                Centre = player.PositionRatings.Centre,
                RightWing = player.PositionRatings.RightWing,
                SerializedRecord = player.ToBytes(),
            });
            context.PlayerAttributes.Add(ToProjection(ordinal, player.RatingAttributes));
        }
    }

    private static void AddTeams(FhmSaveSqliteContext context, FhmTeamsFile file)
    {
        foreach (var (team, ordinal) in file.Teams.Select((value, index) => (value, index)))
        {
            context.Teams.Add(new TeamProjection
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
                AffiliateParentId = team.AffiliateParentId,
                AffiliateParentId2 = team.AffiliateParentId2,
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
            context.TeamTactics.Add(new TeamTacticProjection
            {
                TeamRecordOrdinal = ordinal,
                SerializedSettings = SerializeTeamTactics(team.Tail.Tactics),
            });
        }
    }

    private static void AddGameSettings(FhmSaveSqliteContext context, FhmGameSettingsFile file)
    {
        for (var ordinal = 0; ordinal < file.Values.Count; ordinal++)
        {
            context.GameSettings.Add(ToGameSettingProjection(ordinal, file.Values[ordinal]));
        }
    }

    private static void AddStoredLines(FhmSaveSqliteContext context, FhmStoredLinesFile file)
    {
        foreach (var (line, lineOrdinal) in file.StoredLines.Select((value, index) => (value, index)))
        {
            context.StoredLines.Add(new StoredLineProjection { LineOrdinal = lineOrdinal, Name = line.Name });
            for (var groupOrdinal = 0; groupOrdinal < line.PlayerGroups.Count; groupOrdinal++)
            {
                var players = line.PlayerGroups[groupOrdinal];
                var locks = line.UnitLocks[groupOrdinal];
                for (var slotOrdinal = 0; slotOrdinal < Math.Max(players.Count, locks.Count); slotOrdinal++)
                {
                    context.StoredLineSlots.Add(new StoredLineSlotProjection
                    {
                        LineOrdinal = lineOrdinal,
                        GroupOrdinal = groupOrdinal,
                        SlotOrdinal = slotOrdinal,
                        PlayerInternalId = slotOrdinal < players.Count ? players[slotOrdinal] : null,
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
            context.SetPlays.Add(new SetPlayProjection
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
            context.ModifierCatalogues.Add(new ModifierCatalogueProjection
            {
                RelativePath = relativePath,
                RecordOrdinal = ordinal,
                Data = block.Data.ToArray(),
            });
        }
    }

    internal static PlayerAttributesProjection ToProjection(int ordinal, FhmPlayerAttributes source)
    {
        var result = new PlayerAttributesProjection { RecordOrdinal = ordinal };
        foreach (var target in typeof(PlayerAttributesProjection).GetProperties())
        {
            if (target.Name == nameof(PlayerAttributesProjection.RecordOrdinal))
            {
                continue;
            }

            target.SetValue(result, (int)(byte)typeof(FhmPlayerAttributes).GetProperty(target.Name)!.GetValue(source)!);
        }

        return result;
    }

    internal static GameSettingProjection ToGameSettingProjection(int ordinal, object? value) => value switch
    {
        byte number => new() { SettingOrdinal = ordinal, ValueKind = "Byte", IntegerValue = number },
        ushort number => new() { SettingOrdinal = ordinal, ValueKind = "UInt16", IntegerValue = number },
        int number => new() { SettingOrdinal = ordinal, ValueKind = "Int32", IntegerValue = number },
        double number => new() { SettingOrdinal = ordinal, ValueKind = "Double", RealValue = number },
        string or null => new() { SettingOrdinal = ordinal, ValueKind = "QString", TextValue = (string?)value },
        _ => throw new InvalidDataException($"Unsupported game-settings value type '{value.GetType().Name}'."),
    };

    internal static byte[] SerializeTeamTactics(FhmTeamTacticsSettings tactics)
    {
        using var stream = new MemoryStream();
        tactics.WriteTo(stream);
        return stream.ToArray();
    }

    private sealed record SourceFile(string RelativePath, SaveFileKind Kind, byte[] Content);
}
