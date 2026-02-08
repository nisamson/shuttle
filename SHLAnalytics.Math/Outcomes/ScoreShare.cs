using SHLAnalytics.Math.Elo;

namespace SHLAnalytics.Math.Outcomes;

public record ScoreShare(int PlayerAScore, int PlayerBScore) { }

public class ScoreShareConverter : IEloOutcomeConversionStrategy<ScoreShare> {
    public double ToEloScore(ScoreShare outcome) {
        var totalScore = outcome.PlayerAScore + outcome.PlayerBScore;
        if (totalScore == 0) {
            return 0.5; // Treat 0-0 as a draw
        }
        return (double)outcome.PlayerAScore / totalScore;
    }
    public ScoreShare Invert(ScoreShare outcome) {
        return new(outcome.PlayerBScore, outcome.PlayerAScore);
    }
}