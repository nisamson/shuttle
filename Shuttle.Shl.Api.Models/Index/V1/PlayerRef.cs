using Shuttle.Shl.Api.Models.Common.Mixins;

namespace Shuttle.Shl.Api.Models.Index.V1;

public interface IPlayerRef : IHasId, INamed;

public interface IPlayerSeason : IPlayerRef, ILeagueSeason;


public record PlayerRef(int Id, string Name) : IPlayerRef;

public record IndexPlayer(
    int Id,
    string Name,
    int League,
    int Season,
    string Team,
    string Position,
    int Height,
    int Weight
) : IPlayerSeason;
