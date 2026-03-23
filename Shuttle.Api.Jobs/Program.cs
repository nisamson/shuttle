using LinqToDB.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;
using Quartz;
using Shuttle.Api.Jobs;
using Shuttle.EFCore;
using Shuttle.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddMicrosoftIdentityWebAppAuthentication(builder.Configuration.GetSection("AzureAd"));

// Add services to the container.

builder.Services.AddControllers();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.AddShuttleDatabase();
builder.AddQuartz();

var app = builder.Build();

LinqToDBForEFTools.Initialize();
var db = app.Services.GetRequiredService<ShlDbContext>();
db.Database.ExecuteSql($"SELECT 1"); // Ensure database is created and migrations are applied at startup.

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment()) {
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapQuartzDashboard();
app.MapControllers()
    .DisableCookieRedirect();

app.Run();
