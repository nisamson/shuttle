namespace Shuttle.EFCore;

public interface IEntityConvertible<TEntity, TTarget> {
    static abstract TEntity FromModel(TTarget original);
    TTarget ToModel();
}
