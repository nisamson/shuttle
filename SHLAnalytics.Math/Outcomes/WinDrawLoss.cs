namespace SHLAnalytics.Math.Outcomes;

public enum WinDrawLoss {
    Loss = -1,
    Draw = 0,
    Win = 1,
}

public static class WinDrawLossExtensions
{
    public static double ToEloScore(this WinDrawLoss outcome)
    {
        return outcome switch
        {
            WinDrawLoss.Win => 1.0,
            WinDrawLoss.Draw => 0.5,
            WinDrawLoss.Loss => 0.0,
            _ => throw new ArgumentOutOfRangeException(nameof(outcome), "Invalid game outcome")
        };
    }
}