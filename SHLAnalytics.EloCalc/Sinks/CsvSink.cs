using System.Drawing;
using System.Globalization;
using CsvHelper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SHLAnalytics.Api.Models.Index.V1;
using SHLAnalytics.Math;

namespace SHLAnalytics.EloCalc.Sinks;

public class CsvSink : IResultSink {
    
    private readonly string filePath;
    private readonly ILogger<CsvSink> logger;

    public CsvSink(CliOptions options, ILogger<CsvSink>? logger = null) {
        this.logger = logger ?? NullLogger<CsvSink>.Instance;
        filePath = string.IsNullOrEmpty(options.OutputFile) ? "elocalc_results.csv" : options.OutputFile;
    }
    
    public async ValueTask StoreResults(int season, IList<TeamPlayerSeasonRankings> data) {
        logger.LogInformation("Storing Elo rankings to CSV file at {filePath}", filePath);
        await using var writer = File.CreateText(filePath);
        var header = string.Join(",",data.Select(d => d.Team.Abbreviation));
        await writer.WriteLineAsync(header);
        // pivot data to have one row per game played so that each column is a team and each row is the ratings after each game
        var maxGames = data.Max(d => d.Ratings.Count);
        for (var gameIndex = 0; gameIndex < maxGames; gameIndex++) {
            var row = new List<string>();
            foreach (var teamData in data) {
                var player = teamData.Ratings.ElementAtOrDefault(gameIndex);
                if (player != null) {
                    row.Add(player.Rating.Value.ToString(CultureInfo.InvariantCulture));
                } else {
                    row.Add(string.Empty);
                }
            }
            var line = string.Join(",", row);
            await writer.WriteLineAsync(line);
        }

        await writer.FlushAsync();
        logger.LogInformation("Finished writing CSV file.");
    }
}

file class CsvRecord {
    
    public string Team { get; }
    
    public int Game { get; }
    
    public int Rating { get; }
    
    public int Season { get; }
    
    public string PrimaryColor { get; }
    
    public CsvRecord(Team team, Player player) {
        Team = team.Abbreviation;
        PrimaryColor = ColorTranslator.ToHtml(team.Colors.Primary);
        Game = player.GamesPlayed;
        Rating = player.Rating.Value;
        Season = team.Season;
    }
}
