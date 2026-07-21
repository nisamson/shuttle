namespace Shuttle.Api.Services.Scouting;

/// <summary>
/// The outcome category of a scouting operation, mapped by controllers to HTTP status codes.
/// Keeps the service layer free of ASP.NET types while still communicating rich failure reasons.
/// </summary>
public enum ScoutingOutcome {
    /// <summary>The operation succeeded.</summary>
    Ok,

    /// <summary>A referenced resource (team, board, entry, comment, or user) does not exist.</summary>
    NotFound,

    /// <summary>The caller is authenticated but lacks permission for the operation.</summary>
    Forbidden,

    /// <summary>The operation conflicts with current state (e.g. duplicate, or the last-owner guard).</summary>
    Conflict,

    /// <summary>The request was well-formed but semantically invalid (e.g. a rank out of range).</summary>
    Invalid
}

/// <summary>
/// The result of a scouting service operation that returns a value. Use the static factory helpers
/// to construct instances; controllers switch on <see cref="Outcome"/> to shape the HTTP response.
/// </summary>
public sealed record ScoutingResult<T> {
    public required ScoutingOutcome Outcome { get; init; }
    public T? Value { get; init; }
    public string? Error { get; init; }

    public static ScoutingResult<T> Ok(T value) => new() { Outcome = ScoutingOutcome.Ok, Value = value };
    public static ScoutingResult<T> NotFound(string error) => new() { Outcome = ScoutingOutcome.NotFound, Error = error };
    public static ScoutingResult<T> Forbidden(string error) => new() { Outcome = ScoutingOutcome.Forbidden, Error = error };
    public static ScoutingResult<T> Conflict(string error) => new() { Outcome = ScoutingOutcome.Conflict, Error = error };
    public static ScoutingResult<T> Invalid(string error) => new() { Outcome = ScoutingOutcome.Invalid, Error = error };
}

/// <summary>
/// The result of a scouting service operation that returns no value (e.g. delete, reorder). Mirrors
/// <see cref="ScoutingResult{T}"/> without a payload.
/// </summary>
public sealed record ScoutingResult {
    public required ScoutingOutcome Outcome { get; init; }
    public string? Error { get; init; }

    public static ScoutingResult Ok() => new() { Outcome = ScoutingOutcome.Ok };
    public static ScoutingResult NotFound(string error) => new() { Outcome = ScoutingOutcome.NotFound, Error = error };
    public static ScoutingResult Forbidden(string error) => new() { Outcome = ScoutingOutcome.Forbidden, Error = error };
    public static ScoutingResult Conflict(string error) => new() { Outcome = ScoutingOutcome.Conflict, Error = error };
    public static ScoutingResult Invalid(string error) => new() { Outcome = ScoutingOutcome.Invalid, Error = error };
}
