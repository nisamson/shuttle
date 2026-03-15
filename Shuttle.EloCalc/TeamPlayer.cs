using Shuttle.Math;
using Shuttle.Shl.Api.Models.Index.V1;
using Player = Shuttle.Math.Player;

namespace Shuttle.EloCalc;

public record TeamPlayer(Team Team, Player Player);

public record TeamPlayerSeasonRankings(Team Team, IList<Player> Ratings) {
    Player MostRecentRating() => Ratings[^1];
}
