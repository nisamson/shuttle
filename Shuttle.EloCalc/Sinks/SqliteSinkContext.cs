using Microsoft.EntityFrameworkCore;
using Shuttle.EloCalc.Sinks.Entities;

namespace Shuttle.EloCalc.Sinks;

public class SqliteSinkContext : DbContext {
    public SqliteSinkContext(DbContextOptions<SqliteSinkContext> options) : base(options) { }

    public DbSet<RankingUpdate> RankingUpdates { get; set; } = null!;
    public DbSet<Team> Teams { get; set; } = null!;
}
