using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Shuttle.Fhm.Serde.Sqlite.Entities;

/// <summary>One tactic_templates.dat record.</summary>
public sealed class TacticTemplate
{
    /// <summary>Gets or sets the serialized record ordinal.</summary>
    public int RecordOrdinal { get; set; }
    public string? InternalKey { get; set; }
    public int TemplateIndex { get; set; }
    public string? DisplayName { get; set; }
    /// <summary>Gets or sets the 4,856-byte template settings payload.</summary>
    public byte[] SettingsBlob { get; set; } = [];
}

public sealed class TacticTemplateConfiguration : IEntityTypeConfiguration<TacticTemplate>
{
    public void Configure(EntityTypeBuilder<TacticTemplate> builder)
    {
        builder.ToTable("TacticTemplates");
        builder.HasKey(value => value.RecordOrdinal);
        builder.Property(value => value.RecordOrdinal).ValueGeneratedNever();
        builder.Property(value => value.SettingsBlob).IsRequired();
    }
}
