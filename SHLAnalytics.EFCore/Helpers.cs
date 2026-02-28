using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SHLAnalytics.EFCore;

public static class Constants {
    public const string ValidFrom = "ValidFrom";
    public const string ValidTo = "ValidTo";
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

        void ConfigTable(TableBuilder<TEntity> t) {
            config?.Invoke(t);
            t.IsTemporal(t => {
                    t.HasPeriodStart(Constants.ValidFrom);
                    t.HasPeriodEnd(Constants.ValidTo);
                }
            );
        }
    }
}
