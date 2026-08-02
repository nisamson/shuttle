using System.Diagnostics.CodeAnalysis;

namespace Shuttle.WebClient.Models.Options;

[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
public record ShuttleOptionsModel : IShuttleOptions {
    public bool DarkMode { get; set; }
    
    public static readonly ShuttleOptionsModel Default = new() {
        DarkMode = false
    };
    
    public static ShuttleOptionsModel FromOptions(IShuttleOptions options) {
        return new() {
            DarkMode = options.DarkMode
        };
    }
    
    public ShuttleOptions ToOptions() {
        return new() {
            DarkMode = this.DarkMode
        };
    }
};
