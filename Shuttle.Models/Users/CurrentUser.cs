namespace Shuttle.Models.Users;

/// <summary>
/// The authenticated caller's own account, returned by <c>GET /users/me</c> and
/// <c>PUT /users/me</c>. Carries the account's stable id and the mutable Shuttle username.
/// </summary>
public record CurrentUser {
    /// <summary>The stable, generated identifier for this account.</summary>
    public required Guid Id { get; init; }

    /// <summary>The user-chosen Shuttle username. Editable via <c>PUT /users/me</c>.</summary>
    public required string Username { get; init; }
}
