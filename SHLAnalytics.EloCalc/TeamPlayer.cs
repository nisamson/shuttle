using SHLAnalytics.Api.Models.Index.V1;
using SHLAnalytics.Math;
using Player = SHLAnalytics.Math.Player;

namespace SHLAnalytics.EloCalc;

public record TeamPlayer(Team Team, Player Player);

public record TeamPlayerSeasonRankings(Team Team, IList<Player> Ratings) {
    Player MostRecentRating() => Ratings[^1];
}
