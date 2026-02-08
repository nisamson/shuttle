namespace SHLAnalytics.Math;

public record Game<TOutcome>(Player PlayerA, Player PlayerB, TOutcome Outcome);
