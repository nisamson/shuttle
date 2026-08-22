using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Shuttle.Fhm.Serde.Sqlite.Entities;

/// <summary>One team_tactics.dat catalogue system.</summary>
public sealed class TacticSystem
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

public sealed class TacticSystemConfiguration : IEntityTypeConfiguration<TacticSystem>
{
    public void Configure(EntityTypeBuilder<TacticSystem> builder)
    {
        builder.ToTable("TacticSystems");
        builder.HasKey(value => value.RecordOrdinal);
        builder.Property(value => value.RecordOrdinal).ValueGeneratedNever();
    }
}
