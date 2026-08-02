using Shuttle.Math.Outcomes;
using Shuttle.Shl.Api.Models.Index.V1;

namespace Shuttle.EloCalc;

public static class EloAssist {
    public static ScoreShare ToScoreShare(this GameResult result) {
        return new(result.HomeScore, result.AwayScore);
    }
}
