using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SHLAnalytics.EloCalc.Sinks.Entities;

namespace SHLAnalytics.EloCalc.Sinks;

public class SqliteSink : IResultSink {

    private readonly string filePath;
    private readonly ILogger<SqliteSink> logger;

    public SqliteSink(CliOptions options, ILogger<SqliteSink>? logger = null) {
        this.logger = logger ?? NullLogger<SqliteSink>.Instance;
        filePath = string.IsNullOrEmpty(options.OutputFile) ? "elocalc_results.sqlite" : options.OutputFile;
    }
    
    public async ValueTask StoreResults(int season, IList<TeamPlayerSeasonRankings> data) {
        using var scope = logger.BeginScope("{Sink} StoreResults to {filePath}", nameof(SqliteSink), filePath);
        logger.LogInformation("Storing results to SQLite database");
        await using var context = CreateContext();
        await context.Database.EnsureCreatedAsync();
        await using var tx = await context.Database.BeginTransactionAsync();

        await context.Teams.Where(t => t.Season == season)
            .ExecuteDeleteAsync();
        await context.Teams.AddRangeAsync(data.Select(d => new Team(d.Team)));
        var ratings = data.SelectMany(d => d.Ratings.Select(r => (d.Team.Id, r)))
            .Select(p => new RankingUpdate(p.Id, p.r));
        await context.RankingUpdates.AddRangeAsync(ratings);

        await tx.CommitAsync();
    }

    private SqliteSinkContext CreateContext() {
        var contextOptions = new DbContextOptionsBuilder<SqliteSinkContext>()
            .UseSqlite($"Data Source={filePath}")
            .Options;

        return new(contextOptions);
    }
}
