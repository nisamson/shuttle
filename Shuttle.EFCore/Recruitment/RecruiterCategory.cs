namespace Shuttle.EFCore.Recruitment;

/// <summary>
/// Classifies the source named in a player's <c>Recruiter</c> field.
/// </summary>
public enum RecruiterCategory {

    /// <summary>The recruiter matches a known SHL member (an <c>ShlUser.Name</c>).</summary>
    Player,

    /// <summary>An external / generic source that is not an SHL member (for example Google or Reddit).</summary>
    External,

    /// <summary>The player refers to themselves as their recruiter (the literal value <c>"Myself"</c>).</summary>
    Self,

    /// <summary>No recruiter was recorded (blank / whitespace).</summary>
    None,
}
