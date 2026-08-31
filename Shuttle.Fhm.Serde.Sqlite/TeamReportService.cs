using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Shuttle.Fhm.Serde.Domain.Files;
using Shuttle.Fhm.Serde.Domain.Model;
using Shuttle.Fhm.Serde.Sqlite.Entities;

namespace Shuttle.Fhm.Serde.Sqlite;

/// <summary>Builds human-readable reports for teams in an FHM save SQLite database.</summary>
public interface ITeamReportService
{
    /// <summary>Gets the report for one team selected by its displayed name.</summary>
    Task<TeamReport> GetAsync(string databasePath, string teamName, CancellationToken cancellationToken = default);

    /// <summary>Gets reports for every named team in the database.</summary>
    Task<IReadOnlyList<TeamReport>> GetAllAsync(string databasePath, CancellationToken cancellationToken = default);
}

/// <summary>Builds human-readable reports for teams in an FHM save SQLite database.</summary>
public sealed class TeamReportService : ITeamReportService
{
    /// <inheritdoc />
    public async Task<TeamReport> GetAsync(
        string databasePath,
        string teamName,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(teamName);

        var fullPath = Path.GetFullPath(databasePath);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"SQLite save database '{fullPath}' does not exist.", fullPath);
        }

        await using var context = new FhmSaveSqliteContext(FhmSaveSqliteContext.CreateOptions(fullPath));
        await EnsureAdapterFormatAsync(context, fullPath, cancellationToken);
        var teams = await context.Teams
            .AsNoTracking()
            .Include(team => team.AffiliateParent)
            .Include(team => team.SecondaryAffiliateParent)
            .ToListAsync(cancellationToken);
        var matches = teams
            .Where(team => string.Equals(GetTeamName(team), teamName, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var team = matches.Count switch
        {
            0 => throw new InvalidDataException($"No team named '{teamName}' exists in the SQLite save database."),
            1 => matches[0],
            _ => throw new InvalidDataException($"More than one team named '{teamName}' exists in the SQLite save database."),
        };

        var players = await context.Players
            .AsNoTracking()
            .Where(player => player.TeamRecordOrdinal == team.RecordOrdinal)
            .Include(player => player.TacticalRoleAssignments)
                .ThenInclude(assignment => assignment.Role)
            .Include(player => player.TacticalRoleAssignments)
                .ThenInclude(assignment => assignment.Tendencies)
            .Include(player => player.Contracts)
                .ThenInclude(contract => contract.Years)
            .OrderBy(player => player.Surname!.Text)
            .ThenBy(player => player.FirstName!.Text)
            .ToListAsync(cancellationToken);

        var lineSlots = await context.TeamActiveLineSlots
            .AsNoTracking()
            .Where(slot => slot.TeamRecordOrdinal == team.RecordOrdinal)
            .Include(slot => slot.Player)
            .OrderBy(slot => slot.Group)
            .ThenBy(slot => slot.SlotOrdinal)
            .ToListAsync(cancellationToken);

        var tacticSystems = await context.TacticSystems
            .AsNoTracking()
            .ToDictionaryAsync(system => system.GlobalId, cancellationToken);
        var tactics = ReadTactics(team.Tactics.SerializedSettings);

        return new(
            new(
                GetTeamName(team),
                team.City,
                team.Nickname,
                team.InternalCode,
                team.InternalCode2,
                team.MarketSize,
                team.FanLoyalty,
                [team.Finance1, team.Finance2, team.Finance3, team.Finance4],
                team.AffiliateParent is null ? null : GetTeamName(team.AffiliateParent),
                team.SecondaryAffiliateParent is null ? null : GetTeamName(team.SecondaryAffiliateParent),
                team.Staff
                    .OrderBy(staff => staff.Job)
                    .Select(CreateStaffReport)
                    .ToList()),
            players.Select(CreatePlayerReport).ToList(),
            lineSlots
                .GroupBy(slot => slot.Group)
                .Select(group => new TeamLineReport(
                    ToDisplayName(group.Key),
                    group
                        .Where(slot => slot.Player is not null)
                        .Select(slot => GetPlayerName(slot.Player!))
                        .ToList()))
                .ToList(),
            CreateTacticsReport(tactics, tacticSystems));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TeamReport>> GetAllAsync(
        string databasePath,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);

        var fullPath = Path.GetFullPath(databasePath);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"SQLite save database '{fullPath}' does not exist.", fullPath);
        }

        await using var context = new FhmSaveSqliteContext(FhmSaveSqliteContext.CreateOptions(fullPath));
        await EnsureAdapterFormatAsync(context, fullPath, cancellationToken);
        var teamNames = await context.Teams
            .AsNoTracking()
            .Select(team => new { team.City, team.Nickname })
            .ToListAsync(cancellationToken);

        var reports = new List<TeamReport>();
        foreach (var teamName in teamNames
            .Select(team => GetTeamName(team.City, team.Nickname))
            .Where(name => name.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name, StringComparer.Ordinal))
        {
            reports.Add(await GetAsync(fullPath, teamName, cancellationToken));
        }

        return reports;
    }

    private static async Task EnsureAdapterFormatAsync(
        FhmSaveSqliteContext context,
        string databasePath,
        CancellationToken cancellationToken)
    {
        try
        {
            var manifest = await context.Manifests.AsNoTracking().SingleOrDefaultAsync(cancellationToken);
            if (manifest is null ||
                manifest.Id != 1 ||
                manifest.SchemaVersion != FhmSaveSqliteWriter.SchemaVersion ||
                manifest.SourceFormatVersion != "FHM 10 save folder")
            {
                throw new InvalidDataException($"SQLite database '{databasePath}' is not a supported FHM save adapter database.");
            }
        }
        catch (SqliteException exception)
        {
            throw new InvalidDataException($"SQLite database '{databasePath}' is not an FHM save adapter database.", exception);
        }
    }

    private static TeamStaffReport CreateStaffReport(Personnel staff) =>
        new(
            GetPersonName(staff.FirstName, staff.Surname, staff.Nickname),
            ToDisplayName(staff.Job),
            staff.Reputation,
            staff.Salary,
            new Dictionary<string, int?>(StringComparer.Ordinal)
            {
                ["Negotiating"] = staff.Negotiating,
                ["Player management"] = staff.PlayerManagement,
                ["Coaching defense"] = staff.CoachingDefense,
                ["Coaching forwards"] = staff.CoachingForwards,
                ["Coaching goalies"] = staff.CoachingGoalies,
                ["Coaching prospects"] = staff.CoachingProspects,
                ["Evaluate abilities"] = staff.EvaluateAbilities,
                ["Evaluate potential"] = staff.EvaluatePotential,
                ["Physical training"] = staff.PhysicalTraining,
                ["Tactics"] = staff.Tactics,
                ["In-game tactics"] = staff.IngameTactics,
            });

    private static TeamPlayerReport CreatePlayerReport(Player player) =>
        new(
            GetPlayerName(player),
            player.BirthDate,
            ToDisplayName(player.PrimaryPosition),
            new Dictionary<string, int>(StringComparer.Ordinal)
            {
                ["Goalie"] = player.PositionAffinity.Goalie,
                ["Left defense"] = player.PositionAffinity.LeftDefense,
                ["Right defense"] = player.PositionAffinity.RightDefense,
                ["Left wing"] = player.PositionAffinity.LeftWing,
                ["Center"] = player.PositionAffinity.Center,
                ["Right wing"] = player.PositionAffinity.RightWing,
            },
            ToDisplayName(player.PrimaryContractRole),
            ToDisplayName(player.SupplementaryContractRole),
            GetRatings(player.Attributes),
            player.TacticalRoleAssignments
                .OrderBy(assignment => assignment.Slot)
                .Select(assignment => new PlayerTacticalRoleReport(
                    ToDisplayName(assignment.Slot),
                    assignment.Role.Name ?? ToDisplayName(assignment.Role.RoleId),
                    assignment.Tendencies
                        .OrderBy(tendency => tendency.Tendency)
                        .ToDictionary(
                            tendency => ToDisplayName(tendency.Tendency),
                            tendency => new TendencyReport(tendency.Value, tendency.UseOverride != 0))))
                .ToList(),
            player.Contracts
                .OrderBy(contract => contract.ContractOrdinal)
                .Select(contract => new PlayerContractReport(
                    contract.Years
                        .OrderBy(year => year.YearNumber)
                        .Select(year => new PlayerContractYearReport(
                            year.YearNumber,
                            year.MajorLeagueSalary,
                            year.MinorLeagueSalary))
                        .ToList()))
                .ToList());

    private static IReadOnlyDictionary<string, int> GetRatings(PlayerAttributes attributes) =>
        typeof(PlayerAttributes)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(property => property.PropertyType == typeof(int) && property.Name != nameof(PlayerAttributes.PlayerId))
            .OrderBy(property => property.Name)
            .ToDictionary(
                property => ToDisplayName(property.Name),
                property => (int)property.GetValue(attributes)!,
                StringComparer.Ordinal);

    private static TeamTacticsReport CreateTacticsReport(
        FhmTeamTacticsSettings tactics,
        IReadOnlyDictionary<int, TacticSystem> tacticSystems) =>
        new(
            tactics.TeamValue1,
            tactics.TeamFlag != 0,
            tactics.TeamRating,
            tactics.TeamValues2To4.ToList(),
            tactics.BaseSettings
                .Select((value, index) => new KeyValuePair<string, ushort>($"Base setting {index + 1}", value))
                .ToDictionary(),
            tactics.Selectors
                .Select((selector, index) => new TacticSelectorReport(
                    index == 0 ? "Global settings" : $"Unit settings {index}",
                    selector.SystemIds
                        .Select((systemId, zoneIndex) => new TacticSystemSelectionReport(
                            ToDisplayName((FhmTacticZone)zoneIndex),
                            tacticSystems.TryGetValue(systemId, out var system)
                                ? system.Name ?? "Unnamed tactic system"
                                : "Unknown tactic system"))
                        .ToList(),
                    Describe<FhmOffensiveOrientation>(selector.OffensiveOrientation),
                    Describe<FhmPhysicalOrientation>(selector.PhysicalOrientation),
                    selector.DelayedUseOwnSettingsFlags.Select(flag => flag != 0).ToList(),
                    CreateTendencyReports(tactics.Tendencies[index])))
                .ToList(),
            Describe<FhmOffensiveOrientation>(tactics.FinalOffensiveOrientation),
            Describe<FhmPhysicalOrientation>(tactics.FinalPhysicalOrientation),
            tactics.FinalUseOwnSettingsFlags.Select(flag => flag != 0).ToList());

    private static IReadOnlyDictionary<string, TendencyReport> CreateTendencyReports(FhmTendencyBlock tendencies) =>
        tendencies.Values
            .Select((value, index) => new KeyValuePair<string, TendencyReport>(
                ToDisplayName((FhmTendency)index),
                new(value, tendencies.Overrides[index] != 0)))
            .ToDictionary();

    private static FhmTeamTacticsSettings ReadTactics(byte[] serializedSettings)
    {
        using var stream = new MemoryStream(serializedSettings, writable: false);
        return FhmTeamTacticsSettings.ReadFrom(stream);
    }

    private static string GetTeamName(Team team) => GetTeamName(team.City, team.Nickname);

    private static string GetTeamName(string? city, string? nickname) =>
        string.Join(
            ' ',
            new[] { city, nickname }.Where(value => !string.IsNullOrWhiteSpace(value)))
            .Trim();

    private static string GetPlayerName(Player player) =>
        GetPersonName(player.FirstName, player.Surname, player.CommonName);

    private static string GetPersonName(Name? firstName, Name? surname, Name? commonName) =>
        !string.IsNullOrWhiteSpace(commonName?.Text)
            ? commonName.Text
            : string.Join(
                ' ',
                new[] { firstName?.Text, surname?.Text }.Where(value => !string.IsNullOrWhiteSpace(value)));

    private static string? Describe<TEnum>(FhmEnumValue<TEnum>? value)
        where TEnum : struct, Enum =>
        value is not { } enumValue
            ? null
            : enumValue.IsKnown
                ? ToDisplayName(enumValue.Value)
                : "Unknown";

    private static string ToDisplayName<TEnum>(TEnum value)
        where TEnum : struct, Enum =>
        ToDisplayName(value.ToString());

    private static string ToDisplayName(string value) =>
        Regex.Replace(value, "(?<=[a-z])(?=[A-Z])", " ");
}

