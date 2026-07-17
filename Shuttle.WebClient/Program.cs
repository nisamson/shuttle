using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.FluentUI.AspNetCore.Components;
using Shuttle.Api.Client;
using Shuttle.WebClient;
using Shuttle.WebClient.Services;
using Shuttle.WebClient.Testing;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// The API base URL is supplied via wwwroot config: appsettings.json points at the production
// API (https://api.shl.nes.sh) and appsettings.Development.json overrides it with the local
// Aspire dev-server URL. The host base address is only a last-resort fallback.
var apiBaseAddress = builder.Configuration["Api:BaseUrl"] ?? builder.HostEnvironment.BaseAddress;
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new(apiBaseAddress) });

// Fake-backend run mode: when Testing:FakeBackend is set (see appsettings.Testing.json + the
// "TestServer" launch profile), swap the real Refit API client and MSAL auth for an in-memory
// fake + a fake identity so the whole app runs with no Azure / network dependency. The flag is
// absent from the production appsettings.json, so this path is inert in production.
var useFakeBackend = builder.Configuration.GetValue<bool>("Testing:FakeBackend");
if (useFakeBackend) {
    builder.Services.AddShuttleFakeBackend();
} else {
    // Typed Refit clients for the Shuttle backend API (player endpoints, etc.).
    builder.Services.AddShuttleApiClient(new Uri(apiBaseAddress));

    // The league/team endpoints are public (anonymous), so no access-token handler is attached.
    builder.Services.AddShuttleLeagueClient(new Uri(apiBaseAddress));

    // The user client is attached to the access-token handler so authenticated callers receive the
    // richer (Discord-bearing) projection; anonymous callers fall through without a token.
    builder.Services.AddScoped<ApiAccessTokenHandler>();
    builder.Services.AddShuttleUserClient(new Uri(apiBaseAddress))
        .AddHttpMessageHandler<ApiAccessTokenHandler>();

    // The dev-only debug endpoints require an authenticated caller, so attach the token handler too.
    builder.Services.AddShuttleDebugClient(new Uri(apiBaseAddress))
        .AddHttpMessageHandler<ApiAccessTokenHandler>();

    builder.Services.AddMsalAuthentication(options => {
            builder.Configuration.Bind("AzureAd", options.ProviderOptions.Authentication);
            options.UserOptions.RoleClaim = "roles";

            // Request an access token scoped to the Shuttle API at sign-in. Without this MSAL hands
            // back a token for a different resource (e.g. Microsoft Graph), whose signature the API
            // cannot validate (IDX10511). The scopes are configurable so the API's App ID URI can be
            // corrected without a rebuild.
            var apiScopes = builder.Configuration.GetSection("Api:Scopes").Get<string[]>() ?? [];
            foreach (var scope in apiScopes) {
                options.ProviderOptions.DefaultAccessTokenScopes.Add(scope);
            }
        }
    ).AddAccountClaimsPrincipalFactory<ArrayClaimsPrincipalFactory>();
}

builder.Services.AddFluentUIComponents();
builder.Services.AddLocalStorageServices();
builder.Services.AddSingleton<IBlogService, BlogService>();
builder.Services.AddSingleton<IShuttleOptionsStorage, ShuttleOptionsLocalStorage>();
builder.Services.AddSingleton<IPlayerDirectoryService, PlayerDirectoryService>();
builder.Services.AddSingleton<IUserDirectoryService, UserDirectoryService>();
if (builder.HostEnvironment.IsDevelopment()) {
    builder.Logging.AddFilter("Microsoft.AspNetCore.Components.RenderTree.*", LogLevel.None);
    builder.Logging.SetMinimumLevel(LogLevel.Trace);
} else {
    builder.Logging.SetMinimumLevel(LogLevel.Information);
}

var app = builder.Build();

app.Services.GetRequiredService<ILoggerFactory>()
    .CreateLogger<Program>();

await app.RunAsync();
