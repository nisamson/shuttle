using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Shuttle.EFCore;

public static class Constants {
    public const string ValidFrom = "ValidFrom";
    public const string ValidTo = "ValidTo";

    // Far-future sentinel used as the ValidTo of the "current" row, mirroring the
    // 9999-12-31 upper bound SQL Server temporal tables use for open-ended periods.
    public const string ValidToOpenEnded = "9999-12-31 23:59:59";

    // Annotation flagging an entity as system-versioned so history tables/triggers are
    // generated for it at startup (see Helpers.EnsureTemporalHistory).
    public const string TemporalAnnotation = "Shuttle:Temporal";

    public const string HistoryTableSuffix = "History";
}

public record Temporal<T> where T : class {
    public required T Item { get; init; }
    public required DateTime ValidFrom { get; init; }
    public required DateTime ValidTo { get; init; }
    
    public static implicit operator T(Temporal<T> temporal) => temporal.Item;
}

public static class Helpers {

    internal static void AddTemporalTableSupport<TEntity>(
        this EntityTypeBuilder<TEntity> builder,
        string? tableName = null,
        Action<TableBuilder<TEntity>>? config = null
    ) where TEntity : class {
        if (tableName is not null) {
            builder.ToTable(tableName, ConfigTable);
        } else {
            builder.ToTable(ConfigTable);
        }

        builder.Property<DateTime>(Constants.ValidFrom)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");
        builder.Property<DateTime>(Constants.ValidTo)
            .HasDefaultValueSql($"'{Constants.ValidToOpenEnded}'");

        builder.Metadata.SetAnnotation(Constants.TemporalAnnotation, true);
        return;

        void ConfigTable(TableBuilder<TEntity> t) {
            config?.Invoke(t);
        }
    }
    
    public static IQueryable<Temporal<TEntity>> AsTemporal<TEntity>(this IQueryable<TEntity> queryable) where TEntity : class {
        return queryable.Select(e => new Temporal<TEntity> {
            Item = e,
            ValidFrom = EF.Property<DateTime>(e, Constants.ValidFrom),
            ValidTo = EF.Property<DateTime>(e, Constants.ValidTo)
        });
    }

    /// <summary>
    /// Creates the shadow history table and the AFTER UPDATE / AFTER DELETE triggers that
    /// emulate SQL Server system-versioned temporal tables on SQLite. Idempotent.
    /// </summary>
    internal static async Task EnsureTemporalHistory(this ShlDbContext ctx, CancellationToken cancellationToken = default) {
        foreach (var statement in BuildTemporalDdl(ctx.Model)) {
            await ctx.Database.ExecuteSqlRawAsync(statement, cancellationToken);
        }
    }

    internal static IEnumerable<string> BuildTemporalDdl(IModel model) {
        foreach (var entityType in model.GetEntityTypes()) {
            if (entityType.FindAnnotation(Constants.TemporalAnnotation)?.Value is not true) {
                continue;
            }

            var table = entityType.GetTableName();
            if (table is null) {
                continue;
            }

            var historyTable = table + Constants.HistoryTableSuffix;
            var columns = entityType.GetProperties()
                .Select(p => p.GetColumnName())
                .Where(c => !string.IsNullOrEmpty(c))
                .Select(c => c!)
                .ToList();
            var keyColumns = entityType.FindPrimaryKey()?.Properties
                .Select(p => p.GetColumnName())
                .Where(c => !string.IsNullOrEmpty(c))
                .Select(c => c!)
                .ToList() ?? [];

            foreach (var statement in BuildTableDdl(table, historyTable, columns, keyColumns)) {
                yield return statement;
            }
        }
    }

    private static IEnumerable<string> BuildTableDdl(
        string table,
        string historyTable,
        IReadOnlyList<string> columns,
        IReadOnlyList<string> keyColumns
    ) {
        // Clone the main table's columns (names + affinities) without any constraints, so the
        // history table can hold many versions per key.
        yield return $"CREATE TABLE IF NOT EXISTS {Quote(historyTable)} AS SELECT * FROM {Quote(table)} WHERE 0;";

        var insertColumns = string.Join(", ", columns.Select(Quote));
        var oldValues = string.Join(", ", columns.Select(c => c switch {
            Constants.ValidTo => "CURRENT_TIMESTAMP",
            _ => $"OLD.{Quote(c)}"
        }));

        var updateTrigger = $"{table}_temporal_update";
        var deleteTrigger = $"{table}_temporal_delete";
        var keyPredicate = keyColumns.Count > 0
            ? string.Join(" AND ", keyColumns.Select(c => $"{Quote(c)} = NEW.{Quote(c)}"))
            : "1 = 1";

        yield return $"DROP TRIGGER IF EXISTS {Quote(updateTrigger)};";
        yield return
            $"""
             CREATE TRIGGER {Quote(updateTrigger)} AFTER UPDATE ON {Quote(table)} FOR EACH ROW
             BEGIN
                 INSERT INTO {Quote(historyTable)} ({insertColumns}) VALUES ({oldValues});
                 UPDATE {Quote(table)} SET {Quote(Constants.ValidFrom)} = CURRENT_TIMESTAMP WHERE {keyPredicate};
             END;
             """;

        yield return $"DROP TRIGGER IF EXISTS {Quote(deleteTrigger)};";
        yield return
            $"""
             CREATE TRIGGER {Quote(deleteTrigger)} AFTER DELETE ON {Quote(table)} FOR EACH ROW
             BEGIN
                 INSERT INTO {Quote(historyTable)} ({insertColumns}) VALUES ({oldValues});
             END;
             """;
    }

    private static string Quote(string identifier) => $"\"{identifier.Replace("\"", "\"\"")}\"";
}