/// <summary>A human-readable report of one team and its current setup.</summary>
public sealed record TeamReport(
    TeamDetailsReport Team,
    IReadOnlyList<TeamPlayerReport> Players,
    IReadOnlyList<TeamLineReport> CurrentLines,
    TeamTacticsReport CurrentTactics);

/// <summary>Human-readable team profile data.</summary>
public sealed record TeamDetailsReport(
    string Name,
    string? City,
    string? Nickname,
    string? InternalCode,
    string? AlternateInternalCode,
    int MarketSize,
    int FanLoyalty,
    IReadOnlyList<int> Finances,
    string? AffiliateParent,
    string? SecondaryAffiliateParent,
    IReadOnlyList<TeamStaffReport> Staff);

/// <summary>Human-readable staff data.</summary>
public sealed record TeamStaffReport(
    string Name,
    string Job,
    int Reputation,
    int Salary,
    IReadOnlyDictionary<string, int?> Ratings);

/// <summary>Human-readable player data.</summary>
public sealed record TeamPlayerReport(
    string Name,
    DateOnly BirthDate,
    string PrimaryPosition,
    IReadOnlyDictionary<string, int> PositionRatings,
    string PrimaryContractRole,
    string SquadStatus,
    IReadOnlyDictionary<string, int> Ratings,
    IReadOnlyList<PlayerTacticalRoleReport> TacticalRoles,
    IReadOnlyList<PlayerContractReport> Contracts);

