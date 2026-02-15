using SHLAnalytics.Api.Models.Common;

namespace SHLAnalytics.Api.Models.Index.V1;

public interface IPlayerRef : IHasId, INamed;

public interface IPlayerSeason : IPlayerRef, ILeagueSeason;


public record PlayerRef(int Id, string Name) : IPlayerRef;

public record Player(
    int Id,
    string Name,
    int League,
    int Season,
    string Team,
    string Position,
    int Height,
    int Weight
) : IPlayerSeason;
