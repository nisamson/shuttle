using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SHLAnalytics.EFCore;

public static class Constants {
    public const string ValidFrom = "ValidFrom";
    public const string ValidTo = "ValidTo";
}

public record Temporal<T> where T : class {
    public required T Item { get; init; }
    public required DateTime ValidFrom { get; init; }
    public required DateTime ValidTo { get; init; }
    
    public static implicit operator T(Temporal<T> temporal) => temporal.Item;
}

public static class Helpers {
    
    public static void AddTemporalTableSupport<TEntity>(
        this EntityTypeBuilder<TEntity> builder,
        string? tableName = null,
        Action<TableBuilder<TEntity>>? config = null
    ) where TEntity : class {
        if (tableName is not null) {
            builder.ToTable(tableName, ConfigTable);
        }

        builder.ToTable(ConfigTable);
        return;

        void ConfigTable(TableBuilder<TEntity> t) {
            config?.Invoke(t);
            t.IsTemporal(ttb => {
                    ttb.HasPeriodStart(Constants.ValidFrom);
                    ttb.HasPeriodEnd(Constants.ValidTo);
                }
            );
        }
    }
    
    public static IQueryable<Temporal<TEntity>> AsTemporal<TEntity>(this IQueryable<TEntity> queryable) where TEntity : class {
        return queryable.Select(e => new Temporal<TEntity> {
            Item = e,
            ValidFrom = EF.Property<DateTime>(e, Constants.ValidFrom),
            ValidTo = EF.Property<DateTime>(e, Constants.ValidTo)
        });
    }
}
