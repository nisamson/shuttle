// See https://aka.ms/new-console-template for more information

using dotenv.net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using SHLAnalytics.Api.Client;
using SHLAnalytics.EloCalc;
using SHLAnalytics.EloCalc.Sinks;

DotEnv.Load();

var parsed = CliOptions.Parse(args);

var host = Host.CreateApplicationBuilder();

var services = host.Services;

services.AddShlApiClients();
services.AddSerilog(sc => {
        sc.Enrich.FromLogContext()
            .WriteTo.Console();
    }
);

switch (parsed.SinkFormat) {
    case SinkFormat.Csv:
        services.AddSingleton<IResultSink, CsvSink>();
        break;
    case SinkFormat.Sqlite:
        services.AddSingleton<IResultSink, SqliteSink>();
        break;
    default:
        throw new ArgumentOutOfRangeException();
}

services.AddSingleton<CliOptions>(_ => parsed);
services.AddSingleton<EloCalculator>();
services.AddScoped<ShlEloDataRetriever>();

var app = host.Build();


foreach (var season in parsed.Seasons) {
    EloCalcData data;
    {
        using var scope = app.Services.CreateScope();
        var svcProvider = scope.ServiceProvider;
        var retriever = svcProvider.GetRequiredService<ShlEloDataRetriever>();
        data = await retriever.GetEloCalcData(parsed.League, season);
    }
    IList<TeamPlayerSeasonRankings> results;
    {
        using var scope = app.Services.CreateScope();
        var svcProvider = scope.ServiceProvider;
        var calculator = svcProvider.GetRequiredService<EloCalculator>();
        results = calculator.CalculateEloRatings(data);
    }
    {
        using var scope = app.Services.CreateScope();
        var svcProvider = scope.ServiceProvider;
        var sink = svcProvider.GetRequiredService<IResultSink>();
        await sink.StoreResults(season, results);
    }
}

