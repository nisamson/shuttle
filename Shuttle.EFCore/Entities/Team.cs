using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shuttle.Shl.Api.Models.Index.V1;
using Conference = Shuttle.EFCore.Entities.Index.Conference;
using Division = Shuttle.EFCore.Entities.Index.Division;
using League = Shuttle.EFCore.Entities.Index.League;
using LeagueSeason = Shuttle.EFCore.Entities.Index.LeagueSeason;

namespace Shuttle.EFCore.Entities;

[EntityTypeConfiguration(typeof(TeamEntityConfiguration))]
public record Team : IEntityConvertible<Team, Shl.Api.Models.Index.V1.Team> {

    public int TeamId { get; set; }
    public int Season { get; set; }
    public int LeagueId { get; set; }

    public int? DivisionId { get; set; }

    public int ConferenceId { get; set; }

    [MaxLength(16)] public required string Abbreviation { get; set; }

    [MaxLength(128)] public required string Name { get; set; }

    [MaxLength(128)] public required string Location { get; set; }

    public required TeamColors Colors { get; set; }

    public required NameDetails NameDetails { get; set; }

    public required TeamSeasonStats Stats { get; set; }

    public Division? Division { get; }

    public Conference Conference { get; } = null!;

    public LeagueSeason LeagueSeason => Conference.LeagueSeason;

    public League League => LeagueSeason.League;

    public static Team FromModel(Shl.Api.Models.Index.V1.Team original) {
        return new() {
            TeamId = original.Id,
            Season = original.Season,
            LeagueId = original.League,
            ConferenceId = original.Conference,
            DivisionId = original.Division,
            Name = original.Name,
            Abbreviation = original.Abbreviation,
            Location = original.Location,
            Colors = original.Colors,
            Stats = original.Stats,
            NameDetails = original.NameDetails
        };
    }

    public bool UpdateFromModel(Shl.Api.Models.Index.V1.Team original) {
        var hasChanged = false;
        if (ConferenceId != original.Conference) {
            ConferenceId = original.Conference;
            hasChanged = true;
        }

        if (DivisionId != original.Division) {
            DivisionId = original.Division;
            hasChanged = true;
        }
        
        if (NameDetails != original.NameDetails) {
            NameDetails = original.NameDetails;
            hasChanged = true;
        }

        if (Stats != original.Stats) {
            Stats = original.Stats;
            hasChanged = true;
        }
        
        if (Colors != original.Colors) {
            Colors = original.Colors;
            hasChanged = true;
        }
        
        return hasChanged;
    }

    public Shl.Api.Models.Index.V1.Team ToModel() {
        return new(
            TeamId,
            Season,
            LeagueId,
            ConferenceId,
            DivisionId,
            Name,
            Abbreviation,
            Location,
            Colors,
            NameDetails,
            Stats
        );
    }
}

public class TeamEntityConfiguration : IEntityTypeConfiguration<Team> {

    public void Configure(EntityTypeBuilder<Team> builder) {
        builder.HasKey(t => new { t.TeamId, t.Season, t.LeagueId });
        builder.HasOne<Division>(t => t.Division)
            .WithMany()
            .HasForeignKey(t => new { t.DivisionId, t.Season, t.LeagueId })
            .HasPrincipalKey(d => new { d.DivisionId, d.Season, d.LeagueId })
            .OnDelete(DeleteBehavior.ClientCascade);
        builder.HasOne<Conference>(t => t.Conference)
            .WithMany()
            .HasForeignKey(t => new { t.ConferenceId, t.Season, t.LeagueId })
            .HasPrincipalKey(c => new { c.ConferenceId, c.Season, c.LeagueId })
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();
        builder.HasIndex(t => t.Name);
        builder.HasAlternateKey(t => new { t.Season, t.LeagueId, t.Abbreviation });
        builder.HasIndex(t => t.Location);
        builder.ComplexProperty(
            t => t.Colors,
            b => {
                b.ToJson();
                b.Property(c => c.Primary).HasConversion<ColorValueConverter>();
                b.Property(c => c.Secondary).HasConversion<ColorValueConverter>();
                b.Property(c => c.Text).HasConversion<ColorValueConverter>();
            }
        );
        builder.ComplexProperty(
            t => t.NameDetails,
            b => { b.ToJson(); }
        );
        builder.ComplexProperty(t => t.Stats);
        builder.Navigation(t => t.Division).AutoInclude();
        builder.Navigation(t => t.Conference).AutoInclude();
    }
}

[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.AllProperties)]
public readonly record struct TeamKey(int Team, int Season, int League) {
    public static TeamKey FromShl(Shl.Api.Models.Index.V1.Team team) {
        return new(team.Id, team.Season, team.League);
    }

    public static TeamKey FromEntity(Team team) {
        return new(team.TeamId, team.Season, team.LeagueId);
    }
}