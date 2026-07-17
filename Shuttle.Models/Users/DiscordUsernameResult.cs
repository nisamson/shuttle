namespace Shuttle.Models.Users;

/// <summary>
/// Result of looking up a forum member's self-reported Discord username, returned by the
/// development-only <c>GET /debug/users/{userId}/discord</c> diagnostic endpoint.
/// </summary>
/// <param name="UserId">The forum member id that was looked up.</param>
/// <param name="DiscordUsername">
/// The Discord username listed on the member's forum profile, or <c>null</c> if none was found.
/// </param>
public record DiscordUsernameResult(int UserId, string? DiscordUsername);
