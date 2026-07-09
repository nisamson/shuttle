using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shuttle.EFCore.Entities.Performance;
using Shuttle.EFCore.Entities.Portal;

namespace Shuttle.EFCore.Procedures;

public static class UpdateCaches {
    extension(ShlDbContext ctx) {

        public async Task UpdatePortalCacheTables(CancellationToken token = default) {
            using var activity =  ActivitySources.ShuttleEfCore.StartActivity();
            ctx.Logger.LogInformation("Updating cache tables");
            try {
                await ctx.UpdateMostRecentPlayers(token);
            } catch (Exception ex) {
                activity?.AddException(ex);
                ctx.Logger.LogError(ex, "Error updating cache tables");
                throw;
            }
            ctx.Logger.LogInformation("Finished updating cache tables");
        }

        private async Task UpdateMostRecentPlayers(CancellationToken token = default) {
            using var activity = ActivitySources.ShuttleEfCore.StartActivity();
            var mostRecentPlayers = (await ctx.PlayerInformation
                    .AsNoTracking()
                    .Select(pi => new { pi.UserId, pi.PlayerId, pi.CreationTime })
                    .ToListAsync(token))
                .GroupBy(pi => pi.UserId)
                .Select(grp => grp.MaxBy(pi => pi.CreationTime)!)
                .Select(pi => new MostRecentUserPlayer {
                        UserId = pi.UserId,
                        PlayerId = pi.PlayerId,
                    }
                )
                .ToList();
            int changed;
            try {
                ctx.Logger.LogInformation("Starting update of most recent players cache table");
                changed = await ctx.UpsertAsync(
                    mostRecentPlayers,
                    ctx.MostRecentUserPlayers.IgnoreAutoIncludes(),
                    m => m.UserId,
                    changed: (t, s) => t.PlayerId != s.PlayerId,
                    apply: (t, s) => t.PlayerId = s.PlayerId,
                    token
                );
            } catch (Exception ex) {
                activity?.AddException(ex);
                ctx.Logger.LogError(ex, "Error updating most recent players cache table");
                throw;
            }

            ctx.Logger.LogInformation(
                "Finished update of most recent players cache table with {Changed} altered records",
                changed
            );
        }
    }
}
