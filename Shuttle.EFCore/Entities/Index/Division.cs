using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Shuttle.EFCore.Entities.Index;

[EntityTypeConfiguration(typeof(DivisionEntityConfiguration))]
public record Division : IEntityConvertible<Division, Shl.Api.Models.Index.V1.Division> {
    public int DivisionId { get; set; }
    public int Season { get; set; }
    public int LeagueId { get; set; }
    public int ConferenceId { get; set; }
    public required string Name { get; set; }

    public Conference Conference { get; } = null!;
    
    public static Division FromModel(Shl.Api.Models.Index.V1.Division original) {
        return new Division {
            DivisionId = original.Id,
            Season = original.Season,
            LeagueId = original.League,
            ConferenceId = original.Conference,
            Name = original.Name
        };
    }
    public Shl.Api.Models.Index.V1.Division ToModel() {
        if (Conference is null) {
            throw new InvalidOperationException("Cannot convert Division to API model when Conference is null.");
        }
        return new(DivisionId, LeagueId, ConferenceId, Name, Season);
    }
    
    public static Expression<Func<Division, Division, bool>> Changed => (t, s) => t.Name != s.Name;
}

public class DivisionEntityConfiguration : IEntityTypeConfiguration<Division> {

    public void Configure(EntityTypeBuilder<Division> builder) {
        builder.HasKey(d => new { Id = d.DivisionId, d.Season, d.LeagueId, d.ConferenceId });
        builder.HasOne<Conference>(d => d.Conference)
            .WithMany()
            .HasForeignKey(d => new { d.ConferenceId, d.LeagueId, d.Season })
            .HasPrincipalKey(c => new { Id = c.ConferenceId, c.LeagueId, c.Season })
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();
    }
}