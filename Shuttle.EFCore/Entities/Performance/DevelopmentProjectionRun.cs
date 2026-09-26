using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Shuttle.EFCore.Entities.Performance;

/// <summary>A versioned, atomically published batch of player development projections.</summary>
[EntityTypeConfiguration(typeof(DevelopmentProjectionRunConfiguration))]
public sealed class DevelopmentProjectionRun {
    public long Id { get; set; }

    public required int AlgorithmVersion { get; set; }

    public required DateTimeOffset DataAsOf { get; set; }

    public required DateTimeOffset GeneratedAt { get; set; }

    public DateTimeOffset? PublishedAt { get; set; }

    public IList<PersistedDevelopmentProjection> Projections { get; set; } = [];
}

internal sealed class DevelopmentProjectionRunConfiguration
    : IEntityTypeConfiguration<DevelopmentProjectionRun> {
    public void Configure(EntityTypeBuilder<DevelopmentProjectionRun> builder) {
        builder.HasKey(r => r.Id);
        builder.HasIndex(r => new { r.AlgorithmVersion, r.DataAsOf }).IsUnique();
        builder.HasIndex(r => new { r.AlgorithmVersion, r.PublishedAt });
    }
}
