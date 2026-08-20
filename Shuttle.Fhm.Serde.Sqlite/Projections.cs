using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Shuttle.Fhm.Serde.Sqlite;

/// <summary>Classifies baseline source content.</summary>
public enum SaveFileKind
{
    /// <summary>A supported file represented by an <c>IFhmSaveFile</c>.</summary>
    Documented,

    /// <summary>An unsupported opaque file retained exactly.</summary>
    Opaque,
}

/// <summary>The singleton adapter schema manifest.</summary>
public sealed class SaveManifest
{
    /// <summary>Gets or sets the singleton identifier, always one.</summary>
    public int Id { get; set; } = 1;

    /// <summary>Gets or sets the adapter database schema version.</summary>
    public int SchemaVersion { get; set; }

    /// <summary>Gets or sets the source save format description.</summary>
    public string SourceFormatVersion { get; set; } = string.Empty;

    /// <summary>Gets or sets the number of baseline files.</summary>
    public int SourceFileCount { get; set; }
}

/// <summary>Exact source bytes for one normalized FHM save-folder file.</summary>
public sealed class SaveFile
{
    /// <summary>Gets or sets the normalized relative path.</summary>
    public string RelativePath { get; set; } = string.Empty;

    /// <summary>Gets or sets the source-file classification.</summary>
    public SaveFileKind Kind { get; set; }

    /// <summary>Gets or sets exact source bytes.</summary>
    public byte[] Content { get; set; } = [];
}

/// <summary>One editable entry from names.dat.</summary>
public sealed class NameProjection
{
    /// <summary>Gets or sets the source collection: Master, FirstName, Surname, ScalarA, or ScalarB.</summary>
    public string CollectionKind { get; set; } = string.Empty;

    /// <summary>Gets or sets a nation index, or -1 for master-name entries.</summary>
    public int NationIndex { get; set; }

    /// <summary>Gets or sets the serialized ordinal within the source collection.</summary>
    public int Ordinal { get; set; }

    /// <summary>Gets or sets the master-name identity.</summary>
    public int NameId { get; set; }

    /// <summary>Gets or sets a name-list reference or scalar value.</summary>
    public int IntegerValue { get; set; }

    /// <summary>Gets or sets master-name text.</summary>
    public string? Text { get; set; }

    /// <summary>Gets or sets the master-name group identity.</summary>
    public int GroupId { get; set; }

    /// <summary>Gets or sets the raw signed master-name category weight.</summary>
    public int CategoryWeight { get; set; }

    /// <summary>Gets or sets the first raw master-name flag.</summary>
    public int FlagA { get; set; }

    /// <summary>Gets or sets the second raw master-name flag.</summary>
    public int FlagB { get; set; }

    /// <summary>Gets or sets the third raw master-name flag.</summary>
    public int FlagC { get; set; }
}

/// <summary>One editable players.dat profile record.</summary>
public sealed class PlayerProjection
{
    /// <summary>Gets or sets the serialized record ordinal.</summary>
    public int RecordOrdinal { get; set; }
    /// <summary>Gets or sets the persisted player internal identity.</summary>
    public int InternalId { get; set; }
    /// <summary>Gets or sets the exported player identity.</summary>
    public int ExternalId { get; set; }
    /// <summary>Gets or sets the first-name reference.</summary>
    public int FirstNameId { get; set; }
    /// <summary>Gets or sets the surname reference.</summary>
    public int SurnameId { get; set; }
    /// <summary>Gets or sets the common-name reference.</summary>
    public int CommonNameId { get; set; }
    /// <summary>Gets or sets the birth year.</summary>
    public int BirthYear { get; set; }
    /// <summary>Gets or sets the birth month.</summary>
    public int BirthMonth { get; set; }
    /// <summary>Gets or sets the birth day.</summary>
    public int BirthDay { get; set; }
    /// <summary>Gets or sets the team reference.</summary>
    public int TeamId { get; set; }
    /// <summary>Gets or sets the franchise reference.</summary>
    public int FranchiseId { get; set; }
    /// <summary>Gets or sets the goalie position rating.</summary>
    public int Goalie { get; set; }
    /// <summary>Gets or sets the left-defence position rating.</summary>
    public int LeftDefenceman { get; set; }
    /// <summary>Gets or sets the right-defence position rating.</summary>
    public int RightDefenceman { get; set; }
    /// <summary>Gets or sets the left-wing position rating.</summary>
    public int LeftWing { get; set; }
    /// <summary>Gets or sets the centre position rating.</summary>
    public int Centre { get; set; }
    /// <summary>Gets or sets the right-wing position rating.</summary>
    public int RightWing { get; set; }
    /// <summary>Gets or sets the opaque original player record framing.</summary>
    public byte[] SerializedRecord { get; set; } = [];
}

