using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SHLAnalytics.Api.Client;

namespace SHLAnalytics.EloCalc;

public class ShlEloDataRetriever {

    private readonly IShlIndexV1Client indexClient;
    private readonly ILogger<ShlEloDataRetriever> logger;

    public ShlEloDataRetriever(IShlIndexV1Client indexClient, ILogger<ShlEloDataRetriever>? logger = null) {
        this.indexClient = indexClient;
        this.logger = logger ?? NullLogger<ShlEloDataRetriever>.Instance;
    }

    public async Task<EloCalcData> GetEloCalcData(string leagueName, int season, CancellationToken cancellationToken = default) {
        using var scope = logger.BeginScope("GetEloCalcData for league {leagueName}", leagueName);
        logger.LogInformation("Retrieving league information.");
        var leagues = await indexClient.GetLeagues(cancellationToken);
        var league = leagues.SingleOrDefault(l => l.Abbreviation.Equals(leagueName, StringComparison.OrdinalIgnoreCase));
        if (league == null) {
            throw new ArgumentException($"League '{leagueName}' not found. Available leagues: {string.Join(", ", leagues.Select(l => l.Abbreviation))}");
        }

        logger.LogInformation("Retrieving teams for league {league} and season {season}.", league.Name, season);
        var teams = await indexClient.GetTeams(league.Id, season: season, cancellationToken: cancellationToken);
        var teamDict = teams.ToDictionary(t => t.Id, t => t);
        
        logger.LogInformation("Retrieving schedule for season {season}.", season);
        var gameResults = await indexClient.GetSchedule(league.Id, season, cancellationToken);
        
        return new EloCalcData(teamDict, gameResults);
    }
}
