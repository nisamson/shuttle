using Refit;
using Shuttle.Shl.Api.Models.Portal.V1;

namespace Shuttle.Shl.Api.Client;

public interface IShlPortalV1Client {
    public const string BaseUrl = "https://portal.simulationhockey.com/api/v1";
    
    [Get("/players")]
    public Task<IList<PlayerInfo>> GetPlayers(CancellationToken token = default);
}
