namespace Shuttle.Math.Elo;

public abstract class BaseEloStrategy<TGameOutcome> : IRatingUpdateStrategy<TGameOutcome> {
    public const int RatingFloor = 100;
    
    public int InitialRating { get; }

    protected IEloOutcomeConversionStrategy<TGameOutcome> OutcomeConversionStrategy { get; }

    protected BaseEloStrategy(IEloOutcomeConversionStrategy<TGameOutcome> conversionStrategy, int initialRating = 1000)
    {
        OutcomeConversionStrategy = conversionStrategy;
        InitialRating = initialRating;
    }

    public abstract double KFactor(Player player);

    public abstract double ScaleFactor { get; }
    public double Q(Player player)
    {
        return SMath.Pow(10, player.Rating.Value / ScaleFactor);
    }

    public double ExpectedScore(Player a, Player b)
    {
        var qa = Q(a);
        var qb = Q(b);
        return qa / (qa + qb);
    }

    public Player CreatePlayer()
    {
        return new Player(new(InitialRating), 0);
    }

    public Game<TGameOutcome> UpdateRatings(
        Game<TGameOutcome> game
    )
    {
        var playerAScore = OutcomeConversionStrategy.ToEloScore(game.Outcome);
        var playerBScore = OutcomeConversionStrategy.ToEloScore(OutcomeConversionStrategy.Invert(game.Outcome));
        var expectedScoreA = ExpectedScore(game.PlayerA, game.PlayerB);
        var expectedScoreB = ExpectedScore(game.PlayerB, game.PlayerA);

        var kFactorA = KFactor(game.PlayerA);
        var kFactorB = KFactor(game.PlayerB);

        var newRatingA = SMath.Max(game.PlayerA.Rating.Value + kFactorA * (playerAScore - expectedScoreA), RatingFloor);
        var newRatingB = SMath.Max(game.PlayerB.Rating.Value + kFactorB * (playerBScore - expectedScoreB), RatingFloor);

        var updatedPlayerA = new Player(new(int.CreateTruncating(newRatingA)), game.PlayerA.GamesPlayed + 1);
        var updatedPlayerB = new Player(new(int.CreateTruncating(newRatingB)), game.PlayerB.GamesPlayed + 1);

        return new(updatedPlayerA, updatedPlayerB, game.Outcome);
    }
}

public class EloStrategy<TGameOutcome>(
    IEloOutcomeConversionStrategy<TGameOutcome> conversionStrategy,
    int initialRating = 1000)
    : BaseEloStrategy<TGameOutcome>(conversionStrategy, initialRating)
{
    public override double KFactor(Player player)
    {
        return player.Rating.Value switch
        {
            < 1800 when player.GamesPlayed < 30 => 100.0,
            < 1800 => 50.0,
            _ => 10.0,
        };
    }

    public override double ScaleFactor => 400.0;
}

public class SeasonEloStrategy<TGameOutcome>(
    IEloOutcomeConversionStrategy<TGameOutcome> conversionStrategy,
    int initialRating = 1000)
    : BaseEloStrategy<TGameOutcome>(conversionStrategy, initialRating)
{
    public override double KFactor(Player player) {
        return 100 - player.GamesPlayed;
    }

    public override double ScaleFactor => 200.0;
}