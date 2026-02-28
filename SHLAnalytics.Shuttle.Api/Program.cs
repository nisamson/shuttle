using Azure.Monitor.OpenTelemetry.AspNetCore;
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;
using Serilog;
using SHLAnalytics.Shuttle.Api.Entities;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console() // + file or centralized logging
    .CreateLogger();

// Add services to the container.
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));

var corsOrigins = builder.Configuration
    .GetSection("Shuttle:AllowedCorsOrigins")
    .Get<string[]>();

if (corsOrigins?.Length is not > 0 && !builder.Environment.IsDevelopment()) {
    throw new InvalidOperationException("Allowed CORS origins must be configured in production.");
}

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

builder.Services.AddControllers();

builder.Services.AddSerilog(
    lc => {
        lc.WriteTo.Console();
        lc.MinimumLevel.Information();
    },
    writeToProviders: true
);

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
ArgumentException.ThrowIfNullOrEmpty(databaseUrl);

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

var optionsBuilder = new DbContextOptionsBuilder<ShlDbContext>();
optionsBuilder.UseSqlServer(connectionString);
optionsBuilder.UseLinqToDB(builder => {
    builder.AddCustomOptions(o => o.UseSqlServer());
});

builder.Services.AddHealthChecks();

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
app.MapStaticAssets();

app.Run();
