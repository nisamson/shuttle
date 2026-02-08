using SHLAnalytics.Api.Models.Index.V1;
using SHLAnalytics.Math.Outcomes;

namespace SHLAnalytics.EloCalc;

public static class EloAssist {
    public static ScoreShare ToScoreShare(this GameResult result) {
        return new(result.HomeScore, result.AwayScore);
    }
}
