using System.Diagnostics;
using Microsoft.AspNetCore.Components;
using Shuttle.WebClient.Components.Options;
using Shuttle.WebClient.Models.Options;
using Shuttle.WebClient.Services;

namespace Shuttle.WebClient.Layout;

public partial class MainLayout {
    private ShuttleOptionsContext? shuttleOptionsContext;

    private ShuttleOptions Options => shuttleOptionsContext?.CurrentOptions ?? ShuttleOptions.Default;
}
