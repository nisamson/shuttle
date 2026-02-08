using SHLAnalytics.Api.Models.Index.V1;
using SHLAnalytics.Math;

namespace SHLAnalytics.EloCalc;

public record TeamPlayer(Team Team, Player Player);

public record TeamPlayerSeasonRankings(Team Team, IList<Player> Ratings) {
    Player MostRecentRating() => Ratings[^1];
}
