using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Shuttle.EFCore.Entities.Portal;

[EntityTypeConfiguration(typeof(IndexRecordConfiguration))]
public class IndexRecord {
    public required int PlayerId { get; set; }
    public required int UserId { get; set; }
    public required int LeagueId { get; set; }
    public int StartSeason { get; set; }
    
    public PlayerInfo Player { get; set; } = null!;
}

public class IndexRecordConfiguration : IEntityTypeConfiguration<IndexRecord> {
    public void Configure(EntityTypeBuilder<IndexRecord> builder) {
        builder.HasKey(r => new { r.PlayerId, r.LeagueId, r.StartSeason });
        builder.HasOne(r => r.Player)
            .WithMany()
            .HasForeignKey(ir => new {
                ir.PlayerId,
                ir.UserId
            })
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(r => r.UserId);
        builder.AddTemporalTableSupport();
    }
}
