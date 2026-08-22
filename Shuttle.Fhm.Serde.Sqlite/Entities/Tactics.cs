using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Shuttle.Fhm.Serde.Sqlite.Entities;

/// <summary>The editable tactics.dat header and opaque record region.</summary>
public sealed class Tactics
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

public sealed class TacticsConfiguration : IEntityTypeConfiguration<Tactics>
{
    public void Configure(EntityTypeBuilder<Tactics> builder)
    {
        builder.ToTable("Tactics");
        builder.HasKey(value => value.Id);
        builder.Property(value => value.Id).ValueGeneratedNever();
        builder.Property(value => value.RecordsOpaque).IsRequired();
    }
}
