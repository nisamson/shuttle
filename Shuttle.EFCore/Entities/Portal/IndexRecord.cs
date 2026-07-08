using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Shuttle.EFCore.Entities.Portal;

[EntityTypeConfiguration(typeof(IndexRecordConfiguration))]
public class IndexRecord {
    public required int PlayerId { get; set; }
    public required int UserId { get; set; }
    public required int LeagueId { get; set; }
    public required int IndexId { get; set; }
    public required int StartSeason { get; set; }
    public PlayerInformation Player { get; set; } = null!;
}

public class IndexRecordConfiguration : IEntityTypeConfiguration<IndexRecord> {
    public void Configure(EntityTypeBuilder<IndexRecord> builder) {
        builder.HasKey(r => new { r.PlayerId, r.LeagueId, r.StartSeason });
        builder.HasOne(r => r.Player)
            .WithMany(p => p.IndexRecords)
            .HasForeignKey(ir => new {
                ir.PlayerId,
                ir.UserId
            })
            .HasPrincipalKey(p => new {
                p.PlayerId,
                p.UserId
            })
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(r => r.UserId);
        builder.HasIndex(r => r.IndexId);
        builder.AddTemporalTableSupport();
    }
}
