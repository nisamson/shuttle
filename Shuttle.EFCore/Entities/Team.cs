using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shuttle.Shl.Api.Models.Index.V1;
using Conference = Shuttle.EFCore.Entities.Index.Conference;
using Division = Shuttle.EFCore.Entities.Index.Division;
using League = Shuttle.EFCore.Entities.Index.League;

namespace Shuttle.EFCore.Entities;

using Conference = Index.Conference;
using Index_Division = Index.Division;
using League = Index.League;

public record Team : IEntityConvertible<Team, Shl.Api.Models.Index.V1.Team> {

    public Guid Id { get; set; } = Guid.NewGuid();
    
    public int TeamId { get; set; }
    public int Season { get; set; }
    public int LeagueId { get; set; }
    public int? DivisionId { get; set; }
    
    public int ConferenceId { get; set; }
    
    [MaxLength(16)]
    public required string Abbreviation { get; set; }
    
    [MaxLength(128)]
    public required string Name { get; set; }
    
    [MaxLength(128)]
    public required string Location { get; set; }
    
    public required TeamColors Colors { get; set; }
    
    public Index_Division? Division { get; }
    
    public Conference? Conference => Division?.Conference;
    
    public League? League => Conference?.LeagueSeason?.League;
    
    public static Team From(Shl.Api.Models.Index.V1.Team original) {
        return new() {
            TeamId = original.Id,
            Season = original.Season,
            LeagueId = original.League,
            ConferenceId = original.Conference,
            DivisionId = original.Division,
            Name = original.Name,
            Abbreviation = original.Abbreviation,
            Location = original.Location,
            Colors = original.Colors
        };
    }
    
    public Shl.Api.Models.Index.V1.Team To() {
        return new(TeamId, Season, LeagueId, ConferenceId, DivisionId, Name, Abbreviation, Location, Colors);
    }
}

public class TeamEntityConfiguration : IEntityTypeConfiguration<Team> {

    public void Configure(EntityTypeBuilder<Team> builder) {
        builder.HasKey(t => new { Id = t.TeamId, t.Season, t.LeagueId });
        builder.HasOne<Index_Division>(t => t.Division)
            .WithMany()
            .HasForeignKey(t => new { t.DivisionId, t.Season, t.LeagueId })
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();
        builder.HasIndex(t => t.Name);
        builder.HasIndex(t => t.Abbreviation);
        builder.HasIndex(t => t.Location);

        builder.OwnsOne<TeamColors>(
            t => t.Colors,
            on => {
                on.ToJson();
            }
        );
    }
}
