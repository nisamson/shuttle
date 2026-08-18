using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shuttle.Shl.Api.Models.Common.Mixins;
using SaveAttributes = Shuttle.Fhm.SaveData.Files.FhmPlayerAttributes;

namespace Shuttle.Fhm.Entities.Players;

public sealed class FhmPlayerAttributes : IGoaltenderRatings, IHiddenAttributes, ISkaterRatings {
    public int InternalId { get; set; }
    
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
    public int GoalieReflexes { get; set; }
    public int GoalieStamina { get; set; }
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
    public int GoaliePositioning { get; set; }
    public int GoaliePassing { get; set; }
    public int GoaliePokeCheck { get; set; }
    public int GoalieBlocker { get; set; }
    public int GoalieGlove { get; set; }
    public int GoalieRebound { get; set; }
    public int GoalieRecovery { get; set; }
    public int GoaliePuckhandling { get; set; }
    public int GoalieLowShots { get; set; }
    public int MentalToughness { get; set; }
    public int GoalieSkating { get; set; }

    public int Reflexes {
        get => GoalieReflexes;
        set => GoalieReflexes = value;
    }

    public int Skating {
        get => GoalieSkating;
        set => GoalieSkating = value;
    }

    int IGoaltendingMentalRatings.Stamina => GoalieStamina;
    int IGoaltendingMentalRatings.Aggression => Aggression;
    int IGoaltendingMentalRatings.Determination => Determination;
    int IGoaltendingMentalRatings.TeamPlayer => TeamPlayer;
    int IGoaltendingMentalRatings.Leadership => Leadership;
    int IGoaltendingMentalRatings.Professionalism => Professionalism;
    int IGoaltendingMentalRatings.MentalToughness => MentalToughness;
    int IGoaltendingTechniqueRatings.Positioning => GoaliePositioning;
    int IGoaltendingTechniqueRatings.Passing => GoaliePassing;
    int IGoaltendingTechniqueRatings.PokeCheck => GoaliePokeCheck;
    int IGoaltendingTechniqueRatings.Blocker => GoalieBlocker;
    int IGoaltendingTechniqueRatings.Glove => GoalieGlove;
    int IGoaltendingTechniqueRatings.Rebound => GoalieRebound;
    int IGoaltendingTechniqueRatings.Recovery => GoalieRecovery;
    int IGoaltendingTechniqueRatings.Puckhandling => GoaliePuckhandling;
    int IGoaltendingTechniqueRatings.LowShots => GoalieLowShots;
    int IGoaltendingTechniqueRatings.Skating => GoalieSkating;
    int IGoaltendingTechniqueRatings.Reflexes => GoalieReflexes;

    public static FhmPlayerAttributes FromSave(SaveAttributes source) {
        ArgumentNullException.ThrowIfNull(source);
        return new FhmPlayerAttributes {
            BigGames = source.BigGames,
            Consistency = source.Consistency,
            Greed = source.Greed,
            Adaptability = source.Adaptability,
            Loyalty = source.Loyalty,
            Coachability = source.Coachability,
            Aging = source.Aging,
            Sportsmanship = source.Sportsmanship,
            PassShootTendency = source.PassShootTendency,
            Controversy = source.Controversy,
            HandleCritics = source.HandleCritics,
            HandleFailure = source.HandleFailure,
            HandleSuccess = source.HandleSuccess,
            Intelligence = source.Intelligence,
            Mood = source.Mood,
            DevelopmentRate = source.DevRate,
            Aggression = source.Aggression,
            Bravery = source.Bravery,
            Determination = source.Determination,
            TeamPlayer = source.Teamplayer,
            Leadership = source.Leadership,
            Temperament = source.Temperament,
            Professionalism = source.Professionalism,
            Ambition = source.Ambition,
            Acceleration = source.Acceleration,
            Agility = source.Agility,
            Balance = source.Balance,
            Speed = source.Speed,
            Stamina = source.Stamina,
            Strength = source.Strength,
            Fighting = source.Fighting,
            GoalieReflexes = source.GoalieReflexes,
            GoalieStamina = source.GoalieStamina,
            Screening = source.Screening,
            GettingOpen = source.GettingOpen,
            Passing = source.Passing,
            Puckhandling = source.PuckHandling,
            ShootingAccuracy = source.ShootingAccuracy,
            ShootingRange = source.ShootingRange,
            OffensiveRead = source.OffensiveRead,
            Checking = source.Checking,
            Faceoffs = source.Faceoffs,
            Hitting = source.Hitting,
            Positioning = source.Positioning,
            ShotBlocking = source.ShotBlocking,
            Stickchecking = source.Stickchecking,
            DefensiveRead = source.DefensiveRead,
            GoaliePositioning = source.GoaliePositioning,
            GoaliePassing = source.GoaliePassing,
            GoaliePokeCheck = source.GoaliePokecheck,
            GoalieBlocker = source.GoalieBlocker,
            GoalieGlove = source.GoalieGlove,
            GoalieRebound = source.GoalieRebound,
            GoalieRecovery = source.GoalieRecovery,
            GoaliePuckhandling = source.GoaliePuckhandling,
            GoalieLowShots = source.GoalieLowShots,
            MentalToughness = source.MentalToughness,
            GoalieSkating = source.GoalieSkating,
        };
    }
}

public sealed class FhmPlayerAttributesEntityConfiguration : IEntityTypeConfiguration<FhmPlayerAttributes> {
    public void Configure(EntityTypeBuilder<FhmPlayerAttributes> builder) {
        builder.HasKey(attributes => attributes.InternalId);
        builder.Property(attributes => attributes.InternalId).ValueGeneratedNever();
    }
}