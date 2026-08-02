using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Shuttle.EFCore;
using Shuttle.EFCore.Entities;

namespace Shuttle.Api.Services.Users;

/// <summary>
/// Default <see cref="IUserService"/> implementation backed by <see cref="ShlDbContext"/>.
/// </summary>
public partial class UserService : IUserService {
    private readonly ShlDbContext db;
    private readonly ILogger<UserService> logger;

    public UserService(ShlDbContext db, ILogger<UserService> logger) {
        this.db = db;
        this.logger = logger;
    }

    public async Task<ShuttleUser> GetOrCreateAsync(Guid objectId, CancellationToken cancellationToken = default) {
        var user = await db.ShuttleUsers.FirstOrDefaultAsync(u => u.ObjectId == objectId, cancellationToken);
        if (user is not null) {
            return user;
        }

        var id = Guid.CreateVersion7();
        user = new ShuttleUser {
            Id = id,
            ObjectId = objectId,
            // Default username derived from the primary key; 32 hex chars, valid under the username rules.
            Username = id.ToString("N"),
        };
        db.ShuttleUsers.Add(user);

        try {
            await db.SaveChangesAsync(cancellationToken);
        } catch (DbUpdateException) {
            // Another concurrent request created the account first; fall back to the persisted row.
            db.Entry(user).State = EntityState.Detached;
            user = await db.ShuttleUsers.FirstAsync(u => u.ObjectId == objectId, cancellationToken);
        }

        return user;
    }

    public async Task<UpdateUsernameResult> UpdateUsernameAsync(
        Guid objectId,
        string username,
        CancellationToken cancellationToken = default) {
        var normalized = username?.Trim() ?? string.Empty;
        if (!UsernameRegex().IsMatch(normalized)) {
            return new UpdateUsernameResult.InvalidUsername();
        }

        var user = await GetOrCreateAsync(objectId, cancellationToken);

        if (string.Equals(user.Username, normalized, StringComparison.Ordinal)) {
            return new UpdateUsernameResult.Success(user);
        }

        var taken = await db.ShuttleUsers
            .AsNoTracking()
            .AnyAsync(u => u.Id != user.Id && u.Username == normalized, cancellationToken);
        if (taken) {
            return new UpdateUsernameResult.UsernameTaken();
        }

        user.Username = normalized;
        try {
            await db.SaveChangesAsync(cancellationToken);
        } catch (DbUpdateException ex) {
            logger.LogWarning(ex, "Concurrent username update conflict for account {AccountId}", user.Id);
            return new UpdateUsernameResult.UsernameTaken();
        }

        return new UpdateUsernameResult.Success(user);
    }

    // 2-32 characters: ASCII letters, digits, periods, and underscores.
    [GeneratedRegex(@"^[A-Za-z0-9._]{2,32}$")]
    private static partial Regex UsernameRegex();
}
