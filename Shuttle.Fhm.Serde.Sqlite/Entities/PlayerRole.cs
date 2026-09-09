using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Shuttle.Fhm.Serde.Sqlite.Entities;

/// <summary>FHM 10's built-in player tactical roles from <c>player_roles.dat</c>.</summary>
public enum InGameRole
{
    Screener,
    PowerForward,
    Grinder,
    Agitator,
    PerimeterShooter,
    Sniper,
    Playmaker,
    AggressiveForechecker,
    BackcheckingForward,
    Goon,
    Enforcer,
    GarbageCollector,
    Dangler,
    CounterattackingForward,
    Shadow,
    TwoWayForward,
    UpAndDownWinger,
    PlaymakingDefenceman,
    RushingDefenceman,
    StayAtHomeDefenceman,
    CreaseClearingDefenceman,
    Quarterback,
    PunishingDefenceman,
    ShutdownDefenceman,
    TwoWayDefenceman,
    PointShooter,
    SetupMan,
    PunishingForward,
    MobileDefenceman,
    GretzkysOffice,
    OldSchoolDefenceman,
    SpeedyForward,
}

public enum PlayerRoleSlot
{
    Tactical,
    SecondaryTactical,
}

public enum PlayerRoleWeightGroup
{
    A,
    B,
    C,
    D,
    E,
    F,
}

public enum PlayerRoleIndexList
{
    A,
    B,
    C,
    D,
}

public enum PlayerRoleTendency
{
    Attacking,
    Aggressiveness,
    Backchecking,
    Pressure,
    Hitting,
    Tempo,
    Passing,
    Shooting,
    Reserved,
}

public sealed class PlayerRoleCatalogue
{
    public int Id { get; set; } = 1;
    public int VersionTag { get; set; }
}

public sealed class PlayerRoleDefinition
{
    public InGameRole RoleId { get; set; }
    public int RecordOrdinal { get; set; }
    public string? Name { get; set; }
    public int AppliesToForwards { get; set; }
    public int AppliesToDefencemen { get; set; }
    public int AppliesToGoalies { get; set; }
    public int RoleFlags { get; set; }
    public int PositionCategory { get; set; }
    public string? ShortName { get; set; }
    public int TuningValueA { get; set; }
    public int TuningValueB { get; set; }
    public string? Description { get; set; }
    public int TuningValueC { get; set; }
    public ICollection<PlayerRoleWeight> Weights { get; } = [];
    public ICollection<PlayerRoleIndexEntry> IndexEntries { get; } = [];
    public ICollection<PlayerRoleAssignment> PlayerAssignments { get; } = [];
}

public sealed class PlayerRoleWeight
{
    public InGameRole RoleId { get; set; }
    public PlayerRoleWeightGroup Group { get; set; }
    public int Ordinal { get; set; }
    public int Value { get; set; }
    public PlayerRoleDefinition Role { get; set; } = null!;
}

public sealed class PlayerRoleIndexEntry
{
    public InGameRole RoleId { get; set; }
    public PlayerRoleIndexList List { get; set; }
    public int Ordinal { get; set; }
    public int Value { get; set; }
    public PlayerRoleDefinition Role { get; set; } = null!;
}

public sealed class PlayerRoleAssignment
{
    public int PlayerInternalId { get; set; }
    public PlayerRoleSlot Slot { get; set; }
    public InGameRole RoleId { get; set; }
    public Player Player { get; set; } = null!;
    public PlayerRoleDefinition Role { get; set; } = null!;
    public ICollection<PlayerRoleTendencyValue> Tendencies { get; } = [];
}

public sealed class PlayerRoleTendencyValue
{
    public int PlayerInternalId { get; set; }
    public PlayerRoleSlot Slot { get; set; }
    public PlayerRoleTendency Tendency { get; set; }
    public int UseOverride { get; set; }
    public int Value { get; set; }
    public PlayerRoleAssignment Assignment { get; set; } = null!;
}

internal sealed class PlayerRoleCatalogueConfiguration : IEntityTypeConfiguration<PlayerRoleCatalogue>
{
    public void Configure(EntityTypeBuilder<PlayerRoleCatalogue> builder)
    {
        builder.ToTable("TacticalRoleCatalogue");
        builder.HasKey(value => value.Id);
        builder.Property(value => value.Id).ValueGeneratedNever();
    }
}

