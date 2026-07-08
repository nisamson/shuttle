using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shuttle.Shl.Api.Models.Common;

namespace Shuttle.EFCore.Entities.Index;

public class IndexPlayer : IEntityConvertible<IndexPlayer, Shl.Api.Models.Index.V1.IndexPlayer> {
    
    public required int PlayerId { get; set; }
    
    [MaxLength(256)]
    public required string Name { get; set; }
    public required int LeagueId { get; set; }
    public required int Season { get; set; }
    
    public required PlayerPosition Position { get; set; }
    public required int Height { get; set; }
    public required int Weight { get; set; }

    public Team Team { get; set; } = null!;

    public Division? Division => Team.Division;

    public Conference Conference { get; set; } = null!;
    
    public LeagueSeason LeagueSeason => Conference.LeagueSeason;
    
    public League League => LeagueSeason.League;
    
    [MaxLength(8)]
    public required string TeamAbbreviation { get; set; }
    
    public static IndexPlayer FromModel(Shl.Api.Models.Index.V1.IndexPlayer original) {
        return new() {
            PlayerId = original.Id,
            Name = original.Name,
            LeagueId = original.League,
            Season = original.Season,
            Position = PlayerPosition.FromString(original.Position),
            Height = original.Height,
            Weight = original.Weight,
            TeamAbbreviation = original.Team
        };
    }
    public Shl.Api.Models.Index.V1.IndexPlayer ToModel() {
        return new(
            PlayerId,
            Name,
            LeagueId,
            Season,
            TeamAbbreviation,
            Position.ToShortString(),
            Height,
            Weight
        );
    }
}

public class IndexPlayerEntityConfiguration : IEntityTypeConfiguration<IndexPlayer> {
    public void Configure(EntityTypeBuilder<IndexPlayer> builder) {
        builder.HasKey(p => new { p.LeagueId, p.Season, p.PlayerId });
        builder.HasOne<Team>(b => b.Team)
            .WithMany()
            .HasForeignKey(p => new { p.TeamAbbreviation, p.Season, p.LeagueId })
            .HasPrincipalKey(t => new { t.Abbreviation, t.Season, t.LeagueId })
            .OnDelete(DeleteBehavior.Cascade);
        builder.AddTemporalTableSupport();
    }
}