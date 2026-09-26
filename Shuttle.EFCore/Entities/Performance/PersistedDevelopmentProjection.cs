using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shuttle.Models.Players;

namespace Shuttle.EFCore.Entities.Performance;

/// <summary>A serialized player projection belonging to a versioned publication run.</summary>
[EntityTypeConfiguration(typeof(PersistedDevelopmentProjectionConfiguration))]
public sealed class PersistedDevelopmentProjection {
    public required long RunId { get; set; }

    public required int PlayerId { get; set; }

    public required DevelopmentProjectionStatus Status { get; set; }

    public double? ProjectedPeakTpe { get; set; }

    public string? PayloadJson { get; set; }

    public DevelopmentProjectionRun Run { get; set; } = null!;
}

internal sealed class PersistedDevelopmentProjectionConfiguration
    : IEntityTypeConfiguration<PersistedDevelopmentProjection> {
    public void Configure(EntityTypeBuilder<PersistedDevelopmentProjection> builder) {
        builder.HasKey(p => new { p.RunId, p.PlayerId });
        builder.HasIndex(p => p.PlayerId);
        builder.Property(p => p.PayloadJson).HasColumnType("nvarchar(max)");
        builder.HasOne(p => p.Run)
            .WithMany(r => r.Projections)
            .HasForeignKey(p => p.RunId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
