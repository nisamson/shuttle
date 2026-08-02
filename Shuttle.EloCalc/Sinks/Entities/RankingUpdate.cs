using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shuttle.Math;

namespace Shuttle.EloCalc.Sinks.Entities;

[EntityTypeConfiguration(typeof(RankingUpdateEntityConfiguration))]
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
        builder.HasKey(r => new { r.TeamId, r.Season, r.GamesPlayed });
        builder.HasOne(r => r.Team)
            .WithMany()
            .HasForeignKey(r => new { r.TeamId, r.Season })
            .HasPrincipalKey(t => new { t.TeamId, t.Season })
            .OnDelete(DeleteBehavior.Cascade);
    }
}
