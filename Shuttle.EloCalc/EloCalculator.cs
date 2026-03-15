using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Shuttle.Math;
using Shuttle.Math.Elo;
using Shuttle.Math.Outcomes;

namespace Shuttle.EloCalc;

using GameNumber = int;

public class EloCalculator {
    private readonly ILogger<EloCalculator> logger;
    public EloCalculator(ILogger<EloCalculator>? logger = null) {
        this.logger = logger ?? NullLogger<EloCalculator>.Instance;
    }

    public IList<TeamPlayerSeasonRankings> CalculateEloRatings(EloCalcData calcData) {
        var strategy = new EloStrategy<ScoreShare>(new ScoreShareConverter());
        var res = new Dictionary<GameNumber, TeamPlayerSeasonRankings>();
        var teamNames = calcData.Teams.Values.ToDictionary(t => t.Id, t => t.Abbreviation);

        var player = strategy.CreatePlayer();
        
        foreach (var team in calcData.Teams.Values) {
            res.Add(team.Id, new(team, [player]));
        }

        var gamesByDate = calcData.GameResults
            .OrderBy(g => g.Date)
            .Where(g => g.Played);

        foreach (var game in gamesByDate) {
            logger.LogInformation("Processing game {game}", game.ToString(teamNames[game.HomeTeam], teamNames[game.AwayTeam]));
            var homeTeamPlayer = res[game.HomeTeam];
            var awayTeamPlayer = res[game.AwayTeam];
            var scoreShare = new ScoreShare(game.HomeScore, game.AwayScore);
            var eloGame = new Game<ScoreShare>(
                homeTeamPlayer.Ratings[^1],
                awayTeamPlayer.Ratings[^1],
                scoreShare
            );
            var updatedGame = strategy.UpdateRatings(eloGame);
            res[game.HomeTeam].Ratings.Add(updatedGame.PlayerA);
            res[game.AwayTeam].Ratings.Add(updatedGame.PlayerB);
            logger.LogInformation("Updated ratings: {homeTeam}={homeElo}, {awayTeam}={awayElo}",
                teamNames[game.HomeTeam], updatedGame.PlayerA.Rating,
                teamNames[game.AwayTeam], updatedGame.PlayerB.Rating);
        }

        return res.Values.ToList();
    }
}
