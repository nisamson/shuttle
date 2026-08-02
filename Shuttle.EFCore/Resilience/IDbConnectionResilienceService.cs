using System.Data;
using Microsoft.EntityFrameworkCore;

namespace Shuttle.EFCore.Resilience;

public interface IDbConnectionResilienceService<TTag> {
    public Type Tag => typeof(TTag);
    Task EnsureDbConnectivity(CancellationToken cancellationToken = default);
}
