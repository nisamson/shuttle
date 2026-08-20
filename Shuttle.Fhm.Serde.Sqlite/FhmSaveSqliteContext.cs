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

    /// <summary>Gets master-name, name-list, and nation-scalar projections.</summary>
    public DbSet<NameProjection> Names => Set<NameProjection>();

    /// <summary>Gets editable player profile projections.</summary>
    public DbSet<PlayerProjection> Players => Set<PlayerProjection>();

    /// <summary>Gets editable player rating projections.</summary>
    public DbSet<PlayerAttributesProjection> PlayerAttributes => Set<PlayerAttributesProjection>();

    /// <summary>Gets editable team profile projections.</summary>
    public DbSet<TeamProjection> Teams => Set<TeamProjection>();

    /// <summary>Gets editable fixed-order game-setting values.</summary>
    public DbSet<GameSettingProjection> GameSettings => Set<GameSettingProjection>();

    /// <summary>Gets stored-line projections.</summary>
    public DbSet<StoredLineProjection> StoredLines => Set<StoredLineProjection>();

    /// <summary>Gets stored-line player-slot and lock projections.</summary>
    public DbSet<StoredLineSlotProjection> StoredLineSlots => Set<StoredLineSlotProjection>();

    /// <summary>Gets team-tactics catalogue projections.</summary>
    public DbSet<TacticSystemProjection> TacticSystems => Set<TacticSystemProjection>();

    /// <summary>Gets team-owned embedded tactical-settings projections.</summary>
    public DbSet<TeamTacticProjection> TeamTactics => Set<TeamTacticProjection>();

    /// <summary>Gets headers for tactic-related file projections.</summary>
    public DbSet<TacticFileProjection> TacticFiles => Set<TacticFileProjection>();

    /// <summary>Gets editable tactic-template projections.</summary>
    public DbSet<TacticTemplateProjection> TacticTemplates => Set<TacticTemplateProjection>();

    /// <summary>Gets editable set-play formation and trailing-record projections.</summary>
    public DbSet<SetPlayProjection> SetPlays => Set<SetPlayProjection>();

    /// <summary>Gets editable modifier-catalogue blocks and grids.</summary>
    public DbSet<ModifierCatalogueProjection> ModifierCatalogues => Set<ModifierCatalogueProjection>();

    /// <summary>Gets the tactics.dat projection.</summary>
    public DbSet<TacticsProjection> Tactics => Set<TacticsProjection>();

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
