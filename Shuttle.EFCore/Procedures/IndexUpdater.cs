using LinqToDB;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shuttle.EFCore.Entities;
using Shuttle.EFCore.Entities.Index;
using Shuttle.Shl.Api.Client;
using Shuttle.Shl.Api.Models.Common;

namespace Shuttle.EFCore.Procedures;

public class IndexUpdater {
    private readonly IShlIndexV1Client indexClient;
    private readonly ShlDbContext dbContext;
    private readonly ILogger<IndexUpdater> logger;

    public IndexUpdater(IShlIndexV1Client indexClient, ShlDbContext dbContext, ILogger<IndexUpdater> logger) {
        this.indexClient = indexClient;
        this.dbContext = dbContext;
        this.logger = logger;
    }

    public async Task UpdateIndex(CancellationToken token = default) {
        using var activity = ActivitySources.ShuttleEfCore.StartActivity();
        logger.LogInformation("Updating data from SHL index");
        var strategy = dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () => {
            await using var tx = await dbContext.Database.BeginTransactionAsync(token);
            try {
                await UpdateLeagues(token);
                dbContext.ChangeTracker.Clear();
                await UpdateLeagueSeasons(token);
                dbContext.ChangeTracker.Clear();
                await UpdateConferences(token);
                dbContext.ChangeTracker.Clear();
                await UpdateDivisions(token);
                dbContext.ChangeTracker.Clear();
                await UpdateTeams(token);
                dbContext.ChangeTracker.Clear();
                await UpdateAllGameResults(token);
                dbContext.ChangeTracker.Clear();
            } catch (Exception exception) {
                activity?.AddException(exception);
                logger.LogError(exception, "Error occurred while updating index tables");
                throw;
            }

            await tx.CommitAsync(token);
        });
    }

    private async Task UpdateLeagues(CancellationToken token = default) {
        using var activity = ActivitySources.ShuttleEfCore.StartActivity();
        logger.LogInformation("Updating leagues");
        var leagues = await indexClient.GetLeagues(token);
        logger.LogInformation("Retrieved {Count} leagues from index", leagues.Count);
        var leagueEntities = leagues.Select(League.FromModel).ToList();
        var updated = await dbContext.Leagues.Merge()
            .Using(leagueEntities)
            .OnTargetKey()
            .UpdateWhenMatchedAnd((t, s) => t.Abbreviation != s.Abbreviation || t.Name != s.Name)
            .InsertWhenNotMatched()
            .MergeAsync(token);
        logger.LogInformation("Updated {Count} leagues from index", updated);
    }

    private async Task UpdateLeagueSeasons(CancellationToken token = default) {
        using var activity = ActivitySources.ShuttleEfCore.StartActivity();
        logger.LogInformation("Updating league seasons");
        var knownLeagues = await dbContext.Leagues
            .AsNoTracking()
            .Select(l => l.LeagueId)
            .ToListAsync(token);
        var leagueSeasons = new List<LeagueSeason>();
        foreach (var league in knownLeagues) {
            logger.LogInformation("Fetching data for league {LeagueId}", league);
            var seasons = await indexClient.GetLeagueSeasons(league, token);
            logger.LogInformation("Retrieved {Count} seasons from index", seasons.Count);
            leagueSeasons.AddRange(seasons.Select(LeagueSeason.FromModel));
        }

        logger.LogInformation("Retrieved {Count} league seasons from index", leagueSeasons.Count);
        var added = await dbContext.Seasons.Merge()
            .Using(leagueSeasons)
            .OnTargetKey()
            .InsertWhenNotMatched()
            .MergeAsync(token);
        logger.LogInformation("Added {Count} seasons from index", added);
    }

    private async Task UpdateConferences(CancellationToken token = default) {
        using var activity = ActivitySources.ShuttleEfCore.StartActivity();
        logger.LogInformation("Updating conferences");
        var leagueSeasons = await dbContext.Seasons.AsNoTracking().ToListAsync(token);
        var conferences = new List<Conference>();
        foreach (var season in leagueSeasons) {
            logger.LogInformation(
                "Fetching data for season {SeasonId} for league {LeagueId}",
                season.Season,
                season.LeagueId
            );
            var seasonConferences = await indexClient.GetConferences(season.LeagueId, season.Season, token);
            conferences.AddRange(seasonConferences.Select(Conference.FromModel));
            logger.LogInformation("Retrieved {Count} conferences from index", conferences);
        }

        logger.LogInformation("Retrieved {Count} conferences from index", conferences.Count);
        var updated = await dbContext.Conferences.Merge()
            .Using(conferences)
            .OnTargetKey()
            .InsertWhenNotMatched()
            .UpdateWhenMatchedAnd((t, s) => t.Name != s.Name)
            .MergeAsync(token);
        logger.LogInformation("Updated {Count} conferences from index", updated);
    }

    private async Task UpdateDivisions(CancellationToken token = default) {
        using var activity = ActivitySources.ShuttleEfCore.StartActivity();
        logger.LogInformation("Updating divisions");
        var leagueSeasons = await dbContext.Seasons.AsNoTracking().ToListAsync(token);
        var divisions = new List<Division>();
        foreach (var season in leagueSeasons) {
            logger.LogInformation("Fetching data for season {SeasonId}, league {LeagueId}", season.Season, season.LeagueId);
            var seasonDivisions = await indexClient.GetDivisions(season.LeagueId, season.Season, null, token);
            divisions.AddRange(seasonDivisions.Select(Division.FromModel));
        }
        logger.LogInformation("Retrieved {Count} divisions from index", divisions.Count);
        var updated = dbContext.Divisions.Merge()
            .Using(divisions)
            .OnTargetKey()
            .InsertWhenNotMatched()
            .UpdateWhenMatchedAnd(Division.Changed)
            .MergeAsync(token);
        logger.LogInformation("Updated {Count} divisions from index", updated);
    }

    private async Task UpdateTeams(CancellationToken token = default) {
        using var activity = ActivitySources.ShuttleEfCore.StartActivity();
        logger.LogInformation("Updating teams");
        var leagueSeasons = await dbContext.Seasons.AsNoTracking().ToListAsync(token);
        foreach (var leagueSeason in leagueSeasons) {
            await UpdateLeagueTeamSeasons(leagueSeason.KnownLeague, leagueSeason.Season, token);
            dbContext.ChangeTracker.Clear();
        }
    }
    
    private async Task UpdateAllGameResults(CancellationToken token = default) {
        using var activity = ActivitySources.ShuttleEfCore.StartActivity();
        logger.LogInformation("Updating teams");
        var leagueSeasons = await dbContext.Seasons.AsNoTracking().ToListAsync(token);
        foreach (var leagueSeason in leagueSeasons) {
            await UpdateGamesPlayed(leagueSeason.KnownLeague, leagueSeason.Season, token);
            dbContext.ChangeTracker.Clear();
        }
    }

    private async Task UpdateLeagueTeamSeasons(KnownLeague league, int season, CancellationToken token = default) {
        using var activity = ActivitySources.ShuttleEfCore.StartActivity();
        activity?.SetTag("league", league.ToString());
        activity?.SetTag("season", season.ToString());
        logger.LogInformation("Updating team data information for league {LeagueId} season {Season}", league,  season);
        var teams = await indexClient.GetTeams(league.Id, season: season, cancellationToken: token);
        logger.LogInformation("Retrieved {Count} teams from index", teams);
        var leagueId = league.Id;
        var knownTeams = await dbContext.Teams
            .Where(t => t.LeagueId ==  leagueId)
            .Where(t => t.Season == season)
            .ToDictionaryAsync(TeamKey.FromEntity, token);
        foreach (var team in teams) {
            var key = TeamKey.FromShl(team);
            if (knownTeams.TryGetValue(key, out var knownTeam)) {
                if (knownTeam.UpdateFromModel(team)) {
                    dbContext.Teams.Update(knownTeam);
                }
            } else {
                dbContext.Teams.Add(Team.FromModel(team));
            }
        }

        var changed = await dbContext.SaveChangesAsync(token);
        logger.LogInformation("Updated {Count} teams from index", changed);
    }

    private async Task UpdateGamesPlayed(KnownLeague league, int season, CancellationToken token = default) {
        using var activity = ActivitySources.ShuttleEfCore.StartActivity();
        logger.LogInformation("Updating games played");
        activity?.SetTag("league", league.ToString());
        activity?.SetTag("season", season.ToString());
        var games = await indexClient.GetSchedule(league.Id, season, token);
        logger.LogInformation("Retrieved {Count} games from index", games.Count);
        var gameEntities = games.Select(GameResult.FromModel).ToList();
        var updated = dbContext.GameResults.Merge()
            .Using(gameEntities)
            .OnTargetKey()
            .InsertWhenNotMatched()
            .UpdateWhenMatchedAnd(GameResult.ChangedExpr)
            .MergeAsync(token);
        logger.LogInformation("Updated {Count} games from index", updated);
    }
}
