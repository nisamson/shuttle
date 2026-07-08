using LinqToDB;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
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
            var mostRecentPlayers = ctx.PlayerInformation
                .GroupBy(pi => pi.User)
                .Select(grp => grp.MaxBy(pi => pi.CreationTime)!);
            int changed;
            try {
                ctx.Logger.LogInformation("Starting update of most recent players cache table");
                changed = await ctx.MostRecentUserPlayers.Merge()
                    .Using(mostRecentPlayers)
                    .On(tgt => tgt.UserId, src => src.UserId)
                    .InsertWhenNotMatched(src => new() {
                            UserId = src.UserId,
                            PlayerId = src.PlayerId,
                        }
                    )
                    .UpdateWhenMatchedAnd(
                        (tgt, src) => tgt.PlayerId != src.PlayerId,
                        (tgt, src) =>
                            new() {
                                UserId = src.UserId,
                                PlayerId = src.PlayerId,
                            }
                    )
                    .MergeAsync(token);
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
