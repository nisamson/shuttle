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
            // Select the most recent player per user (max CreationTime, tie-broken by max PlayerId) using an
            // anti-join. This translates to a single SQL NOT EXISTS query; LINQ operators such as GroupBy/MaxBy
            // are not translatable by linq2db as a MERGE source.
            var mostRecentPlayers = ctx.PlayerInformation
                .Where(pi => !ctx.PlayerInformation.Any(other =>
                    other.UserId == pi.UserId
                    && (other.CreationTime > pi.CreationTime
                        || (other.CreationTime == pi.CreationTime && other.PlayerId > pi.PlayerId))));
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
