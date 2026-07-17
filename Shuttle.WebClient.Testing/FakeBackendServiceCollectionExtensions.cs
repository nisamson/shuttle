using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shuttle.Api.Client;

namespace Shuttle.WebClient.Testing;

/// <summary>
/// DI registration for the Azure-free "fake backend": an in-memory <see cref="IShuttlePlayerClient"/>
/// plus a fake <see cref="AuthenticationStateProvider"/>. Call this from the WebClient's
/// <c>Program.cs</c> when the <c>Testing:FakeBackend</c> flag is set instead of the real Refit +
/// MSAL registrations.
/// </summary>
public static class FakeBackendServiceCollectionExtensions {
    /// <summary>
    /// Registers the in-memory player client and fake authentication so the app runs entirely
    /// offline. <paramref name="configureAuth"/> can tweak the fake user (name / roles / signed-in
    /// state); by default an authenticated admin user is used.
    /// </summary>
    public static IServiceCollection AddShuttleFakeBackend(
        this IServiceCollection services,
        Action<FakeAuthOptions>? configureAuth = null) {
        var authOptions = new FakeAuthOptions();
        configureAuth?.Invoke(authOptions);

        services.AddAuthorizationCore();

        services.RemoveAll<IShuttlePlayerClient>();
        services.AddSingleton<IShuttlePlayerClient>(_ => new InMemoryShuttlePlayerClient());

        services.RemoveAll<IShuttleLeagueClient>();
        services.AddSingleton<IShuttleLeagueClient>(_ => new InMemoryShuttleLeagueClient());

        services.AddSingleton(authOptions);
        services.AddSingleton<FakeAuthenticationStateProvider>();
        services.AddSingleton<AuthenticationStateProvider>(
            sp => sp.GetRequiredService<FakeAuthenticationStateProvider>());

        // The user client is auth-aware (Discord gating), so hand it the fake auth state provider.
        services.RemoveAll<IShuttleUserClient>();
        services.AddSingleton<IShuttleUserClient>(
            sp => new InMemoryShuttleUserClient(sp.GetRequiredService<AuthenticationStateProvider>()));

        // The debug client reports the fake caller's roles as the server would.
        services.RemoveAll<IShuttleDebugClient>();
        services.AddSingleton<IShuttleDebugClient>(
            sp => new InMemoryShuttleDebugClient(sp.GetRequiredService<AuthenticationStateProvider>()));

        return services;
    }
}
