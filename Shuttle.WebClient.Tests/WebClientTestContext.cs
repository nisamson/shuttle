using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.FluentUI.AspNetCore.Components;
using Shuttle.Api.Client;
using Shuttle.WebClient.Services;
using Shuttle.WebClient.Testing;

namespace Shuttle.WebClient.Tests;

/// <summary>
/// Base bUnit context for WebClient component tests. Wires up the Azure-free dependencies every
/// component needs — Fluent UI services, the in-memory <see cref="IShuttlePlayerClient"/> (seeded
/// with <see cref="SeedData"/>), and the client-side <see cref="IPlayerDirectoryService"/> — and
/// runs JS interop in loose mode so Fluent UI components render without real JS modules.
/// Auth is intentionally NOT registered here: auth-sensitive tests opt in with bUnit's
/// <c>AddAuthorization()</c> so they can control the signed-in state and roles.
/// </summary>
public abstract class WebClientTestContext : BunitContext {
    protected WebClientTestContext() {
        JSInterop.Mode = JSRuntimeMode.Loose;

        Services.AddLogging();
        Services.AddFluentUIComponents();
        Services.AddSingleton<IShuttlePlayerClient>(new InMemoryShuttlePlayerClient());
        Services.AddSingleton<IShuttleUserClient>(new InMemoryShuttleUserClient());
        Services.AddSingleton<IShuttleDebugClient>(new InMemoryShuttleDebugClient());
        Services.AddSingleton<IPlayerDirectoryService, PlayerDirectoryService>();
        Services.AddSingleton<IUserDirectoryService, UserDirectoryService>();
    }
}
