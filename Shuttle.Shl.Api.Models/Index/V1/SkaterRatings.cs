using Shuttle.Shl.Api.Models.Common.Mixins;

namespace Shuttle.Shl.Api.Models.Index.V1;



public record SkaterRatings(
    int Id,
    int League,
    int Season,
    int Screening,
    int GettingOpen,
    int Passing,
    int Puckhandling,
    int ShootingAccuracy,
    int ShootingRange,
    int OffensiveRead,
    int Aggression,
    int Bravery,
    int Determination,
    int TeamPlayer,
    int Leadership,
    int Temperament,
    int Professionalism,
    int Checking,
    int Stickchecking,
    int Hitting,
    int Positioning,
    int ShotBlocking,
    int StickChecking,
    int DefensiveRead,
    int Acceleration,
    int Agility,
    int Balance,
    int Speed,
    int Stamina,
    int Strength,
    int Fighting,
    int AppliedTpe,
    int Faceoffs
) : IHasId, ILeagueSeason, ISkaterRatings;
