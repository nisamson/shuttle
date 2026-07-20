using Refit;
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
}
