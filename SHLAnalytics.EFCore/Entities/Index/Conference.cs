using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SHLAnalytics.EFCore.Entities.Index;

public record Conference : IEntityConvertible<Conference, Api.Models.Index.V1.Conference> {
    public int Id { get; set; }
    public int LeagueId { get; set; }
    public int Season { get; set; }
    
    [MaxLength(128)]
    public required string Name { get; set; }
    
    public LeagueSeason? LeagueSeason { get; set; }
    
    public static Conference From(Api.Models.Index.V1.Conference original) {
        return new Conference {
            Id = original.Id,
            LeagueId = original.League,
            Season = original.Season,
            Name = original.Name
        };
    }
    public Api.Models.Index.V1.Conference To() {
        if (LeagueSeason is null) {
            throw new InvalidOperationException("Cannot convert Conference to API model when LeagueSeason is null.");
        }
        return new(Id, LeagueSeason.LeagueId, Name, Season);
    }
}

public class ConferenceEntityConfiguration : IEntityTypeConfiguration<Conference> {
    public void Configure(EntityTypeBuilder<Conference> builder) {
        builder.HasKey(c => new { c.Id, c.LeagueId, c.Season });
        builder.HasOne(c => c.LeagueSeason)
            .WithMany()
            .HasForeignKey(c => new { c.LeagueId, c.Season })
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();
        builder.HasIndex(c => c.Name);
        builder.HasIndex(c => c.Season);
    }
}
