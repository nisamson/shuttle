using System.Text.Json.Serialization;

namespace Shuttle.Models.Recruitment;

/// <summary>
/// The field a recruiter list can be sorted by (see the <c>GET /recruitment/recruiters</c>
/// endpoint). Serialized/bound by name.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<RecruiterSortField>))]
public enum RecruiterSortField {

    /// <summary>The number of members the recruiter directly recruited (default).</summary>
    Recruits,

    /// <summary>The combined full-career TPE of directly-recruited members.</summary>
    CareerTpe,

    /// <summary>The number of members in the recruiter's full downstream lineage.</summary>
    LineageUsers,

    /// <summary>The combined full-career TPE of the recruiter's full downstream lineage.</summary>
    LineageTpe,
}
