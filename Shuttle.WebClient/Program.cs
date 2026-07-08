using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor;
using MudBlazor.Services;
using Shuttle.WebClient;
using Shuttle.WebClient.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new(builder.HostEnvironment.BaseAddress) });

builder.Services.AddMsalAuthentication(options => {
        builder.Configuration.Bind("AzureAd", options.ProviderOptions.Authentication);
    }
);

builder.Services.AddMudServices();
builder.Services.AddMudBlazorDialog();
builder.Services.AddMudMarkdownServices();
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
