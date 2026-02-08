using JetBrains.Annotations;
using SHLAnalytics.Math;
using SHLAnalytics.Math.Elo;
using SHLAnalytics.Math.Outcomes;

namespace SHLAnalytics.Tests.Elo;

[TestSubject(typeof(BaseEloStrategy<>))]
public class BaseEloStrategyTest
{

    [Fact]
    public void StrategyCorrectlyCalculates()
    {
        var fideStrategy = new EloStrategy<WinDrawLoss>(new WinDrawLossEloOutcomeConversionStrategy());
        var playerA = fideStrategy.CreatePlayer();
        var playerB = fideStrategy.CreatePlayer();

        var game = new Game<WinDrawLoss>(playerA, playerB, WinDrawLoss.Win);
        var updated = fideStrategy.UpdateRatings(game);
        Assert.True(updated.PlayerA.Rating.Value > playerA.Rating.Value);
        Assert.True(updated.PlayerB.Rating.Value < playerB.Rating.Value);
    }
}