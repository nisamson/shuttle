using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shuttle.Shl.Api.Models.Common;

namespace Shuttle.EFCore.Entities.Index;

[EntityTypeConfiguration(typeof(GameResultEntityConfiguration))]
public class GameResult : IEntityConvertible<GameResult, Shl.Api.Models.Index.V1.GameResult> {

    [MaxLength(64)] public required string Slug { get; set; }

    public int? GameId { get; set; }
    
    public required int Season { get; set; }
    
    public required int LeagueId { get; set; }

    public required DateOnly SimDate { get; set; }

    public required int HomeTeamId { get; set; }

    public required int AwayTeamId { get; set; }

    public required int HomeScore { get; set; }

    public required int AwayScore { get; set; }

    public required bool Played { get; set; }

    public required bool Overtime { get; set; }

    public required bool Shootout { get; set; }

    public GameType GameType { get; set; }

    public LeagueSeason LeagueSeason { get; set; } = null!;

    public Team HomeTeam { get; set; } = null!;

    public Team AwayTeam { get; set; } = null!;

    public DateTimeOffset? DatePlayed { get; set; }

    public static Expression<Func<GameResult, GameResult, bool>> ChangedExpr => (tgt, src) =>
        tgt.GameId != src.GameId
        || tgt.HomeScore != src.HomeScore
        || tgt.AwayScore != src.AwayScore
        || tgt.Played != src.Played
        || tgt.Overtime != src.Overtime
        || tgt.Shootout != src.Shootout
        || tgt.SimDate != src.SimDate;

    public static GameResult FromModel(Shl.Api.Models.Index.V1.GameResult original) {
        return new() {
            GameId = original.GameId,
            Season = original.Season,
            LeagueId = original.League,
            SimDate = original.Date,
            HomeTeamId = original.HomeTeam,
            AwayTeamId = original.AwayTeam,
            HomeScore = original.HomeScore,
            AwayScore = original.AwayScore,
            GameType = ResolveGameType(original),
            Played = original.Played,
            Overtime = original.Overtime,
            Shootout = original.Shootout,
            Slug = original.Slug
        };
    }

    // The Index API sometimes omits the GameType string, so fall back to the game type
    // encoded in the slug (see Shl.Api.Models.Index.V1.GameSlug) before giving up.
    private static GameType ResolveGameType(Shl.Api.Models.Index.V1.GameResult original) {
        if (GameType.TryFromString(original.GameType, out var fromString)) {
            return fromString;
        }

        if (Shl.Api.Models.Index.V1.GameSlug.TryParse(original.Slug, null, out var slug)) {
            return slug.GameType;
        }

        throw new FormatException(
            $"Cannot determine game type for game '{original.Slug}': " +
            $"GameType string was '{original.GameType ?? "<null>"}' and the slug could not be parsed.");
    }

    public Shl.Api.Models.Index.V1.GameResult ToModel() {
        return new(
            GameId,
            Season,
            LeagueId,
            SimDate,
            HomeTeamId,
            AwayTeamId,
            HomeScore,
            AwayScore,
            GameType.ToDisplayString(),
            Played,
            Overtime,
            Shootout,
            Slug
        );
    }
}

public class GameResultEntityConfiguration : IEntityTypeConfiguration<GameResult> {

    public void Configure(EntityTypeBuilder<GameResult> builder) {
        builder.HasKey(gr => gr.Slug);
        builder.HasIndex(gr => new { gr.LeagueId, gr.SimDate });
        builder.HasIndex(gr => new { gr.LeagueId, gr.GameId })
            .HasFilter($"{nameof(GameResult.GameId)} IS NOT NULL")
            .IsUnique();
        builder.HasOne<Team>(gr => gr.HomeTeam)
            .WithMany()
            .HasForeignKey(gr => new { gr.HomeTeamId, gr.Season, gr.LeagueId })
            .HasPrincipalKey(t => new { t.TeamId, t.Season, t.LeagueId })
            .OnDelete(DeleteBehavior.ClientCascade)
            .IsRequired();
        builder.HasOne<Team>(gr => gr.AwayTeam)
            .WithMany()
            .HasForeignKey(gr => new { gr.AwayTeamId, gr.Season, gr.LeagueId })
            .HasPrincipalKey(t => new { t.TeamId, t.Season, t.LeagueId })
            .OnDelete(DeleteBehavior.ClientCascade)
            .IsRequired();
        builder.HasOne<LeagueSeason>(gr => gr.LeagueSeason)
            .WithMany()
            .HasForeignKey(gr => new { gr.LeagueId, gr.Season })
            .HasPrincipalKey(ls => new { ls.LeagueId, ls.Season })
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();
        builder.Navigation(gr => gr.HomeTeam).AutoInclude();
        builder.Navigation(gr => gr.AwayTeam).AutoInclude();
        builder.Navigation(gr => gr.LeagueSeason).AutoInclude();
    }
}
