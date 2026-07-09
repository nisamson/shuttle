using Microsoft.EntityFrameworkCore;

namespace Shuttle.EFCore.Procedures;

/// <summary>
/// Provider-agnostic upsert helpers used in place of linq2db's SQL-Server-only
/// <c>Merge</c> API. Existing rows are loaded, compared, and only updated when the
/// supplied <paramref name="changed"/> predicate reports a difference — this keeps the
/// temporal history triggers from recording spurious versions on unchanged rows.
/// </summary>
internal static class UpsertExtensions {
    public static async Task<int> UpsertAsync<TEntity, TKey>(
        this ShlDbContext ctx,
        IReadOnlyCollection<TEntity> incoming,
        IQueryable<TEntity> existingQuery,
        Func<TEntity, TKey> key,
        Func<TEntity, TEntity, bool>? changed,
        Action<TEntity, TEntity>? apply,
        CancellationToken token = default
    ) where TEntity : class where TKey : notnull {
        var existing = await existingQuery.ToDictionaryAsync(key, token);
        var changes = 0;
        foreach (var src in incoming) {
            var k = key(src);
            if (existing.TryGetValue(k, out var tgt)) {
                if (changed is not null && apply is not null && changed(tgt, src)) {
                    apply(tgt, src);
                    changes++;
                }
            } else {
                await ctx.Set<TEntity>().AddAsync(src, token);
                existing[k] = src;
                changes++;
            }
        }

        await ctx.SaveChangesAsync(token);
        return changes;
    }
}
