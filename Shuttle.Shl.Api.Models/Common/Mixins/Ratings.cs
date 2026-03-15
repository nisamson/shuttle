namespace Shuttle.Shl.Api.Models.Common.Mixins;

public interface IOffensiveRatings {
    int Screening { get; }
    int GettingOpen { get; }
    int Passing { get; }
    int Puckhandling { get; }
    int ShootingAccuracy { get; }
    int ShootingRange { get; }
    int OffensiveRead { get; }
}

public interface IMentalRatings {
    int Aggression { get; }
    int Bravery { get; }
    int Determination { get; }
    int TeamPlayer { get; }
    int Leadership { get; }
    int Temperament { get; }
    int Professionalism { get; }
}

public interface IDefensiveRatings {
    int Checking { get; }
    int Stickchecking { get; }
    int Hitting { get; }
    int Positioning { get; }
    int ShotBlocking { get; }
    int DefensiveRead { get; }
    int Faceoffs { get; }
}

public interface IPhysicalRatings {
    int Acceleration { get; }
    int Agility { get; }
    int Balance { get; }
    int Speed { get; }
    int Stamina { get; }
    int Strength { get; }
    int Fighting { get; }
}

public interface ISkaterRatings : IOffensiveRatings, IMentalRatings, IDefensiveRatings, IPhysicalRatings {
    
    bool RatingsChanged(ISkaterRatings other) =>
        Screening != other.Screening ||
        GettingOpen != other.GettingOpen ||
        Passing != other.Passing ||
        Puckhandling != other.Puckhandling ||
        ShootingAccuracy != other.ShootingAccuracy ||
        ShootingRange != other.ShootingRange ||
        OffensiveRead != other.OffensiveRead ||
        Aggression != other.Aggression ||
        Bravery != other.Bravery ||
        Determination != other.Determination ||
        TeamPlayer != other.TeamPlayer ||
        Leadership != other.Leadership ||
        Temperament != other.Temperament ||
        Professionalism != other.Professionalism ||
        Checking != other.Checking ||
        Stickchecking != other.Stickchecking ||
        Hitting != other.Hitting ||
        Positioning != other.Positioning ||
        ShotBlocking != other.ShotBlocking ||
        DefensiveRead != other.DefensiveRead ||
        Acceleration != other.Acceleration ||
        Agility != other.Agility ||
        Balance != other.Balance ||
        Speed != other.Speed ||
        Stamina != other.Stamina ||
        Strength != other.Strength ||
        Fighting != other.Fighting;
}

public interface IGoaltendingTechniqueRatings {
    int Positioning { get; }
    int Passing { get; }
    int PokeCheck { get; }
    int Blocker { get; }
    int Glove { get; }
    int Rebound { get; }
    int Recovery { get; }
    int Puckhandling { get; }
    int LowShots { get; }
    int Skating { get; }
    int Reflexes { get; }
}

public interface IGoaltendingMentalRatings {
    int Aggression { get; }
    int MentalToughness { get; }
    int Determination { get; }
    int TeamPlayer { get; }
    int Leadership { get; }
    int Stamina { get; }
    int Professionalism { get; }
}

public interface IGoaltenderRatings : IGoaltendingMentalRatings, IGoaltendingTechniqueRatings {
};
