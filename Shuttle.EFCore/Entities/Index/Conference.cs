using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Shuttle.EFCore.Entities.Index;

[EntityTypeConfiguration(typeof(ConferenceEntityConfiguration))]
public record Conference : IEntityConvertible<Conference, Shl.Api.Models.Index.V1.Conference> {
    public int ConferenceId { get; set; }
    public int LeagueId { get; set; }
    public int Season { get; set; }
    
    [MaxLength(128)]
    public required string Name { get; set; }
    
    public LeagueSeason LeagueSeason { get; } = null!;
    
    public static Conference FromModel(Shl.Api.Models.Index.V1.Conference original) {
        return new Conference {
            ConferenceId = original.Id,
            LeagueId = original.League,
            Season = original.Season,
            Name = original.Name
        };
    }
    public Shl.Api.Models.Index.V1.Conference ToModel() {
        if (LeagueSeason is null) {
            throw new InvalidOperationException("Cannot convert Conference to API model when LeagueSeason is null.");
        }
        return new(ConferenceId, LeagueSeason.LeagueId, Name, Season);
    }
}

public class ConferenceEntityConfiguration : IEntityTypeConfiguration<Conference> {
    public void Configure(EntityTypeBuilder<Conference> builder) {
        builder.HasKey(c => new { Id = c.ConferenceId, c.LeagueId, c.Season });
        builder.HasOne(c => c.LeagueSeason)
            .WithMany()
            .HasForeignKey(c => new { c.LeagueId, c.Season })
            .HasPrincipalKey(ls => new { ls.LeagueId, ls.Season })
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();
        builder.HasIndex(c => c.Name);
        builder.HasIndex(c => c.Season);
    }
}
