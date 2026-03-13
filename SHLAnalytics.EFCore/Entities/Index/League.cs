using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SHLAnalytics.Api.Models.Common;

namespace SHLAnalytics.EFCore.Entities.Index;

public record League : IEntityConvertible<League, Api.Models.Index.V1.League> {

    public required int Id { get; set; }

    [MaxLength(128)] public required string Name { get; set; }

    [MaxLength(16)] public required string Abbreviation { get; set; }

    public KnownLeague KnownLeague => KnownLeague.FromAbbreviation(Abbreviation);

    public static League From(Api.Models.Index.V1.League target) {
        return new League() {
            Id = target.Id,
            Name = target.Name,
            Abbreviation = target.Abbreviation
        };
    }

    public Api.Models.Index.V1.League To() {
        return new(Id, Name, Abbreviation);
    }
}

public record LeagueSeason : IEntityConvertible<LeagueSeason, Api.Models.Index.V1.LeagueSeason> {
    public int LeagueId { get; set; }
    public int Season { get; set; }
    public League? League { get; set; }

    public KnownLeague KnownLeague =>
        League is not null ? KnownLeague.FromAbbreviation(League.Abbreviation) : KnownLeague.Shl;

    public static LeagueSeason From(Api.Models.Index.V1.LeagueSeason original) {
        return new LeagueSeason() {
            Season = original.Season,
            LeagueId = original.Id,
        };
    }

    public Api.Models.Index.V1.LeagueSeason To() {
        if (League is null) {
            throw new InvalidOperationException("Cannot convert LeagueSeason to API model when League is null.");
        }

        return new(LeagueId, League.Name, League.Abbreviation, Season);
    }
}

public class LeagueEntityConfiguration : IEntityTypeConfiguration<League> {

    public void Configure(EntityTypeBuilder<League> builder) {
        builder.HasKey(l => l.Id);
        builder.HasIndex(l => l.Name).IsUnique();
        builder.HasIndex(l => l.Abbreviation).IsUnique();

        builder.HasMany<LeagueSeason>()
            .WithOne(ls => ls.League)
            .HasForeignKey(ls => ls.LeagueId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();
    }
}

public class LeagueSeasonEntityConfiguration : IEntityTypeConfiguration<LeagueSeason> {
    public void Configure(EntityTypeBuilder<LeagueSeason> builder) {
        builder.HasKey(ls => new { ls.LeagueId, ls.Season });
        builder.HasIndex(ls => ls.Season);
    }
}
