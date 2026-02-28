using Refit;

namespace SHLAnalytics.Api.Client.Raw.Index;

public interface IRawIndexV1Client {
    public const string BaseUrl = "https://index.simulationhockey.com/api/v1";

    [Get("/leagues")]
    Task<IApiResponse> GetLeagues(CancellationToken token = default);

    [Get("/leagues/seasons")]
    Task<IApiResponse> GetLeagueSeasons(CancellationToken token = default);

    [Get("/conferences")]
    Task<IApiResponse> GetConferences(int league, int season, CancellationToken token = default);
    
    [Get("/teams")]
    Task<IApiResponse> GetTeams(int league, int season, CancellationToken token = default);
    
    [Get("/teams/{team}/roster")]
    Task<IApiResponse> GetTeamRoster(int league, int season, int team, bool full = true, CancellationToken token = default);

    [Get("/players")]
    Task<IApiResponse> GetPlayers(int league, int season, CancellationToken token = default);

    [Get("/players/{id}")]
    Task<IApiResponse> GetPlayer(int league, int season, int id, CancellationToken token = default);
    
    [Get("/players/stats")]
    Task<IApiResponse> GetPlayerStats(int league, int season,  CancellationToken token = default);
}