/// <summary>The editable 58-value player-rating vector.</summary>
public sealed class PlayerAttributesProjection
{
    /// <summary>Gets or sets the owning player record ordinal.</summary>
    public int RecordOrdinal { get; set; }
    public int BigGames { get; set; }
    public int Consistency { get; set; }
    public int Greed { get; set; }
    public int Adaptability { get; set; }
    public int Loyalty { get; set; }
    public int Coachability { get; set; }
    public int Aging { get; set; }
    public int Sportsmanship { get; set; }
    public int PassShootTendency { get; set; }
    public int Controversy { get; set; }
    public int HandleCritics { get; set; }
    public int HandleFailure { get; set; }
    public int HandleSuccess { get; set; }
    public int Intelligence { get; set; }
    public int Mood { get; set; }
    public int DevRate { get; set; }
    public int Aggression { get; set; }
    public int Bravery { get; set; }
    public int Determination { get; set; }
    public int Teamplayer { get; set; }
    public int Leadership { get; set; }
    public int Temperament { get; set; }
    public int Professionalism { get; set; }
    public int Ambition { get; set; }
    public int Acceleration { get; set; }
    public int Agility { get; set; }
    public int Balance { get; set; }
    public int Speed { get; set; }
    public int Stamina { get; set; }
    public int Strength { get; set; }
    public int Fighting { get; set; }
    public int GoalieReflexes { get; set; }
    public int GoalieStamina { get; set; }
    public int Screening { get; set; }
    public int GettingOpen { get; set; }
    public int Passing { get; set; }
    public int PuckHandling { get; set; }
    public int ShootingAccuracy { get; set; }
    public int ShootingRange { get; set; }
    public int OffensiveRead { get; set; }
    public int Checking { get; set; }
    public int Faceoffs { get; set; }
    public int Hitting { get; set; }
    public int Positioning { get; set; }
    public int ShotBlocking { get; set; }
    public int Stickchecking { get; set; }
    public int DefensiveRead { get; set; }
    public int GoaliePositioning { get; set; }
    public int GoaliePassing { get; set; }
    public int GoaliePokecheck { get; set; }
    public int GoalieBlocker { get; set; }
    public int GoalieGlove { get; set; }
    public int GoalieRebound { get; set; }
    public int GoalieRecovery { get; set; }
    public int GoaliePuckhandling { get; set; }
    public int GoalieLowShots { get; set; }
    public int MentalToughness { get; set; }
    public int GoalieSkating { get; set; }
}

/// <summary>One editable teams.dat top-level record.</summary>
public sealed class TeamProjection
{
    /// <summary>Gets or sets the serialized ordinal.</summary>
    public int RecordOrdinal { get; set; }
    /// <summary>Gets or sets the record's persisted ordinal field.</summary>
    public int RecordIndex { get; set; }
    /// <summary>Gets or sets the stable team identity.</summary>
    public int TeamId { get; set; }
    public string? InternalCode { get; set; }
    public string? InternalCode2 { get; set; }
    public int Flag1 { get; set; }
    public string? City { get; set; }
    public string? Nickname { get; set; }
    public int NicknamePlacement { get; set; }
    public int AffiliateParentId { get; set; }
    public int AffiliateParentId2 { get; set; }
    public int LeagueId { get; set; }
    public int ConferenceId { get; set; }
    public int DivisionId { get; set; }
    public int LocationId { get; set; }
    public int MarketSize { get; set; }
    public int FanLoyalty { get; set; }
    public int Finance1 { get; set; }
    public int Finance2 { get; set; }
    public int Finance3 { get; set; }
    public int Finance4 { get; set; }
}

