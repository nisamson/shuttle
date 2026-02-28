using System.IdentityModel.Tokens.Jwt;
using Azure.Identity;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Data.SqlClient;
using Microsoft.Identity.Web;
using MudBlazor.Services;
using Serilog;
using SHLAnalytics.WebApp.Components;
using SHLAnalytics.WebApp.Options;
using SHLAnalytics.WebApp.Services.IO;
using SHLAnalytics.WebApp.Services.Jobs;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console() // + file or centralized logging
    .CreateLogger();

builder.Services.AddControllers();

builder.Services.AddSerilog(
    lc => {
        lc.WriteTo.Console();
        lc.MinimumLevel.Information();
    },
    writeToProviders: true
);
builder.Services.AddSingleton<IIoConfiguration, IoConfiguration>();

builder.Services.AddOpenTelemetry()
    .UseAzureMonitor(opt => {
            if (builder.Environment.IsDevelopment()) {
                opt.ConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"];
            }
        }
    );

builder.Logging.AddOpenTelemetry(l => { l.IncludeScopes = true; }
);

var databaseName = builder.Configuration["Database:DatabaseName"];
ArgumentException.ThrowIfNullOrWhiteSpace(databaseName);

var databaseUrl = builder.Configuration["Database:DatabaseUrl"]
    ?? Environment.GetEnvironmentVariable("DATABASE_URL");
var authMethod = SqlAuthenticationMethod.ActiveDirectoryDefault;
if (builder.Environment.IsDevelopment()) {
    authMethod = SqlAuthenticationMethod.ActiveDirectoryInteractive;
}

var connectionStringBuilder = new SqlConnectionStringBuilder() {
    DataSource = databaseUrl,
    InitialCatalog = databaseName,
    PersistSecurityInfo = false,
    Encrypt = true,
    TrustServerCertificate = false,
    ConnectTimeout = 30,
    Authentication = authMethod
};
var connectionString = connectionStringBuilder.ToString();

var options = new CommonOptions();
builder.Configuration.GetSection(CommonOptions.SectionName).Bind(options);
builder.Services.AddJobs(options.GetFileStorageLocation());
builder.Services.AddHealthChecks();

JwtSecurityTokenHandler.DefaultMapInboundClaims = false;

var app = builder.Build();

app.MapHealthChecks("/health");

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment()) {
    app.UseExceptionHandler("/Error");

    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseAuthentication();
app.UseAuthorization();
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapControllers();
if (app.Environment.IsDevelopment()) {
    app.MapOpenApi();
}

app.MapStaticAssets();
await app.Services.EnsureQuartzDatabaseExists();

app.Run();
