using Azure.Identity;
using MudBlazor.Services;
using Serilog;
using SHLAnalytics.WebApp.Components;
using SHLAnalytics.WebApp.Options;
using SHLAnalytics.WebApp.Services.Blobs;
using SHLAnalytics.WebApp.Services.IO;
using SHLAnalytics.WebApp.Services.Jobs;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddMudServices();
builder.Services.AddSerilog(lc => {
    lc.WriteTo.Console();
}, writeToProviders:true);
builder.Services.AddSingleton<IIoConfiguration, IoConfiguration>();

builder.Logging.AddOpenTelemetry(l => {
        l.IncludeScopes = true;
    }
);

builder.AddAzureBlobServiceClient("blobs",
    c => {
        if (builder.Environment.IsDevelopment()) {
            c.Credential = new AzureCliCredential();
        }
    });

var options = new CommonOptions();
builder.Configuration.GetSection(CommonOptions.SectionName).Bind(options);
builder.Services.AddJobs(options.GetFileStorageLocation());

builder.Services.AddControllers();

if (builder.Environment.IsDevelopment()) {
    builder.Services.AddScoped<IBlobReaderWriter, DevBlobReaderWriter>();
} else {
    builder.Services.AddScoped<IBlobReaderWriter, BlobReaderWriter>();
}

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment()) {
    app.UseExceptionHandler("/Error");

    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapControllers();
if (app.Environment.IsDevelopment()) {
    app.MapOpenApi();
}
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();
await app.Services.EnsureQuartzDatabaseExists();

app.Run();