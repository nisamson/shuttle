using System.Text.Json.Serialization;

namespace Shuttle.WebClient.Models.Options;

[JsonSerializable(typeof(ShuttleOptions))]
public record ShuttleOptions : IShuttleOptions {
    
    public required bool DarkMode { get; init; }
    
    public ShuttleOptionsModel ToModel() {
        return new() {
            DarkMode = this.DarkMode
        };
    }
    
    public static readonly ShuttleOptions Default = new() {
        DarkMode = false
    };
}
