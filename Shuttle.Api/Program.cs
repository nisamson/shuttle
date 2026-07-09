using LinqToDB.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.TokenCacheProviders.InMemory;
using Microsoft.Identity.Web.UI;
using Shuttle.Api.Jobs;
using Shuttle.EFCore;
using Shuttle.ServiceDefaults;
using Shuttle.Shl.Api.Client;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container.
// JWT bearer is the default scheme so API endpoints keep their JWT/anonymous behaviour.
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));

// The Quartz dashboard uses interactive OpenID Connect sign-in. Registered without
// changing the default scheme, so it only applies to endpoints that opt in via policy.
builder.Services.AddAuthentication()
    .AddMicrosoftIdentityWebApp(
        builder.Configuration,
        Startup.OptionsSectionName,
        displayName: Startup.OptionsSectionName
    );
builder.Services.AddInMemoryTokenCaches();

var corsOrigins = builder.Configuration
    .GetSection("Shuttle:AllowedCorsOrigins")
    .Get<string[]>();

if (corsOrigins?.Length is not > 0 && !builder.Environment.IsDevelopment()) {
    throw new InvalidOperationException("Allowed CORS origins must be configured in production.");
}

builder.Services.AddAntiforgery();

builder.Services.AddCors(options => {
        options.AddDefaultPolicy(policy => {
                if (builder.Environment.IsDevelopment()) {
                    policy.SetIsOriginAllowed(orig => Uri.TryCreate(orig, UriKind.Absolute, out var uri) && uri.IsLoopback);
                } else {
                    ArgumentNullException.ThrowIfNull(corsOrigins);
                    policy.WithOrigins(corsOrigins);
                }
                policy.AllowAnyMethod();
                policy.AllowAnyHeader();
            }
        );
    }
);

builder.Services.AddShlApiClients();

builder.AddShuttleDatabase();
builder.AddQuartz();

builder.Services.AddControllersWithViews()
    .AddMicrosoftIdentityUI();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

LinqToDBForEFTools.Initialize();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment()) {
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseCors();

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapControllers();
app.MapGet("/MicrosoftIdentity/Account/AccessDenied", () =>
    Results.Text("403 Forbidden — you do not have the required role to access this resource.\n\nSign out and back in at /MicrosoftIdentity/Account/SignOut", statusCode: 403))
    .AllowAnonymous();

if (app.Environment.IsDevelopment()) {
    app.MapGet("/debug/claims", (HttpContext context) => {
        if (context.User.Identity?.IsAuthenticated != true)
            return Results.Text("Not authenticated");

        var claims = context.User.Claims
            .Select(c => $"{c.Type} = {c.Value}");
        return Results.Text(string.Join("\n", claims));
    }).RequireAuthorization();
}

app.AddQuartz();

app.MapDefaultEndpoints();
app.MapStaticAssets();

app.UseOutputCache();
app.UseRequestTimeouts();

await app.StartAsync();
await app.EnsureShuttleDatabaseConnectivity();
await app.WaitForShutdownAsync();
