namespace Shuttle.EFCore.Resilience;

public interface IConnectionStringProvider<T> {
    string ConnectionString { get; }
}