/// <summary>One type-tagged fixed-order game setting.</summary>
public sealed class GameSettingProjection
{
    /// <summary>Gets or sets the documented setting ordinal.</summary>
    public int SettingOrdinal { get; set; }
    /// <summary>Gets or sets the immutable source type tag.</summary>
    public string ValueKind { get; set; } = string.Empty;
    /// <summary>Gets or sets an integral setting value.</summary>
    public long? IntegerValue { get; set; }
    /// <summary>Gets or sets a real setting value.</summary>
    public double? RealValue { get; set; }
    /// <summary>Gets or sets a string setting value.</summary>
    public string? TextValue { get; set; }
}

/// <summary>One stored-line header.</summary>
public sealed class StoredLineProjection
{
    /// <summary>Gets or sets the serialized line ordinal.</summary>
    public int LineOrdinal { get; set; }
    /// <summary>Gets or sets the line name.</summary>
    public string? Name { get; set; }
}

/// <summary>One stored-line player slot and matching lock value.</summary>
public sealed class StoredLineSlotProjection
{
    /// <summary>Gets or sets the line ordinal.</summary>
    public int LineOrdinal { get; set; }
    /// <summary>Gets or sets the fixed group ordinal.</summary>
    public int GroupOrdinal { get; set; }
    /// <summary>Gets or sets the slot ordinal within the group.</summary>
    public int SlotOrdinal { get; set; }
    /// <summary>Gets or sets the player internal identity.</summary>
    public int? PlayerInternalId { get; set; }
    /// <summary>Gets or sets the raw lock flag.</summary>
    public int? UnitLock { get; set; }
}

/// <summary>One team_tactics.dat catalogue system.</summary>
public sealed class TacticSystemProjection
{
    /// <summary>Gets or sets the serialized record ordinal.</summary>
    public int RecordOrdinal { get; set; }
    /// <summary>Gets or sets the stable global system identity.</summary>
    public int GlobalId { get; set; }
    /// <summary>Gets or sets the raw tactical-zone enum value.</summary>
    public int ZoneGroupRaw { get; set; }
    /// <summary>Gets or sets the display name.</summary>
    public string? Name { get; set; }
    /// <summary>Gets or sets the first rating.</summary>
    public int RatingA { get; set; }
    /// <summary>Gets or sets the second rating.</summary>
    public int RatingB { get; set; }
}

/// <summary>One teams.dat embedded tactical-settings payload.</summary>
public sealed class TeamTacticProjection
{
    /// <summary>Gets or sets the owning team record ordinal.</summary>
    public int TeamRecordOrdinal { get; set; }
    /// <summary>Gets or sets the complete 1,303-byte editable tactical-settings payload.</summary>
    public byte[] SerializedSettings { get; set; } = [];
}

/// <summary>One tactic-related file header.</summary>
public sealed class TacticFileProjection
{
    /// <summary>Gets or sets the normalized source file path.</summary>
    public string RelativePath { get; set; } = string.Empty;
    /// <summary>Gets or sets the projection kind.</summary>
    public string Kind { get; set; } = string.Empty;
    /// <summary>Gets or sets the source version tag.</summary>
    public int Version { get; set; }
    /// <summary>Gets or sets the source declared count when present.</summary>
    public int RecordCount { get; set; }
}

/// <summary>One tactic_templates.dat record.</summary>
public sealed class TacticTemplateProjection
{
    /// <summary>Gets or sets the serialized record ordinal.</summary>
    public int RecordOrdinal { get; set; }
    public string? InternalKey { get; set; }
    public int TemplateIndex { get; set; }
    public string? DisplayName { get; set; }
    /// <summary>Gets or sets the 4,856-byte template settings payload.</summary>
    public byte[] SettingsBlob { get; set; } = [];
}

/// <summary>One set-play formation or trailing record.</summary>
public sealed class SetPlayProjection
{
    /// <summary>Gets or sets the normalized set-play source file path.</summary>
    public string RelativePath { get; set; } = string.Empty;
    /// <summary>Gets or sets whether this is a trailing extra record.</summary>
    public bool IsExtraRecord { get; set; }
    /// <summary>Gets or sets the ordinal within formations or extra records.</summary>
    public int RecordOrdinal { get; set; }
    /// <summary>Gets or sets the opaque payload after its wire length.</summary>
    public byte[] Data { get; set; } = [];
}

