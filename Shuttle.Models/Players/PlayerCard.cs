using Shuttle.Shl.Api.Models.Common;
using Shuttle.Shl.Api.Models.Portal.V1;

namespace Shuttle.Models.Players;

/// <summary>
/// Lean "at a glance" player card returned by the public <c>GET /player/{playerId}</c> endpoint
/// and consumed by the Blazor WebClient. Carries a player's identity, TPE/bank totals,
/// team/rights, and their current in-game (Franchise Hockey Manager) <see cref="Attributes"/> —
/// either a <see cref="SkaterAttributes"/> or a <see cref="GoaltenderAttributes"/> depending on
/// position. Index records and boxscore stats are intentionally omitted from this first pass.
/// </summary>
public record PlayerCard {
    // Identity
    public required int PlayerId { get; init; }
    public required int UserId { get; init; }
    public required string Username { get; init; }
    public required string Name { get; init; }
    public required PlayerStatus Status { get; init; }
    public required PlayerPosition Position { get; init; }
    public required PlayerHandedness Handedness { get; init; }
    public DateTime CreationDate { get; init; }
    public DateTime? RetirementDate { get; init; }
    public int? JerseyNumber { get; init; }
    public Height? Height { get; init; }
    public int? Weight { get; init; }
    public string? Birthplace { get; init; }
    public string? IihfNation { get; init; }
    public int? DraftSeason { get; init; }
    public bool IsSuspended { get; init; }
    public bool Inactive { get; init; }

    /// <summary>
    /// <see langword="true"/> when this is not the user's earliest-created player — i.e. the member
    /// already had a player before this one (a "recreate"). <see langword="false"/> for a member's
    /// first-ever (first-gen) player.
    /// </summary>
    public bool Recreate { get; init; }

    // TPE / bank
    public required int TotalTpe { get; init; }
    public required int AppliedTpe { get; init; }
    public required int BankedTpe { get; init; }
    public required int BankBalance { get; init; }

    // Team / rights
    public KnownLeague? CurrentLeague { get; init; }
    public int? CurrentTeamId { get; init; }
    public int? ShlRightsTeamId { get; init; }
    public int? SmjhlRightsTeamId { get; init; }

    /// <summary>
    /// The player's current in-game attributes. Exactly one concrete type is present depending on
    /// position: <see cref="SkaterAttributes"/> for skaters, <see cref="GoaltenderAttributes"/> for
    /// goaltenders. <see langword="null"/> when the player has no ingested attributes yet.
    /// </summary>
    public PlayerAttributes? Attributes { get; init; }
}
