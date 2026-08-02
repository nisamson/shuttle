namespace Shuttle.Models.Users;

/// <summary>
/// Slim "user directory" entry returned by <c>GET /users/suggestions</c>. Carries just enough to
/// power client-side username autocomplete (and navigate to the profile) without shipping the full
/// <see cref="UserCard"/>. Intentionally tiny and Discord-free so the directory stays public and
/// cacheable.
/// </summary>
public record UserSuggestion {
    public required int UserId { get; init; }
    public required string Username { get; init; }
}
