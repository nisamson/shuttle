namespace Shuttle.Math;

public interface IRatingUpdateStrategy<TGameOutcome> {
    Game<TGameOutcome> UpdateRatings(
        Game<TGameOutcome> game
    );

    Player CreatePlayer();
}
