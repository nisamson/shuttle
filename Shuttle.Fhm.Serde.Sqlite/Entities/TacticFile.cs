using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Shuttle.Fhm.Serde.Sqlite.Entities;

/// <summary>Classifies a tactic-related source file.</summary>
public enum TacticFileKind
{
    TacticSystems,
    TacticTemplates,
    SetPlay,
    ModifierCatalogue,
    ZoneEventModifiers,
}

/// <summary>One tactic-related file header.</summary>
public sealed class TacticFile
{
    /// <summary>Gets or sets the normalized source file path.</summary>
    public string RelativePath { get; set; } = string.Empty;
    /// <summary>Gets or sets the entity kind.</summary>
    public TacticFileKind Kind { get; set; }
    /// <summary>Gets or sets the source version tag.</summary>
    public int Version { get; set; }
    /// <summary>Gets or sets the source declared count when present.</summary>
    public int RecordCount { get; set; }
}

public sealed class TacticFileConfiguration : IEntityTypeConfiguration<TacticFile>
{
    public void Configure(EntityTypeBuilder<TacticFile> builder)
    {
        builder.ToTable("TacticFiles");
        builder.HasKey(value => value.RelativePath);
        builder.Property(value => value.RelativePath).UseCollation("NOCASE");
        builder.Property(value => value.Kind).HasConversion<string>().IsRequired();
    }
}
