using Shuttle.Core.DataProtection;

namespace Shuttle.EFCore;

public record ShuttleConnectionString(
    string ConnectionString) {
    public static implicit operator string(ShuttleConnectionString connectionString) => connectionString.ConnectionString;
}
