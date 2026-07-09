using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Shuttle.EFCore.Entities;
using Shuttle.EFCore.Entities.Index;
using Shuttle.EFCore.Entities.Performance;
using Shuttle.EFCore.Entities.Portal;
using Shuttle.EFCore.Entities.Ratings;

namespace Shuttle.EFCore;

public class ShlDbContext : DbContext {

    internal ILogger<ShlDbContext> Logger { get; }

    public ShlDbContext(DbContextOptions<ShlDbContext> options, ILogger<ShlDbContext> logger) : base(options) {
        this.Logger = logger;
    }
    
    public DbSet<ShlUser> Users { get; set; }

    public DbSet<League> Leagues { get; set; }
    
    public DbSet<LeagueSeason> Seasons { get; set; }

    public DbSet<Conference> Conferences { get; set; }

    public DbSet<Division> Divisions { get; set; }

    public DbSet<GameResult> GameResults { get; set; }
    
    public DbSet<PlayerInformation> PlayerInformation { get; set; }
    
    public DbSet<MostRecentUserPlayer> MostRecentUserPlayers { get; set; }
    
    public DbSet<IndexRecord> IndexRecords { get; set; }
    
    public DbSet<Team> Teams { get; set; }

}
