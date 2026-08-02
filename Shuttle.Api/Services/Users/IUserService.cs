using Microsoft.Extensions.DependencyInjection;
using Shuttle.EFCore.Entities;

namespace Shuttle.Api.Services.Users;

/// <summary>
/// Owns all persistence and business logic for <see cref="ShuttleUser"/> accounts, so callers other
/// than the HTTP endpoint (background jobs, other controllers) can ensure a user exists and mutate
/// accounts without going through the API surface.
/// </summary>
public interface IUserService {
    /// <summary>
    /// Resolves the account for the given Entra object id, creating it (with a generated id and a
    /// default username derived from that id) if it does not already exist.
    /// </summary>
    Task<ShuttleUser> GetOrCreateAsync(Guid objectId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Ensures the account exists (see <see cref="GetOrCreateAsync"/>) and then attempts to update its
    /// username. Returns a result describing success, invalid input, or a uniqueness conflict.
    /// </summary>
    Task<UpdateUsernameResult> UpdateUsernameAsync(
        Guid objectId,
        string username,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Outcome of <see cref="IUserService.UpdateUsernameAsync"/>. Deliberately free of ASP.NET types so
/// the service is reusable outside of controllers; callers map the outcome to HTTP semantics.
/// </summary>
public abstract record UpdateUsernameResult {
    private UpdateUsernameResult() { }

    /// <summary>The username was updated (or already matched); carries the current account state.</summary>
    public sealed record Success(ShuttleUser User) : UpdateUsernameResult;

    /// <summary>The requested username did not satisfy the format/length rules.</summary>
    public sealed record InvalidUsername : UpdateUsernameResult;

    /// <summary>The requested username is already in use by another account.</summary>
    public sealed record UsernameTaken : UpdateUsernameResult;
}

public static class UserServiceRegistration {
    public static IServiceCollection AddUserService(this IServiceCollection services) {
        services.AddScoped<IUserService, UserService>();
        return services;
    }
}