/// <summary>One player tactical role and its tendency settings.</summary>
public sealed record PlayerTacticalRoleReport(
    string Slot,
    string Name,
    IReadOnlyDictionary<string, TendencyReport> Tendencies);

/// <summary>One player contract.</summary>
public sealed record PlayerContractReport(IReadOnlyList<PlayerContractYearReport> Years);

/// <summary>One contract year's salaries.</summary>
public sealed record PlayerContractYearReport(int Year, int? MajorLeagueSalary, int? MinorLeagueSalary);

/// <summary>One current team line.</summary>
public sealed record TeamLineReport(string Group, IReadOnlyList<string> Players);

/// <summary>The decoded current team tactics.</summary>
public sealed record TeamTacticsReport(
    ushort TeamValue,
    bool TeamFlag,
    byte TeamRating,
    IReadOnlyList<int> AdditionalTeamValues,
    IReadOnlyDictionary<string, ushort> BaseSettings,
    IReadOnlyList<TacticSelectorReport> Selectors,
    string? FinalOffensiveOrientation,
    string? FinalPhysicalOrientation,
    IReadOnlyList<bool> FinalUseOwnSettingsFlags);

/// <summary>One global or unit-specific tactical selector.</summary>
public sealed record TacticSelectorReport(
    string Scope,
    IReadOnlyList<TacticSystemSelectionReport> Systems,
    string? OffensiveOrientation,
    string? PhysicalOrientation,
    IReadOnlyList<bool> UseOwnSettingsFlags,
    IReadOnlyDictionary<string, TendencyReport> Tendencies);

/// <summary>One zone's selected tactic system.</summary>
public sealed record TacticSystemSelectionReport(string Zone, string System);

/// <summary>One tactical tendency and whether it overrides its inherited setting.</summary>
public sealed record TendencyReport(int Value, bool OverridesInheritedSetting);
