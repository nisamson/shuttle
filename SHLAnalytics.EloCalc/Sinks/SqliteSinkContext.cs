using Microsoft.EntityFrameworkCore;
using SHLAnalytics.EloCalc.Sinks.Entities;

namespace SHLAnalytics.EloCalc.Sinks;

public class SqliteSinkContext : DbContext {
    public SqliteSinkContext(DbContextOptions<SqliteSinkContext> options) : base(options) { }

    public DbSet<RankingUpdate> RankingUpdates { get; set; } = null!;
    public DbSet<Team> Teams { get; set; } = null!;
}
