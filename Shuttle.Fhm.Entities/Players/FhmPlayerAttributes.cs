using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shuttle.Shl.Api.Models.Common.Mixins;

namespace Shuttle.Fhm.Entities.Players;

public sealed class FhmPlayerAttributes : IGoaltenderRatings, IHiddenAttributes, ISkaterRatings {
    public int PlayerId { get; set; }
    
    public int BigGames { get; set; }
    public int Consistency { get; set; }
    public int Greed { get; set; }
    public int Adaptability { get; set; }
    public int Loyalty { get; set; }
    public int Coachability { get; set; }
    public int Aging { get; set; }
    public int Sportsmanship { get; set; }
    public int PassShootTendency { get; set; }
    public int Controversy { get; set; }
    public int HandleCritics { get; set; }
    public int HandleFailure { get; set; }
    public int HandleSuccess { get; set; }
    public int Intelligence { get; set; }
    public int Mood { get; set; }
    public int DevelopmentRate { get; set; }
    public int Aggression { get; set; }
    public int Bravery { get; set; }
    public int Determination { get; set; }
    public int TeamPlayer { get; set; }
    public int Leadership { get; set; }
    public int Temperament { get; set; }
    public int Professionalism { get; set; }
    public int Ambition { get; set; }
    public int Acceleration { get; set; }
    public int Agility { get; set; }
    public int Balance { get; set; }
    public int Speed { get; set; }
    public int Stamina { get; set; }
    public int Strength { get; set; }
    public int Fighting { get; set; }
    public int Screening { get; set; }
    public int GettingOpen { get; set; }
    public int Passing { get; set; }
    public int PokeCheck { get; set; }
    public int Blocker { get; set; }
    public int Glove { get; set; }
    public int Rebound { get; set; }
    public int Recovery { get; set; }
    public int Puckhandling { get; set; }
    public int LowShots { get; set; }
    public int Skating { get; set; }
    public int Reflexes { get; set; }
    public int ShootingAccuracy { get; set; }
    public int ShootingRange { get; set; }
    public int OffensiveRead { get; set; }
    public int Checking { get; set; }
    public int Faceoffs { get; set; }
    public int Hitting { get; set; }
    public int Positioning { get; set; }
    public int ShotBlocking { get; set; }
    public int Stickchecking { get; set; }
    public int DefensiveRead { get; set; }
    public int MentalToughness { get; set; }

    public static FhmPlayerAttributes FromSave(SaveData.Files.FhmPlayerAttributes fhmPlayerAttributes) {
        // TODO: implement me!
        throw new NotImplementedException();
    }
}

public class FhmPlayerAttributesEntityConfiguration : IEntityTypeConfiguration<FhmPlayerAttributes> {
    public void Configure(EntityTypeBuilder<FhmPlayerAttributes> builder) {
        
    }
}