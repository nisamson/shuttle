using LinqToDB.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.TokenCacheProviders.InMemory;
using Microsoft.Identity.Web.UI;
using Quartz;
using Shuttle.Api.Jobs;
using Shuttle.EFCore;
using Shuttle.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddAuthentication(Startup.OptionsAuthScheme)
    .AddMicrosoftIdentityWebApp(
        builder.Configuration,
        Startup.OptionsSectionName,
        displayName: Startup.OptionsSectionName
    );
builder.Services.AddAntiforgery();
builder.Services.AddInMemoryTokenCaches();

// Add services to the container.
builder.Services.AddControllersWithViews()
    .AddMicrosoftIdentityUI();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.AddShuttleDatabase();
builder.AddQuartz();

var app = builder.Build();

LinqToDBForEFTools.Initialize();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment()) {
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
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
await app.EnsureShuttleDatabaseConnectivity();
await app.RunAsync();
