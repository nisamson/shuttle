using Microsoft.EntityFrameworkCore;
using Shuttle.Fhm.Serde.Domain.Files;
using Shuttle.Fhm.Serde.Domain.Model;
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

            var teamIds = save.Files.Values
                .OfType<FhmTeamsFile>()
                .SelectMany(value => value.Teams)
                .Select(value => value.TeamId)
                .ToHashSet();
            var teamIdsByRecordIndex = save.Files.Values
                .OfType<FhmTeamsFile>()
                .SelectMany(value => value.Teams)
                .ToDictionary(value => value.RecordIndex, value => value.TeamId);
            var playerTeamIds = save.Files.Values
                .OfType<FhmPlayersFile>()
                .SelectMany(value => value.Players)
                .ToDictionary(
                    value => value.InternalIdentity,
                    value => ResolveTeamId(value.TeamId, teamIdsByRecordIndex, $"player {value.InternalIdentity}"));

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
                    AddEntities(
                        context,
                        sourceFile.RelativePath,
                        FhmSaveFileFactory.TryRead(sourceFile.RelativePath, sourceFile.Content),
                        playerTeamIds,
                        teamIds,
                        teamIdsByRecordIndex);
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

    private static void AddEntities(
        FhmSaveSqliteContext context,
        string relativePath,
        IFhmSaveFile? file,
        IReadOnlyDictionary<int, int?> playerTeamIds,
        ISet<int> teamIds,
        IReadOnlyDictionary<int, int> teamIdsByRecordIndex)
    {
        switch (file)
        {
            case FhmNamesFile names:
                AddNames(context, names);
                break;
            case FhmPlayersFile players:
                AddPlayers(context, players, teamIdsByRecordIndex);
                break;
            case FhmPlayerRolesFile playerRoles:
                AddPlayerRoles(context, playerRoles);
                break;
            case FhmPersonnelFile personnel:
                AddPersonnel(context, personnel, teamIdsByRecordIndex);
                break;
            case FhmTeamsFile teams:
                AddTeams(context, teams);
                break;
            case FhmGameSettingsFile gameSettings:
                AddGameSettings(context, gameSettings);
                break;
            case FhmStoredLinesFile storedLines:
                AddStoredLines(context, storedLines, playerTeamIds, teamIds);
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
        IReadOnlyDictionary<int, int> teamIdsByRecordIndex)
    {
        foreach (var (player, ordinal) in file.Players.Select((value, index) => (value, index)))
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
                TeamId = ResolveTeamId(player.TeamId, teamIdsByRecordIndex, $"player {player.InternalIdentity}"),
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
    }

    private static void AddPlayerRoles(FhmSaveSqliteContext context, FhmPlayerRolesFile file)
    {
        context.TacticalRoleCatalogues.Add(new PlayerRoleCatalogue { VersionTag = file.VersionTag });
        foreach (var (role, recordOrdinal) in file.Records.Select((value, index) => (value, index)))
        {
            context.TacticalRoles.Add(new PlayerRoleDefinition
            {
                RoleId = role.RoleId,
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
            AddPlayerRoleWeights(context, role.RoleId, PlayerRoleWeightGroup.A, role.WeightGroupA);
            AddPlayerRoleWeights(context, role.RoleId, PlayerRoleWeightGroup.B, role.WeightGroupB);
            AddPlayerRoleWeights(context, role.RoleId, PlayerRoleWeightGroup.C, role.WeightGroupC);
            AddPlayerRoleWeights(context, role.RoleId, PlayerRoleWeightGroup.D, role.WeightGroupD);
            AddPlayerRoleWeights(context, role.RoleId, PlayerRoleWeightGroup.E, role.WeightGroupE);
            AddPlayerRoleWeights(context, role.RoleId, PlayerRoleWeightGroup.F, role.WeightGroupF);
            foreach (var (list, listIndex) in role.IndexLists.Select((value, index) => (value, index)))
            {
                foreach (var (value, ordinal) in list.Select((item, index) => (item, index)))
                {
                    context.TacticalRoleIndexEntries.Add(new PlayerRoleIndexEntry
                    {
                        RoleId = role.RoleId,
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
        int roleId,
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
            RoleId = role.RoleId,
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

    private static void AddPersonnel(
        FhmSaveSqliteContext context,
        FhmPersonnelFile file,
        IReadOnlyDictionary<int, int> teamIdsByRecordIndex)
    {
        foreach (var record in file.Records)
        {
            int? teamId = null;
            if (record.TeamRecordIndex is int teamRecordIndex)
            {
                if (!teamIdsByRecordIndex.TryGetValue(teamRecordIndex, out var resolvedTeamId))
                {
                    throw new InvalidDataException(
                        $"Personnel {record.PersonnelId} references missing team record index {teamRecordIndex}.");
                }

                teamId = resolvedTeamId;
            }

            context.Personnel.Add(new Personnel
            {
                PersonnelId = record.PersonnelId,
                FirstNameNameId = record.FirstNameNameId,
                SurnameNameId = record.SurnameNameId,
                NicknameNameId = record.NicknameNameId,
                BirthDate = new DateOnly(record.BirthDate.Year, record.BirthDate.Month, record.BirthDate.Day),
                NationalityId = record.NationalityId,
                BirthCityId = record.BirthCityId,
                TeamId = teamId,
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
            });
        }
    }

    private static void AddTeams(FhmSaveSqliteContext context, FhmTeamsFile file)
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
                AffiliateParentId = ToNullableReference(team.AffiliateParentId),
                AffiliateParentId2 = ToNullableReference(team.AffiliateParentId2),
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
                TeamId = team.TeamId,
                SerializedSettings = SerializeTeamTactics(team.Tail.Tactics),
            });
            foreach (var line in team.ActiveLines.Lists)
            {
                foreach (var (playerId, slotOrdinal) in line.PlayerReferences.Select((value, index) => (value, index)))
                {
                    context.TeamActiveLineSlots.Add(new TeamActiveLineSlot
                    {
                        TeamId = team.TeamId,
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
        IReadOnlyDictionary<int, int?> playerTeamIds,
        ISet<int> teamIds)
    {
        foreach (var (line, lineOrdinal) in file.StoredLines.Select((value, index) => (value, index)))
        {
            var teamId = line.PlayerGroups
                .SelectMany(value => value)
                .Where(value => value != FhmNullConstants.Null)
                .Select(value => playerTeamIds.GetValueOrDefault(value))
                .Where(value => value.HasValue && teamIds.Contains(value.Value))
                .Select(value => value!.Value)
                .Distinct()
                .ToArray();
            context.StoredLines.Add(new StoredLine
            {
                LineOrdinal = lineOrdinal,
                Name = line.Name,
                TeamId = teamId.Length == 1 ? teamId[0] : null,
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

    private static int? ResolveTeamId(
        int teamRecordIndex,
        IReadOnlyDictionary<int, int> teamIdsByRecordIndex,
        string owner)
    {
        if (teamRecordIndex == FhmNullConstants.Null)
        {
            return null;
        }

        if (!teamIdsByRecordIndex.TryGetValue(teamRecordIndex, out var teamId))
        {
            throw new InvalidDataException($"{owner} references missing team record index {teamRecordIndex}.");
        }

        return teamId;
    }

    private sealed record SourceFile(string RelativePath, SaveFileKind Kind, byte[] Content);
}
