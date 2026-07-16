using Shuttle.Shl.Api.Models.Common;
using Shuttle.Shl.Api.Models.Portal.V1;

namespace Shuttle.Models.Players;

/// <summary>
/// Slim "player directory" entry returned by <c>GET /players/suggestions</c>. Carries just enough
/// to power client-side name/username autocomplete (and navigate to the profile) without shipping
/// the full <see cref="PlayerCard"/>. The WebClient fetches the whole directory once and filters it
/// locally, so this type is intentionally tiny.
/// </summary>
public record PlayerSuggestion {
    public required int PlayerId { get; init; }
    public required string Name { get; init; }
    public required string Username { get; init; }
    public required PlayerStatus Status { get; init; }
    public required PlayerPosition Position { get; init; }
}
