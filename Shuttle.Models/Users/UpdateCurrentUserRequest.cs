using System.ComponentModel.DataAnnotations;

namespace Shuttle.Models.Users;

/// <summary>
/// Payload for <c>PUT /users/me</c>. Updates the mutable fields of the caller's own account. Only
/// the username is mutable today; further fields may be added over time.
/// </summary>
public record UpdateCurrentUserRequest {
    /// <summary>
    /// The desired username. Must be 2-32 characters and contain only ASCII letters, digits,
    /// periods, and underscores.
    /// </summary>
    [Required]
    public required string Username { get; init; }
}