internal sealed class PlayerRoleDefinitionConfiguration : IEntityTypeConfiguration<PlayerRoleDefinition>
{
    public void Configure(EntityTypeBuilder<PlayerRoleDefinition> builder)
    {
        builder.ToTable(
            "TacticalRoles",
            table =>
            {
                table.HasCheckConstraint("CK_PlayerRoles_AppliesToForwards", "AppliesToForwards BETWEEN 0 AND 255");
                table.HasCheckConstraint("CK_PlayerRoles_AppliesToDefencemen", "AppliesToDefencemen BETWEEN 0 AND 255");
                table.HasCheckConstraint("CK_PlayerRoles_AppliesToGoalies", "AppliesToGoalies BETWEEN 0 AND 255");
                table.HasCheckConstraint("CK_PlayerRoles_RoleFlags", "RoleFlags BETWEEN 0 AND 255");
                table.HasCheckConstraint("CK_PlayerRoles_PositionCategory", "PositionCategory BETWEEN 0 AND 65535");
                table.HasCheckConstraint("CK_PlayerRoles_TuningValueA", "TuningValueA BETWEEN 0 AND 65535");
                table.HasCheckConstraint("CK_PlayerRoles_TuningValueB", "TuningValueB BETWEEN 0 AND 65535");
                table.HasCheckConstraint("CK_PlayerRoles_TuningValueC", "TuningValueC BETWEEN 0 AND 65535");
            });
        builder.HasKey(value => value.RoleId);
        builder.HasIndex(value => value.RecordOrdinal).IsUnique();
        builder.Property(value => value.RoleId).ValueGeneratedNever();
        builder.Property(value => value.RecordOrdinal).ValueGeneratedNever();
        builder.Navigation(value => value.Weights).AutoInclude();
        builder.Navigation(value => value.IndexEntries).AutoInclude();
    }
}

internal sealed class PlayerRoleWeightConfiguration : IEntityTypeConfiguration<PlayerRoleWeight>
{
    public void Configure(EntityTypeBuilder<PlayerRoleWeight> builder)
    {
        builder.ToTable("TacticalRoleWeights");
        builder.HasKey(value => new { value.RoleId, value.Group, value.Ordinal });
        builder.HasOne(value => value.Role)
            .WithMany(value => value.Weights)
            .HasForeignKey(value => value.RoleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class PlayerRoleIndexEntryConfiguration : IEntityTypeConfiguration<PlayerRoleIndexEntry>
{
    public void Configure(EntityTypeBuilder<PlayerRoleIndexEntry> builder)
    {
        builder.ToTable(
            "TacticalRoleIndexEntries",
            table => table.HasCheckConstraint(
                "CK_PlayerRoleIndexEntries_Value",
                "Value BETWEEN 0 AND 255"));
        builder.HasKey(value => new { value.RoleId, value.List, value.Ordinal });
        builder.HasOne(value => value.Role)
            .WithMany(value => value.IndexEntries)
            .HasForeignKey(value => value.RoleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class PlayerRoleAssignmentConfiguration : IEntityTypeConfiguration<PlayerRoleAssignment>
{
    public void Configure(EntityTypeBuilder<PlayerRoleAssignment> builder)
    {
        builder.ToTable("PlayerTacticalRoleAssignments");
        builder.HasKey(value => new { value.PlayerInternalId, value.Slot });
        builder.HasOne(value => value.Player)
            .WithMany(value => value.TacticalRoleAssignments)
            .HasForeignKey(value => value.PlayerInternalId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(value => value.Role)
            .WithMany(value => value.PlayerAssignments)
            .HasForeignKey(value => value.RoleId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(value => value.Tendencies).AutoInclude();
    }
}

internal sealed class PlayerRoleTendencyValueConfiguration : IEntityTypeConfiguration<PlayerRoleTendencyValue>
{
    public void Configure(EntityTypeBuilder<PlayerRoleTendencyValue> builder)
    {
        builder.ToTable(
            "PlayerTacticalRoleTendencyValues",
            table =>
            {
                table.HasCheckConstraint(
                    "CK_PlayerRoleTendencyValues_UseOverride",
                    "UseOverride BETWEEN 0 AND 255");
                table.HasCheckConstraint(
                    "CK_PlayerRoleTendencyValues_Value",
                    "Value BETWEEN 0 AND 65535");
            });
        builder.HasKey(value => new { value.PlayerInternalId, value.Slot, value.Tendency });
        builder.HasOne(value => value.Assignment)
            .WithMany(value => value.Tendencies)
            .HasForeignKey(value => new { value.PlayerInternalId, value.Slot })
            .OnDelete(DeleteBehavior.Cascade);
    }
}
