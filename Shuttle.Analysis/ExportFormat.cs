namespace Shuttle.Analysis;

/// <summary>
/// Output formats supported by the player-information export.
/// </summary>
public enum ExportFormat {

    /// <summary>A JSON array of player records.</summary>
    Json,

    /// <summary>A flat CSV table with one row per player.</summary>
    Csv,
}