/// <summary>One modifier-catalogue block or zone-event grid.</summary>
public sealed class ModifierCatalogueProjection
{
    /// <summary>Gets or sets the normalized source file path.</summary>
    public string RelativePath { get; set; } = string.Empty;
    /// <summary>Gets or sets the serialized record ordinal.</summary>
    public int RecordOrdinal { get; set; }
    /// <summary>Gets or sets the opaque payload after its wire length.</summary>
    public byte[] Data { get; set; } = [];
}

/// <summary>The editable tactics.dat header and opaque record region.</summary>
public sealed class TacticsProjection
{
    /// <summary>Gets or sets the singleton identifier, always one.</summary>
    public int Id { get; set; } = 1;
    /// <summary>Gets or sets the tactics version.</summary>
    public int Version { get; set; }
    /// <summary>Gets or sets the declared tactics record count.</summary>
    public int TacticCount { get; set; }
    /// <summary>Gets or sets exact opaque bytes after the documented header.</summary>
    public byte[] RecordsOpaque { get; set; } = [];
}

public sealed class SaveManifestConfiguration : IEntityTypeConfiguration<SaveManifest>
{
    public void Configure(EntityTypeBuilder<SaveManifest> builder)
    {
        builder.ToTable("SaveManifest");
        builder.HasKey(value => value.Id);
        builder.Property(value => value.Id).ValueGeneratedNever();
        builder.Property(value => value.SourceFormatVersion).IsRequired();
    }
}

public sealed class SaveFileConfiguration : IEntityTypeConfiguration<SaveFile>
{
    public void Configure(EntityTypeBuilder<SaveFile> builder)
    {
        builder.ToTable("SaveFiles");
        builder.HasKey(value => value.RelativePath);
        builder.Property(value => value.RelativePath).UseCollation("NOCASE");
        builder.Property(value => value.Content).IsRequired();
        builder.HasIndex(value => value.RelativePath).IsUnique();
    }
}

public sealed class NameProjectionConfiguration : IEntityTypeConfiguration<NameProjection>
{
    public void Configure(EntityTypeBuilder<NameProjection> builder)
    {
        builder.ToTable("Names");
        builder.HasKey(value => new { value.CollectionKind, value.NationIndex, value.Ordinal });
        builder.Property(value => value.CollectionKind).IsRequired();
    }
}

