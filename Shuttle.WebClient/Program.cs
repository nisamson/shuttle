using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.FluentUI.AspNetCore.Components;
using Shuttle.WebClient;
using Shuttle.WebClient.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// The API base URL is supplied via wwwroot config: appsettings.json points at the production
// API (https://api.shl.nes.sh) and appsettings.Development.json overrides it with the local
// Aspire dev-server URL. The host base address is only a last-resort fallback.
var apiBaseAddress = builder.Configuration["Api:BaseUrl"] ?? builder.HostEnvironment.BaseAddress;
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new(apiBaseAddress) });

builder.Services.AddMsalAuthentication(options => {
        builder.Configuration.Bind("AzureAd", options.ProviderOptions.Authentication);
    }
);

builder.Services.AddFluentUIComponents();
builder.Services.AddLocalStorageServices();
builder.Services.AddSingleton<IShuttleOptionsStorage, ShuttleOptionsLocalStorage>();
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
