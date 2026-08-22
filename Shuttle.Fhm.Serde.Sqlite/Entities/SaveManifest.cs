using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Shuttle.Fhm.Serde.Sqlite.Entities;

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
