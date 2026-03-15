using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shuttle.Shl.Api.Models.Common;

namespace Shuttle.EFCore.Entities.Index;

public class GameResult : IEntityConvertible<GameResult, Shl.Api.Models.Index.V1.GameResult> {

    public required int GameId { get; set; }
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

    public LeagueSeason? LeagueSeason { get; }
    public Team? HomeTeam { get; }
    public Team? AwayTeam { get; }

    public DateTimeOffset? DatePlayed { get; set; }

    public static GameResult From(Shl.Api.Models.Index.V1.GameResult original) {
        return new() {
            GameId = original.GameId,
            Season = original.Season,
            LeagueId = original.League,
            SimDate = original.Date,
            HomeTeamId = original.HomeTeam,
            AwayTeamId = original.AwayTeam,
            HomeScore = original.HomeScore,
            AwayScore = original.AwayScore,
            GameType = GameType.FromString(original.GameType),
            Played = original.Played,
            Overtime = original.Overtime,
            Shootout = original.Shootout
        };
    }

    public Shl.Api.Models.Index.V1.GameResult To() {
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
                Shootout
            );
    }

    public void UpdateFrom(Shl.Api.Models.Index.V1.GameResult target) {
        HomeScore = target.HomeScore;
        AwayScore = target.AwayScore;
        Played = target.Played;
        Overtime = target.Overtime;
        Shootout = target.Shootout;
        if (target.Played && !DatePlayed.HasValue) {
            DatePlayed = DateTimeOffset.UtcNow;
        }
    }
}

public class GameResultEntityConfiguration : IEntityTypeConfiguration<GameResult> {

    public void Configure(EntityTypeBuilder<GameResult> builder) {
        builder.HasKey(gr => new { gr.LeagueId, gr.GameId });
        builder.HasIndex(gr => new { gr.LeagueId, gr.SimDate });
        builder.HasOne<Team>(gr => gr.HomeTeam)
            .WithMany()
            .HasForeignKey(gr => new { gr.HomeTeamId, gr.Season, gr.LeagueId })
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();
        builder.HasOne<Team>(gr => gr.AwayTeam)
            .WithMany()
            .HasForeignKey(gr => new { gr.AwayTeamId, gr.Season, gr.LeagueId })
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();
    }
}
