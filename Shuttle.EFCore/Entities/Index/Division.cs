using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Shuttle.EFCore.Entities.Index;

public record Division : IEntityConvertible<Division, Shl.Api.Models.Index.V1.Division> {
    public int Id { get; set; }
    public int Season { get; set; }
    public int LeagueId { get; set; }
    public int ConferenceId { get; set; }
    public required string Name { get; set; }
    
    public Conference? Conference { get; }
    public static Division From(Shl.Api.Models.Index.V1.Division original) {
        return new Division {
            Id = original.Id,
            Season = original.Season,
            LeagueId = original.League,
            ConferenceId = original.Conference,
            Name = original.Name
        };
    }
    public Shl.Api.Models.Index.V1.Division To() {
        if (Conference is null) {
            throw new InvalidOperationException("Cannot convert Division to API model when Conference is null.");
        }
        return new(Id, LeagueId, ConferenceId, Name, Season);
    }
}

public class DivisionEntityConfiguration : IEntityTypeConfiguration<Division> {

    public void Configure(EntityTypeBuilder<Division> builder) {
        builder.HasKey(d => new { d.Id, d.Season, d.LeagueId, d.ConferenceId });
        builder.HasOne<Conference>(d => d.Conference)
            .WithMany()
            .HasForeignKey(d => new { d.ConferenceId, d.LeagueId, d.Season })
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();
    }
}