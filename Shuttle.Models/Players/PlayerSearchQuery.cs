using Shuttle.Shl.Api.Models.Common;
using Shuttle.Shl.Api.Models.Portal.V1;

namespace Shuttle.Models.Players;

/// <summary>
/// The set of filters, paging, and sort options accepted by the <c>GET /players/search</c> endpoint
/// and sent by the Refit client as a flattened query string. Every property is optional; unset /
/// empty properties are ignored by the server.
/// <para>
/// The enum-like filters (<see cref="Positions"/>, <see cref="Statuses"/>, <see cref="Leagues"/>,
/// <see cref="Handedness"/>) are multiselect: a player matches when it equals any value in the
/// collection (OR within a field), and all supplied filters must match (AND across fields).
/// </para>
/// </summary>
public record PlayerSearchQuery {
    /// <summary>Free-text match against player name or username (case-insensitive contains).</summary>
    public string? Text { get; init; }

    /// <summary>
    /// Positions to include, expressed as short codes ("G", "C", "LW", "RW", "LD", "RD"). Short
    /// codes are used to avoid the ambiguity of the <see cref="PlayerPosition"/> <c>[Flags]</c> enum.
    /// </summary>
    public IReadOnlyList<string>? Positions { get; init; }

    /// <summary>Lifecycle statuses to include.</summary>
    public IReadOnlyList<PlayerStatus>? Statuses { get; init; }

    /// <summary>Current leagues to include.</summary>
    public IReadOnlyList<KnownLeague>? Leagues { get; init; }

    /// <summary>Restrict to a single draft season.</summary>
    public int? DraftSeason { get; init; }

    /// <summary>Inclusive lower bound on total TPE.</summary>
    public int? MinTotalTpe { get; init; }

    /// <summary>Inclusive upper bound on total TPE.</summary>
    public int? MaxTotalTpe { get; init; }

    /// <summary>Handedness values to include.</summary>
    public IReadOnlyList<PlayerHandedness>? Handedness { get; init; }

    /// <summary>Restrict to players representing this IIHF nation (case-insensitive contains).</summary>
    public string? IihfNation { get; init; }

    /// <summary>Filter by on-site inactivity flag. <see langword="null"/> matches any.</summary>
    public bool? Inactive { get; init; }

    /// <summary>Filter by suspension flag. <see langword="null"/> matches any.</summary>
    public bool? Suspended { get; init; }

    /// <summary>
    /// Filter by "recreate" status (a player that is not the user's earliest-created player).
    /// <see langword="true"/> returns only recreates, <see langword="false"/> only first-gen
    /// players, and <see langword="null"/> matches any.
    /// </summary>
    public bool? Recreate { get; init; }

    /// <summary>Inclusive lower bound on bank balance.</summary>
    public int? MinBankBalance { get; init; }

    /// <summary>Inclusive upper bound on bank balance.</summary>
    public int? MaxBankBalance { get; init; }

    /// <summary>The 1-based page number to return. Defaults to the first page.</summary>
    public int Page { get; init; } = 1;

    /// <summary>The page size. The server clamps this to the range 1..100.</summary>
    public int PageSize { get; init; } = 25;

    /// <summary>The field to sort by. Defaults to <see cref="PlayerSortField.Name"/>.</summary>
    public PlayerSortField SortBy { get; init; } = PlayerSortField.Name;

    /// <summary>Whether to sort in descending order. Defaults to ascending.</summary>
    public bool SortDescending { get; init; }
}