public sealed class PlayerProjectionConfiguration : IEntityTypeConfiguration<PlayerProjection>
{
    public void Configure(EntityTypeBuilder<PlayerProjection> builder)
    {
        builder.ToTable("Players");
        builder.HasKey(value => value.RecordOrdinal);
        builder.Property(value => value.RecordOrdinal).ValueGeneratedNever();
        builder.Property(value => value.SerializedRecord).IsRequired();
        builder.HasOne<PlayerAttributesProjection>()
            .WithOne()
            .HasForeignKey<PlayerAttributesProjection>(value => value.RecordOrdinal)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class PlayerAttributesProjectionConfiguration : IEntityTypeConfiguration<PlayerAttributesProjection>
{
    public void Configure(EntityTypeBuilder<PlayerAttributesProjection> builder)
    {
        builder.ToTable("PlayerAttributes");
        builder.HasKey(value => value.RecordOrdinal);
        builder.Property(value => value.RecordOrdinal).ValueGeneratedNever();
    }
}

public sealed class TeamProjectionConfiguration : IEntityTypeConfiguration<TeamProjection>
{
    public void Configure(EntityTypeBuilder<TeamProjection> builder)
    {
        builder.ToTable("Teams");
        builder.HasKey(value => value.RecordOrdinal);
        builder.Property(value => value.RecordOrdinal).ValueGeneratedNever();
        builder.HasOne<TeamTacticProjection>()
            .WithOne()
            .HasForeignKey<TeamTacticProjection>(value => value.TeamRecordOrdinal)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class GameSettingProjectionConfiguration : IEntityTypeConfiguration<GameSettingProjection>
{
    public void Configure(EntityTypeBuilder<GameSettingProjection> builder)
    {
        builder.ToTable("GameSettings");
        builder.HasKey(value => value.SettingOrdinal);
        builder.Property(value => value.SettingOrdinal).ValueGeneratedNever();
        builder.Property(value => value.ValueKind).IsRequired();
    }
}

public sealed class StoredLineProjectionConfiguration : IEntityTypeConfiguration<StoredLineProjection>
{
    public void Configure(EntityTypeBuilder<StoredLineProjection> builder)
    {
        builder.ToTable("StoredLines");
        builder.HasKey(value => value.LineOrdinal);
        builder.Property(value => value.LineOrdinal).ValueGeneratedNever();
    }
}

public sealed class StoredLineSlotProjectionConfiguration : IEntityTypeConfiguration<StoredLineSlotProjection>
{
    public void Configure(EntityTypeBuilder<StoredLineSlotProjection> builder)
    {
        builder.ToTable("StoredLineSlots");
        builder.HasKey(value => new { value.LineOrdinal, value.GroupOrdinal, value.SlotOrdinal });
        builder.HasOne<StoredLineProjection>()
            .WithMany()
            .HasForeignKey(value => value.LineOrdinal)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class TacticSystemProjectionConfiguration : IEntityTypeConfiguration<TacticSystemProjection>
{
    public void Configure(EntityTypeBuilder<TacticSystemProjection> builder)
    {
        builder.ToTable("TacticSystems");
        builder.HasKey(value => value.RecordOrdinal);
        builder.Property(value => value.RecordOrdinal).ValueGeneratedNever();
    }
}

public sealed class TeamTacticProjectionConfiguration : IEntityTypeConfiguration<TeamTacticProjection>
{
    public void Configure(EntityTypeBuilder<TeamTacticProjection> builder)
    {
        builder.ToTable("TeamTactics");
        builder.HasKey(value => value.TeamRecordOrdinal);
        builder.Property(value => value.TeamRecordOrdinal).ValueGeneratedNever();
        builder.Property(value => value.SerializedSettings).IsRequired();
    }
}

public sealed class TacticFileProjectionConfiguration : IEntityTypeConfiguration<TacticFileProjection>
{
    public void Configure(EntityTypeBuilder<TacticFileProjection> builder)
    {
        builder.ToTable("TacticFiles");
        builder.HasKey(value => value.RelativePath);
        builder.Property(value => value.RelativePath).UseCollation("NOCASE");
        builder.Property(value => value.Kind).IsRequired();
    }
}

public sealed class TacticTemplateProjectionConfiguration : IEntityTypeConfiguration<TacticTemplateProjection>
{
    public void Configure(EntityTypeBuilder<TacticTemplateProjection> builder)
    {
        builder.ToTable("TacticTemplates");
        builder.HasKey(value => value.RecordOrdinal);
        builder.Property(value => value.RecordOrdinal).ValueGeneratedNever();
        builder.Property(value => value.SettingsBlob).IsRequired();
    }
}

public sealed class SetPlayProjectionConfiguration : IEntityTypeConfiguration<SetPlayProjection>
{
    public void Configure(EntityTypeBuilder<SetPlayProjection> builder)
    {
        builder.ToTable("SetPlays");
        builder.HasKey(value => new { value.RelativePath, value.IsExtraRecord, value.RecordOrdinal });
        builder.Property(value => value.RelativePath).UseCollation("NOCASE");
        builder.Property(value => value.Data).IsRequired();
    }
}

public sealed class ModifierCatalogueProjectionConfiguration : IEntityTypeConfiguration<ModifierCatalogueProjection>
{
    public void Configure(EntityTypeBuilder<ModifierCatalogueProjection> builder)
    {
        builder.ToTable("ModifierCatalogues");
        builder.HasKey(value => new { value.RelativePath, value.RecordOrdinal });
        builder.Property(value => value.RelativePath).UseCollation("NOCASE");
        builder.Property(value => value.Data).IsRequired();
    }
}

public sealed class TacticsProjectionConfiguration : IEntityTypeConfiguration<TacticsProjection>
{
    public void Configure(EntityTypeBuilder<TacticsProjection> builder)
    {
        builder.ToTable("Tactics");
        builder.HasKey(value => value.Id);
        builder.Property(value => value.Id).ValueGeneratedNever();
        builder.Property(value => value.RecordsOpaque).IsRequired();
    }
}
