using Microsoft.EntityFrameworkCore;
using Shuttle.EFCore.Entities.Portal;

namespace Shuttle.EFCore.Recruitment;

/// <summary>
/// Default <see cref="IRecruitmentAnalysisService"/>: reads the member names and player projections
/// from <see cref="ShlDbContext"/> and delegates the classification/aggregation to
/// <see cref="RecruitmentAnalyzer"/>.
/// </summary>
public sealed class RecruitmentAnalysisService : IRecruitmentAnalysisService {

    private readonly ShlDbContext _db;

    public RecruitmentAnalysisService(ShlDbContext db) {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    /// <inheritdoc/>
    public async Task<RecruitmentAnalysis> GetRecruitmentAnalysisAsync(CancellationToken cancellationToken = default) {
        var memberNames = await _db.Users
            .AsNoTracking()
            .Select(u => u.Name)
            .ToListAsync(cancellationToken);

        // Latest cumulative total TPE per player, taken from the most recent point on its TPE timeline.
        // TpeEvent is keyed by (PlayerId, TaskDate), so joining a player's max TaskDate yields a single row.
        var latestDates = _db.Set<TpeEvent>()
            .GroupBy(e => e.PlayerId)
            .Select(g => new { PlayerId = g.Key, TaskDate = g.Max(e => e.TaskDate) });

        var latestTpe = await _db.Set<TpeEvent>()
            .AsNoTracking()
            .Join(
                latestDates,
                e => new { e.PlayerId, e.TaskDate },
                l => new { l.PlayerId, l.TaskDate },
                (e, l) => new { e.PlayerId, e.TotalTpe })
            .ToListAsync(cancellationToken);

        var latestByPlayer = latestTpe.ToDictionary(x => x.PlayerId, x => (long)x.TotalTpe);

        var playerRows = await _db.PlayerInformation
            .AsNoTracking()
            .IgnoreAutoIncludes()
            .Select(p => new {
                p.UserId,
                p.Username,
                p.PlayerId,
                p.Name,
                p.CreationTime,
                p.Recruiter,
            })
            .ToListAsync(cancellationToken);

        var players = playerRows
            .Select(p => new RecruitedPlayer(
                p.UserId,
                p.Username,
                p.PlayerId,
                p.Name,
                p.CreationTime,
                p.Recruiter,
                latestByPlayer.GetValueOrDefault(p.PlayerId)))
            .ToList();

        return RecruitmentAnalyzer.Aggregate(players, memberNames);
    }
}
