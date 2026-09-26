namespace Shuttle.Models.Players;

/// <summary>Known persisted development-projection algorithm versions.</summary>
public static class DevelopmentProjectionAlgorithmVersions {
    public const int Current = 3;
}

/// <summary>The availability of a player's latest development projection.</summary>
public enum DevelopmentProjectionStatus {
    /// <summary>A projection has been generated and is present in the response.</summary>
    Available,

    /// <summary>No projection run has been published for the player yet.</summary>
    NotGenerated,

    /// <summary>The latest run could not project the player because too little history exists.</summary>
    InsufficientObservedData,
}

/// <summary>
/// The latest development-projection status for an existing player. A missing player is represented
/// by HTTP 404 rather than this contract.
/// </summary>
public sealed record PlayerDevelopmentProjectionResult {
    /// <summary>The SHL player id.</summary>
    public required int PlayerId { get; init; }

    /// <summary>The projection algorithm version used by the run.</summary>
    public int AlgorithmVersion { get; init; } = DevelopmentProjectionAlgorithmVersions.Current;

    /// <summary>The projection's availability in the latest published run.</summary>
    public required DevelopmentProjectionStatus Status { get; init; }

    /// <summary>The database update represented by the latest run, when one has been published.</summary>
    public DateTimeOffset? DataAsOf { get; init; }

    /// <summary>When the latest run was generated, when one has been published.</summary>
    public DateTimeOffset? GeneratedAt { get; init; }

    /// <summary>Whether the latest run represents the current database freshness signal.</summary>
    public required bool IsCurrent { get; init; }

    /// <summary>The projection when <see cref="Status"/> is <see cref="DevelopmentProjectionStatus.Available"/>.</summary>
    public PlayerDevelopmentProjection? Projection { get; init; }
}

/// <summary>
/// A persisted development-curve projection for one player.
/// </summary>
public sealed record PlayerDevelopmentProjection {
    /// <summary>The SHL player id.</summary>
    public required int PlayerId { get; init; }

    /// <summary>The database update against which this projection was computed.</summary>
    public required DateTimeOffset DataAsOf { get; init; }

    /// <summary>When the projection was generated.</summary>
    public required DateTimeOffset GeneratedAt { get; init; }

    /// <summary>
    /// Whether this projection was generated from the latest successfully refreshed database data.
    /// </summary>
    public required bool IsCurrent { get; init; }

    /// <summary>The fixed number of days between observed and projected points.</summary>
    public required int CadenceDays { get; init; }

    /// <summary>The latest known cumulative TPE used as the forecast baseline.</summary>
    public required int BaselineTotalTpe { get; init; }

    /// <summary>
    /// The latest actual TPE event from which the forecast continues. Older projection payloads may
    /// omit this value; clients then infer it from the first projected point and cadence.
    /// </summary>
    public DateTimeOffset? BaselineDate { get; init; }

    /// <summary>The player's observed, fixed-cadence cumulative TPE curve.</summary>
    public required IReadOnlyList<DevelopmentCurvePoint> ObservedPoints { get; init; }

    /// <summary>The peer-derived P10/P50/P90 cumulative TPE forecast.</summary>
    public required IReadOnlyList<DevelopmentProjectionPoint> ProjectedPoints { get; init; }

    /// <summary>The closest historical development curves used as forecast peers.</summary>
    public required IReadOnlyList<DevelopmentSimilarity> SimilarPlayers { get; init; }
}

/// <summary>A fixed-cadence cumulative TPE observation.</summary>
public sealed record DevelopmentCurvePoint {
    /// <summary>The UTC instant represented by this point.</summary>
    public required DateTimeOffset Date { get; init; }

    /// <summary>The cumulative TPE at <see cref="Date"/>.</summary>
    public required int TotalTpe { get; init; }
}

/// <summary>A peer-derived cumulative TPE forecast point.</summary>
public sealed record DevelopmentProjectionPoint {
    /// <summary>The future UTC instant represented by this point.</summary>
    public required DateTimeOffset Date { get; init; }

    /// <summary>The tenth-percentile projected cumulative TPE.</summary>
    public required double P10 { get; init; }

    /// <summary>The median projected cumulative TPE.</summary>
    public required double P50 { get; init; }

    /// <summary>The ninetieth-percentile projected cumulative TPE.</summary>
    public required double P90 { get; init; }

    /// <summary>The number of peers contributing to this horizon.</summary>
    public required int PeerCount { get; init; }
}

/// <summary>A historical player whose development curve resembles the requested player's curve.</summary>
public sealed record DevelopmentSimilarity {
    /// <summary>The similar player's SHL player id.</summary>
    public required int PlayerId { get; init; }

    /// <summary>The similar player's display name at generation time.</summary>
    public required string Name { get; init; }

    /// <summary>The similar player's username at generation time.</summary>
    public required string Username { get; init; }

    /// <summary>A bounded similarity score where 1 is identical and 0 is maximally dissimilar.</summary>
    public required double Score { get; init; }

    /// <summary>
    /// The path-length-normalized DTW distance after endpoint and earning-rate penalties.
    /// </summary>
    public required double Distance { get; init; }
}
