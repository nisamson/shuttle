using System.Diagnostics.CodeAnalysis;
using MudBlazor;

namespace Shuttle.WebClient.Models.Options;

[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
public record ShuttleOptionsModel : IShuttleOptions {
    [Label("Use Dark Mode")]
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
