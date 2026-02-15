using SHLAnalytics.Api.Models.Common.Mixins;

namespace SHLAnalytics.Api.Models.Index.V1;

public record GoaltenderRatings(
    int Id,
    string Name,
    int Aggression,
    int MentalToughness,
    int Determination,
    int TeamPlayer,
    int Leadership,
    int Stamina,
    int Professionalism,
    int Positioning,
    int Passing,
    int PokeChecking,
    int Blocker,
    int Glove,
    int Rebound,
    int Recovery,
    int Puckhandling,
    int LowShots,
    int Skating,
    int Reflexes,
    int AppliedTpe
) : IPlayerRef, IGoaltenderRatings;
