using Shuttle.WebClient.Models;
using Shuttle.WebClient.Models.Options;

namespace Shuttle.WebClient.Services;

public interface IShuttleOptionsStorage {
    public Task<ShuttleOptions> LoadOptions(bool forceLoad, CancellationToken token = default);
    public Task SaveOptions(ShuttleOptions options, CancellationToken token = default);
    
    public ShuttleOptions CurrentOptions { get; }
    
    public event Action<ShuttleOptions>? OptionsChanged;
}
