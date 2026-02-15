namespace SHLAnalytics.EFCore;

public interface IEntityConvertible<TEntity, TTarget> {
    static abstract TEntity From(TTarget original);
    TTarget To();
}
