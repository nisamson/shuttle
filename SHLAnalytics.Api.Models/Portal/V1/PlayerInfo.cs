using System.Text.Json.Serialization;
using SHLAnalytics.Api.Models.Common;

namespace SHLAnalytics.Api.Models.Portal.V1;

[JsonSerializable(typeof(PlayerInfo))]
public record PlayerInfo(
    [property: JsonPropertyName("uid")]
    int UserId,
    [property: JsonPropertyName("pid")]
    int PlayerId,
    string Username,
    DateTime CreationDate,
    PlayerStatus Status,
    string Name,
    PlayerPosition Position,
    PlayerHandedness Handedness,
    int TotalTpe,
    int AppliedTpe,
    int BankedTpe,
    int BankBalance,
    PlayerAttributes Attributes,
    TaskStatus? TaskStatus,
    DateTime? RetirementDate = null,
    string? Recruiter = null,
    string? Render = null,
    int? JerseyNumber = null,
    Height? Height = null,
    int? Weight = null,
    string? Birthplace = null,
    KnownLeague? CurrentLeague = null,
    int? CurrentTeamId = null,
    int? ShlRightsTeamId = null,
    int? SmjhlRightsTeamId = null,
    string? IihfNation = null,
    [property: JsonConverter(typeof(IntBoolJsonConverter))]
    bool PositionChanged = false,
    int? DraftSeason = null,
    int UsedRedistribution = 0,
    int CoachingPurchased = 0,
    int TrainingPurchased = 0,
    bool ActivityCheckComplete = false,
    bool TrainingCampComplete = false,
    [property: JsonConverter(typeof(IntBoolJsonConverter))]
    bool IsSuspended = false,
    bool Inactive = false,
    IList<IndexRecord>? IndexRecords = null
);
