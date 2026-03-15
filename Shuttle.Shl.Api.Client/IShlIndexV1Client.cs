using Refit;
using Shuttle.Shl.Api.Models.Index.V1;

namespace Shuttle.Shl.Api.Client;

public interface IShlIndexV1Client {
    public const string BaseUrl = "https://index.simulationhockey.com/api/v1";

    [Get("/leagues")]
    Task<IList<League>> GetLeagues(CancellationToken cancellationToken = default);

    [Get("/leagues/seasons")]
    Task<IList<LeagueSeason>> GetLeagueSeasons(int league = 0, CancellationToken cancellationToken = default);

    [Get("/conferences")]
    Task<IList<Conference>> GetConferences(
        int league,
        int? season = null,
        CancellationToken cancellationToken = default
    );

    [Get("/conferences/{conference}")]
    Task<Conference> GetConference(int league, int conference, CancellationToken cancellationToken = default);

    [Get("/teams")]
    Task<IList<Team>> GetTeams(
        int league,
        int? conference = null,
        int? division = null,
        int? season = null,
        CancellationToken cancellationToken = default
    );
    
    [Get("/teams/{team}")]
    Task<Team> GetTeam(int league, int team, CancellationToken cancellationToken = default);
    
    [Get("/schedule")]
    Task<IList<GameResult>> GetSchedule(
        int league,
        int season,
        CancellationToken cancellationToken = default
    );
}
