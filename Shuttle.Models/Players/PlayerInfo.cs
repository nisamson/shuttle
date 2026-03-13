using System.Text.Json.Serialization;
using SHLAnalytics.Api.Models;
using SHLAnalytics.Api.Models.Common;
using SHLAnalytics.Api.Models.Portal.V1;
using Shuttle.Models.Meta;
using TaskStatus = System.Threading.Tasks.TaskStatus;

namespace Shuttle.Models.Players;

public record PlayerInfo {
    public required int UserId { get; init; }
    public required int PlayerId { get; init; }
    public required string Username { get; init; }
    public required DateTime CreationDate { get; init; }
    public required PlayerStatus Status { get; init; }
    public required string Name { get; init; }
    public required PlayerPosition Position { get; init; }
    public required PlayerHandedness Handedness { get; init; }
    public required int TotalTpe { get; init; }
    public required int AppliedTpe { get; init; }
    public required int BankedTpe { get; init; }
    public required int BankBalance { get; init; }
    public TaskStatus? TaskStatus { get; init; }
    public DateTime? RetirementDate { get; init; }
    public string? Recruiter { get; init; }
    public string? Render { get; init; }
    public int? JerseyNumber { get; init; }
    public Height? Height { get; init; }
    public int? Weight { get; init; }
    public string? Birthplace { get; init; }
    public KnownLeague? CurrentLeague { get; init; }
    public int? CurrentTeamId { get; init; }
    public int? ShlRightsTeamId { get; init; }
    public int? SmjhlRightsTeamId { get; init; }
    public string? IihfNation { get; init; }
    public bool PositionChanged { get; init; }
    public int? DraftSeason { get; init; }
    public int UsedRedistribution { get; init; }
    public int CoachingPurchased { get; init; }
    public int TrainingPurchased { get; init; }
    public bool ActivityCheckComplete { get; init; }
    public bool TrainingCampComplete { get; init; }
    public bool IsSuspended { get; init; }
    public bool Inactive { get; init; }
}

public record PlayerInfoFull : PlayerInfo {
    public required IList<IndexRecord>? IndexRecords { get; init; }
    public required PlayerAttributes Attributes { get; init; }
}

public record PlayerAttributeHistory {
    public required int PlayerId { get; init; }
    public required int UserId { get; init; }
    public required IList<Temporal<PlayerAttributes>> AttributeHistory { get; init; }
}