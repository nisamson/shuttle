using System.Text.Json.Serialization;

namespace Shuttle.Models.Recruitment;

/// <summary>
/// Classifies the source named in a player's recruiter field. Serialized by name (string) per the
/// repository convention.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<RecruiterCategory>))]
public enum RecruiterCategory {

    /// <summary>The recruiter matches a known SHL member.</summary>
    Player,

    /// <summary>An external / generic source that is not an SHL member (for example Google or Reddit).</summary>
    External,

    /// <summary>The player refers to themselves as their recruiter (the literal value "Myself").</summary>
    Self,

    /// <summary>No recruiter was recorded (blank / whitespace).</summary>
    None,
}
