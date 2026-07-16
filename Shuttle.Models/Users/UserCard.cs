using System.Text.Json.Serialization;
using Shuttle.Models.Players;

namespace Shuttle.Models.Users;

/// <summary>
/// Slim "at a glance" view of an SHL user returned by <c>GET /user/{userId}</c>. Surfaces the
/// identifying information we hold on a member: their user id, forum/portal username, and Discord
/// name when we have one on file.
/// </summary>
public record UserCard {
    public required int UserId { get; init; }
    public required string Username { get; init; }

    /// <summary>
    /// The user's Discord name. Only populated for authenticated callers (and when we have one on
    /// file); omitted from the payload entirely otherwise.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? DiscordName { get; init; }

    /// <summary>
    /// The player cards for players this user has created. <see langword="null"/> when the caller
    /// did not request them; an empty list when requested via <c>?players=true</c> but the user has
    /// created none.
    /// </summary>
    public IReadOnlyList<PlayerCard>? Players { get; init; }
}
