using Shuttle.Core.DataProtection;
using Shuttle.EFCore.Resilience;

namespace Shuttle.EFCore;

public record ShuttleConnectionString(
    string ConnectionString) : IConnectionStringProvider<ShlDbContext> {
    public static implicit operator string(ShuttleConnectionString connectionString) => connectionString.ConnectionString;
}
