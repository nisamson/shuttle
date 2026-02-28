using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SHLAnalytics.Api.Models.Common.Mixins;

namespace SHLAnalytics.EFCore.Entities;

public class SkaterRatings : ISkaterRatings, IEntityConvertible<SkaterRatings, Api.Models.Index.V1.SkaterRatings> {
    public required int PlayerId { get; set; }
    public required int Season { get; set; }
    public required int LeagueId { get; set; }
    public required int Screening { get; set; }
    public required int GettingOpen { get; set; }
    public required int Passing { get; set; }
    public required int Puckhandling { get; set; }
    public required int ShootingAccuracy { get; set; }
    public required int ShootingRange { get; set; }
    public required int OffensiveRead { get; set; }
    public required int Agression { get; set; }
    public required int Bravery { get; set; }
    public required int Determination { get; set; }
    public required int TeamPlayer { get; set; }
    public required int Leadership { get; set; }
    public required int Temperament { get; set; }
    public required int Professionalism { get; set; }
    public required int Checking { get; set; }
    public required int Stickchecking { get; set; }
    public required int Hitting { get; set; }
    public required int Positioning { get; set; }
    public required int ShotBlocking { get; set; }
    public required int StickChecking { get; set; }
    public required int DefensiveRead { get; set; }
    public required int Acceleration { get; set; }
    public required int Agility { get; set; }
    public required int Balance { get; set; }
    public required int Speed { get; set; }
    public required int Stamina { get; set; }
    public required int Strength { get; set; }
    public required int Fighting { get; set; }
    public required int AppliedTpe { get; set; }
    
    public Player? Player { get; }
    public LeagueSeason? LeagueSeason { get; }
    
    public static SkaterRatings From(Api.Models.Index.V1.SkaterRatings original) {
        return new() {
            PlayerId = original.Id,
            Season = original.Season,
            LeagueId = original.League,
            Screening = original.Screening,
            GettingOpen = original.GettingOpen,
            Passing = original.Passing,
            Puckhandling = original.Puckhandling,
            ShootingAccuracy = original.ShootingAccuracy,
            ShootingRange = original.ShootingRange,
            OffensiveRead = original.OffensiveRead,
            Agression = original.Agression,
            Bravery = original.Bravery,
            Determination = original.Determination,
            TeamPlayer = original.TeamPlayer,
            Leadership = original.Leadership,
            Temperament = original.Temperament,
            Professionalism = original.Professionalism,
            Checking = original.Checking,
            Stickchecking = original.Stickchecking,
            Hitting = original.Hitting,
            Positioning = original.Positioning,
            ShotBlocking = original.ShotBlocking,
            StickChecking = original.StickChecking,
            DefensiveRead = original.DefensiveRead,
            Acceleration = original.Acceleration,
            Agility = original.Agility,
            Balance = original.Balance,
            Speed = original.Speed,
            Stamina = original.Stamina,
            Strength = original.Strength,
            Fighting = original.Fighting,
            AppliedTpe = original.AppliedTpe
        };
    }
    public Api.Models.Index.V1.SkaterRatings To() {
        return new(
            PlayerId,
            LeagueId,
            Season,
            Screening,
            GettingOpen,
            Passing,
            Puckhandling,
            ShootingAccuracy,
            ShootingRange,
            OffensiveRead,
            Agression,
            Bravery,
            Determination,
            TeamPlayer,
            Leadership,
            Temperament,
            Professionalism,
            Checking,
            Stickchecking,
            Hitting,
            Positioning,
            ShotBlocking,
            StickChecking,
            DefensiveRead,
            Acceleration,
            Agility,
            Balance,
            Speed,
            Stamina,
            Strength,
            Fighting,
            AppliedTpe
            );
    }

    /// <summary>
    /// Updates the current entity with values from the provided model.
    /// </summary>
    /// <param name="ratings"></param>
    /// <returns></returns>
    public bool UpdateFrom(Api.Models.Index.V1.SkaterRatings ratings) {
        if (!(this as ISkaterRatings).RatingsChanged(ratings)) {
            return false;
        }
        
        Screening = ratings.Screening;
        GettingOpen = ratings.GettingOpen;
        Passing = ratings.Passing;
        Puckhandling = ratings.Puckhandling;
        ShootingAccuracy = ratings.ShootingAccuracy;
        ShootingRange = ratings.ShootingRange;
        OffensiveRead = ratings.OffensiveRead;
        Agression = ratings.Agression;
        Bravery = ratings.Bravery;
        Determination = ratings.Determination;
        TeamPlayer = ratings.TeamPlayer;
        Leadership = ratings.Leadership;
        Temperament = ratings.Temperament;
        Professionalism = ratings.Professionalism;
        Checking = ratings.Checking;
        Stickchecking = ratings.Stickchecking;
        Hitting = ratings.Hitting;
        Positioning = ratings.Positioning;
        ShotBlocking = ratings.ShotBlocking;
        StickChecking = ratings.StickChecking;
        DefensiveRead = ratings.DefensiveRead;
        Acceleration = ratings.Acceleration;
        Agility = ratings.Agility;
        Balance = ratings.Balance;
        Speed = ratings.Speed;
        Stamina = ratings.Stamina;
        Strength = ratings.Strength;
        Fighting = ratings.Fighting;
        AppliedTpe = ratings.AppliedTpe;
        
        return true;
    }
}

public class SkaterRatingsEntityConfiguration : IEntityTypeConfiguration<SkaterRatings> {
    public void Configure(EntityTypeBuilder<SkaterRatings> builder) {
        builder.AddTemporalTableSupport();
        builder.HasKey(r => new { r.PlayerId, r.Season, r.LeagueId });
        builder.HasIndex(r => new { r.PlayerId, ValidFrom = EF.Property<DateTime>(r, Constants.ValidFrom) });
        builder.HasIndex(r => new { r.PlayerId, r.Season, ValidFrom = EF.Property<DateTime>(r, Constants.ValidFrom) });
        builder.HasOne<Player>()
            .WithMany()
            .HasForeignKey(p => p.PlayerId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();
        builder.HasOne<LeagueSeason>(p => p.LeagueSeason)
            .WithMany()
            .HasForeignKey(p => new { p.LeagueId, p.Season })
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();
    }
}
