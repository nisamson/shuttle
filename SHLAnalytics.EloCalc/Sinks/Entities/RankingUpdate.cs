using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SHLAnalytics.Math;

namespace SHLAnalytics.EloCalc.Sinks.Entities;

public record RankingUpdate {

    public RankingUpdate() {}

    public RankingUpdate(int teamId, Player player) {
        TeamId = teamId;
        Rating = player.Rating.Value;
        GamesPlayed = player.GamesPlayed;
    }

    public int GamesPlayed { get; set; }

    public int Rating { get; set; }

    public int TeamId { get; set; }
    
    public int Season { get; set; }

    public Team Team { get; set; } = null!;
}

public class RankingUpdateEntityConfiguration : IEntityTypeConfiguration<RankingUpdate> {

    public void Configure(EntityTypeBuilder<RankingUpdate> builder) {
        builder.HasKey(r => new { r.TeamId, r.GamesPlayed, r.Season });
        builder.HasOne(r => r.Team)
            .WithMany()
            .HasForeignKey(r => r.TeamId);
    }
}
