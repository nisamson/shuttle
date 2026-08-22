using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Shuttle.Fhm.Serde.Sqlite.Entities;

/// <summary>One modifier-catalogue block or zone-event grid.</summary>
public sealed class ModifierCatalogue
{
    /// <summary>Gets or sets the normalized source file path.</summary>
    public string RelativePath { get; set; } = string.Empty;
    /// <summary>Gets or sets the serialized record ordinal.</summary>
    public int RecordOrdinal { get; set; }
    /// <summary>Gets or sets the opaque payload after its wire length.</summary>
    public byte[] Data { get; set; } = [];
}

public sealed class ModifierCatalogueConfiguration : IEntityTypeConfiguration<ModifierCatalogue>
{
    public void Configure(EntityTypeBuilder<ModifierCatalogue> builder)
    {
        builder.ToTable("ModifierCatalogues");
        builder.HasKey(value => new { value.RelativePath, value.RecordOrdinal });
        builder.Property(value => value.RelativePath).UseCollation("NOCASE");
        builder.Property(value => value.Data).IsRequired();
    }
}
