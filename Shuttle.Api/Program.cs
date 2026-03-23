using LinqToDB.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;
using Shuttle.EFCore;
using Shuttle.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));

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

builder.Services.AddControllers();

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
app.MapDefaultEndpoints();
app.MapStaticAssets();

app.UseOutputCache();
app.UseRequestTimeouts();

app.Run();
