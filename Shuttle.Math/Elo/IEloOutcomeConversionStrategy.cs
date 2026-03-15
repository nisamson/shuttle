using Shuttle.Math.Outcomes;

namespace Shuttle.Math.Elo;

public interface IEloOutcomeConversionStrategy<TGameOutcome>
{
    double ToEloScore(TGameOutcome outcome);

    TGameOutcome Invert(TGameOutcome outcome);
}

public class WinDrawLossEloOutcomeConversionStrategy : IEloOutcomeConversionStrategy<WinDrawLoss>
{
    public double ToEloScore(WinDrawLoss outcome)
    {
        return outcome switch
        {
            WinDrawLoss.Win => 1.0,
            WinDrawLoss.Draw => 0.5,
            WinDrawLoss.Loss => 0.0,
            _ => throw new ArgumentOutOfRangeException(nameof(outcome), "Invalid game outcome")
        };
    }

    public WinDrawLoss Invert(WinDrawLoss outcome)
    {
        return outcome switch
        {
            WinDrawLoss.Win => WinDrawLoss.Loss,
            WinDrawLoss.Draw => WinDrawLoss.Draw,
            WinDrawLoss.Loss => WinDrawLoss.Win,
            _ => throw new ArgumentOutOfRangeException(nameof(outcome), "Invalid game outcome")
        };
    }
}

public class WinLossOvertimeEloOutcomeConversionStrategy : IEloOutcomeConversionStrategy<WinLossOvertime>
{
    public double ToEloScore(WinLossOvertime outcome)
    {
        return outcome switch
        {
            WinLossOvertime.RegulationWin => 1.0,
            WinLossOvertime.OvertimeWin => 0.666,
            WinLossOvertime.OvertimeLoss => 0.333,
            WinLossOvertime.RegulationLoss => 0.0,
            _ => throw new ArgumentOutOfRangeException(nameof(outcome), "Invalid game outcome")
        };
    }

    public WinLossOvertime Invert(WinLossOvertime outcome)
    {
        return outcome switch
        {
            WinLossOvertime.OvertimeWin => WinLossOvertime.OvertimeLoss,
            WinLossOvertime.RegulationLoss => WinLossOvertime.RegulationWin,
            WinLossOvertime.OvertimeLoss => WinLossOvertime.OvertimeWin,
            WinLossOvertime.RegulationWin => WinLossOvertime.RegulationLoss,
            _ => throw new ArgumentOutOfRangeException(nameof(outcome), "Invalid game outcome")
        };
    }
}

public class BettmanEloOutcomeConversionStrategy : IEloOutcomeConversionStrategy<WinLossOvertime>
{
    public double ToEloScore(WinLossOvertime outcome)
    {
        return outcome switch
        {
            WinLossOvertime.RegulationWin or WinLossOvertime.OvertimeWin => 1.0,
            WinLossOvertime.OvertimeLoss => 0.5,
            WinLossOvertime.RegulationLoss => 0.0,
            _ => throw new ArgumentOutOfRangeException(nameof(outcome), "Invalid game outcome")
        };
    }

    public WinLossOvertime Invert(WinLossOvertime outcome)
    {
        return outcome switch
        {
            WinLossOvertime.OvertimeWin => WinLossOvertime.OvertimeLoss,
            WinLossOvertime.RegulationLoss => WinLossOvertime.RegulationWin,
            WinLossOvertime.OvertimeLoss => WinLossOvertime.OvertimeWin,
            WinLossOvertime.RegulationWin => WinLossOvertime.RegulationLoss,
            _ => throw new ArgumentOutOfRangeException(nameof(outcome), "Invalid game outcome")
        };
    }
}