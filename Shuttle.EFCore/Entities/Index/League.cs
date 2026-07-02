using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shuttle.Shl.Api.Models.Common;

namespace Shuttle.EFCore.Entities.Index;

[EntityTypeConfiguration(typeof(LeagueEntityConfiguration))]
public record League : IEntityConvertible<League, Shl.Api.Models.Index.V1.League> {

    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public required int LeagueId { get; set; }

    [MaxLength(128)] public required string Name { get; set; }

    [MaxLength(16)] public required string Abbreviation { get; set; }

    [NotMapped]
    public KnownLeague KnownLeague => KnownLeague.FromAbbreviation(Abbreviation);

    public static League FromModel(Shl.Api.Models.Index.V1.League target) {
        return new League() {
            LeagueId = target.Id,
            Name = target.Name,
            Abbreviation = target.Abbreviation
        };
    }

    public Shl.Api.Models.Index.V1.League ToModel() {
        return new(LeagueId, Name, Abbreviation);
    }
}

[EntityTypeConfiguration(typeof(LeagueSeasonEntityConfiguration))]
public record LeagueSeason : IEntityConvertible<LeagueSeason, Shl.Api.Models.Index.V1.LeagueSeason> {
    public required int LeagueId { get; set; }
    public required int Season { get; set; }
    public League League { get; set; } = null!;

    public KnownLeague KnownLeague => KnownLeague.FromAbbreviation(League.Abbreviation);

    public static LeagueSeason FromModel(Shl.Api.Models.Index.V1.LeagueSeason original) {
        return new LeagueSeason() {
            Season = original.Season,
            LeagueId = original.Id,
        };
    }

    public Shl.Api.Models.Index.V1.LeagueSeason ToModel() {
        if (League is null) {
            throw new InvalidOperationException("Cannot convert LeagueSeason to API model when League is null.");
        }

        return new(LeagueId, League.Name, League.Abbreviation, Season);
    }
}

public class LeagueEntityConfiguration : IEntityTypeConfiguration<League> {

    public void Configure(EntityTypeBuilder<League> builder) {
        builder.HasKey(l => l.LeagueId);
        builder.HasIndex(l => l.Name).IsUnique();
        builder.HasIndex(l => l.Abbreviation).IsUnique();
        builder.HasMany<LeagueSeason>()
            .WithOne(ls => ls.League)
            .HasForeignKey(ls => ls.LeagueId)
            .HasPrincipalKey(ls => ls.LeagueId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();
    }
}

public class LeagueSeasonEntityConfiguration : IEntityTypeConfiguration<LeagueSeason> {
    public void Configure(EntityTypeBuilder<LeagueSeason> builder) {
        builder.HasKey(ls => new { ls.LeagueId, ls.Season });
        builder.HasIndex(ls => ls.Season);
        builder.Navigation(ls => ls.League).AutoInclude();
    }
}
