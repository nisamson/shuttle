using LinqToDB.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.TokenCacheProviders.InMemory;
using Microsoft.Identity.Web.UI;
using Shuttle.Api.Quartz;
using Shuttle.EFCore;
using Shuttle.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// API authentication: JWT bearer (default scheme) for the protected backend API.
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));

// Dashboard authentication: interactive OpenID Connect + cookie for the CrystalQuartz
// dashboard, layered on top without changing the default (JWT) scheme.
builder.Services.AddAuthentication()
    .AddMicrosoftIdentityWebApp(
        builder.Configuration,
        QuartzSetup.OptionsSectionName,
        QuartzSetup.OptionsAuthScheme,
        displayName: QuartzSetup.OptionsSectionName
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
app.UseStaticFiles();

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

// Migrations, temporal history and the Quartz schema must all be in place before the
// scheduler starts (AddQuartzServer awaits application startup).
await app.EnsureShuttleDatabaseConnectivity();
await app.EnsureQuartzSchema();

await app.StartAsync();
await app.WaitForShutdownAsync();
