using Microsoft.EntityFrameworkCore;

namespace Shuttle.Fhm.Serde.Sqlite;

/// <summary>The EF Core model for a lossless, editable FHM 10 save-folder database.</summary>
public sealed class FhmSaveSqliteContext : DbContext
{
    /// <summary>Initializes a context using caller-supplied options.</summary>
    public FhmSaveSqliteContext(DbContextOptions<FhmSaveSqliteContext> options)
        : base(options)
    {
    }

    /// <summary>Gets the singleton schema and source manifest.</summary>
    public DbSet<SaveManifest> Manifests => Set<SaveManifest>();

    /// <summary>Gets exact baseline content for every source file.</summary>
    public DbSet<SaveFile> Files => Set<SaveFile>();

    /// <summary>Gets editable master names.</summary>
    public DbSet<Name> Names => Set<Name>();
    /// <summary>Gets nation-specific first-name and surname list entries.</summary>
    public DbSet<NameListEntry> NameListEntries => Set<NameListEntry>();
    /// <summary>Gets nation-indexed name scalars.</summary>
    public DbSet<NameScalar> NameScalars => Set<NameScalar>();

    /// <summary>Gets editable player profile entities.</summary>
    public DbSet<Player> Players => Set<Player>();

    /// <summary>Gets editable player rating entities.</summary>
    public DbSet<PlayerAttributes> PlayerAttributes => Set<PlayerAttributes>();

    /// <summary>Gets editable player contract entities.</summary>
    public DbSet<PlayerContract> PlayerContracts => Set<PlayerContract>();

    /// <summary>Gets editable annual player contract salary entities.</summary>
    public DbSet<PlayerContractYear> PlayerContractYears => Set<PlayerContractYear>();

    public DbSet<PlayerRoleCatalogue> TacticalRoleCatalogues => Set<PlayerRoleCatalogue>();
    public DbSet<PlayerRoleDefinition> TacticalRoles => Set<PlayerRoleDefinition>();
    public DbSet<PlayerRoleWeight> TacticalRoleWeights => Set<PlayerRoleWeight>();
    public DbSet<PlayerRoleIndexEntry> TacticalRoleIndexEntries => Set<PlayerRoleIndexEntry>();
    public DbSet<PlayerRoleAssignment> PlayerTacticalRoleAssignments => Set<PlayerRoleAssignment>();
    public DbSet<PlayerRoleTendencyValue> PlayerTacticalRoleTendencyValues => Set<PlayerRoleTendencyValue>();

    /// <summary>Gets editable staff entities.</summary>
    public DbSet<Personnel> Personnel => Set<Personnel>();

    /// <summary>Gets editable team profile entities.</summary>
    public DbSet<Team> Teams => Set<Team>();

    /// <summary>Gets editable fixed-order game-setting values.</summary>
    public DbSet<GameSetting> GameSettings => Set<GameSetting>();

    /// <summary>Gets stored-line entities.</summary>
    public DbSet<StoredLine> StoredLines => Set<StoredLine>();

    /// <summary>Gets stored-line player-slot and lock entities.</summary>
    public DbSet<StoredLineSlot> StoredLineSlots => Set<StoredLineSlot>();

    /// <summary>Gets team-tactics catalogue entities.</summary>
    public DbSet<TacticSystem> TacticSystems => Set<TacticSystem>();

    /// <summary>Gets team-owned embedded tactical-settings entities.</summary>
    public DbSet<TeamTactic> TeamTactics => Set<TeamTactic>();

    /// <summary>Gets player slots from each team's active game lineup.</summary>
    public DbSet<TeamActiveLineSlot> TeamActiveLineSlots => Set<TeamActiveLineSlot>();

    /// <summary>Gets headers for tactic-related file entities.</summary>
    public DbSet<TacticFile> TacticFiles => Set<TacticFile>();

    /// <summary>Gets editable tactic-template entities.</summary>
    public DbSet<TacticTemplate> TacticTemplates => Set<TacticTemplate>();

    /// <summary>Gets editable set-play formation and trailing-record entities.</summary>
    public DbSet<SetPlay> SetPlays => Set<SetPlay>();

    /// <summary>Gets editable modifier-catalogue blocks and grids.</summary>
    public DbSet<ModifierCatalogue> ModifierCatalogues => Set<ModifierCatalogue>();

    /// <summary>Gets the tactics.dat entity.</summary>
    public DbSet<Tactics> Tactics => Set<Tactics>();

    /// <summary>Builds SQLite options for a database file.</summary>
    public static DbContextOptions<FhmSaveSqliteContext> CreateOptions(string databasePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);
        return new DbContextOptionsBuilder<FhmSaveSqliteContext>()
            .UseSqlite($"Data Source={Path.GetFullPath(databasePath)};Pooling=False")
            .Options;
    }

    /// <summary>Opens a database after applying every available schema migration.</summary>
    public static async Task<FhmSaveSqliteContext> OpenAsync(
        string databasePath,
        CancellationToken cancellationToken = default)
    {
        var context = new FhmSaveSqliteContext(CreateOptions(databasePath));
        try
        {
            await context.Database.MigrateAsync(cancellationToken);
            return context;
        }
        catch
        {
            await context.DisposeAsync();
            throw;
        }
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FhmSaveSqliteContext).Assembly);
    }
}
