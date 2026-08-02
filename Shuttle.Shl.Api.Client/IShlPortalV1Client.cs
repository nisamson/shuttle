using Refit;
using Shuttle.Shl.Api.Models.Common;
using Shuttle.Shl.Api.Models.Portal.V1;

namespace Shuttle.Shl.Api.Client;

public interface IShlPortalV1Client {
    public const string BaseUrl = "https://portal.simulationhockey.com/api/v1";
    
    [Get("/player")]
    public Task<IList<PlayerInfo>> GetPlayers(CancellationToken token = default);

    /// <summary>
    /// Fetches the TPE timeline for a single player: the chronological sequence of the player's
    /// cumulative total TPE at each TPE-affecting task.
    /// </summary>
    /// <param name="pid">The portal player id (<c>pid</c>) to fetch the timeline for.</param>
    /// <param name="token">A cancellation token.</param>
    [Get("/tpeevents/timeline")]
    public Task<IList<TpeTimelineEntry>> GetTpeTimeline(int pid, CancellationToken token = default);

    /// <summary>
    /// Fetches per-player, per-season TPE-earning breakdowns from the analytics endpoint. All filters
    /// are optional; omitting them returns the full, unfiltered set. When filtering by
    /// <paramref name="currentTeamId"/>, <paramref name="currentLeague"/> must also be supplied.
    /// </summary>
    /// <param name="playerUpdateId">Filter to a single portal player update id (<c>playerUpdateID</c>).</param>
    /// <param name="season">Filter to the season the TPE was earned in.</param>
    /// <param name="currentLeague">Filter to players currently in this league.</param>
    /// <param name="currentTeamId">Filter to players on this team (requires <paramref name="currentLeague"/>).</param>
    /// <param name="draftSeason">Filter to players drafted in this season.</param>
    /// <param name="userId">Filter to the player(s) behind this user id.</param>
    /// <param name="token">A cancellation token.</param>
    public Task<IList<EarnedTpeEntry>> GetEarnedTpe(
        int? playerUpdateId = null,
        int? season = null,
        KnownLeague? currentLeague = null,
        int? currentTeamId = null,
        int? draftSeason = null,
        int? userId = null,
        CancellationToken token = default
    ) => GetEarnedTpe(
        playerUpdateId,
        season,
        currentLeague?.Abbreviation,
        currentTeamId,
        draftSeason,
        userId,
        token
    );

    /// <summary>
    /// Raw Refit binding for <c>GET /analytics/earned-tpe</c>. Prefer the strongly-typed
    /// <see cref="GetEarnedTpe(int?,int?,KnownLeague?,int?,int?,int?,CancellationToken)"/> overload,
    /// which formats <c>currentLeague</c> as the league abbreviation the API expects.
    /// </summary>
    [Get("/analytics/earned-tpe")]
    public Task<IList<EarnedTpeEntry>> GetEarnedTpe(
        [AliasAs("playerUpdateID")] int? playerUpdateId,
        [AliasAs("season")] int? season,
        [AliasAs("currentLeague")] string? currentLeague,
        [AliasAs("currentTeamID")] int? currentTeamId,
        [AliasAs("draftSeason")] int? draftSeason,
        [AliasAs("userID")] int? userId,
        CancellationToken token = default
    );
}
