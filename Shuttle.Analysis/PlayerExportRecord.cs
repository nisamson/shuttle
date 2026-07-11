using Shuttle.EFCore.Entities.Portal;
using Shuttle.Shl.Api.Models.Common;
using Shuttle.Shl.Api.Models.Portal.V1;
using TaskStatus = Shuttle.Shl.Api.Models.Portal.V1.TaskStatus;

namespace Shuttle.Analysis;

/// <summary>
/// A flattened, analysis-friendly projection of a <see cref="PlayerInformation"/> row.
/// Height is expressed as integer inches, the skater/goaltender attribute sets are kept as
/// nested objects, and navigation data (owning user, index records) is deliberately excluded.
/// Mental attributes that are constant (15) for every player are dropped during serialization
/// (see <see cref="PlayerExportJson"/>).
/// </summary>
public sealed record PlayerExportRecord {
    public required int UserId { get; init; }
    public required int PlayerId { get; init; }
    public required string Username { get; init; }
    public required DateTime CreationTime { get; init; }
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
    public int? JerseyNumber { get; init; }

    /// <summary>Player height in total inches, or <c>null</c> when unknown.</summary>
    public int? HeightInches { get; init; }

    public int? Weight { get; init; }
    public string? Birthplace { get; init; }
    public string? Recruiter { get; init; }
    public string? Render { get; init; }
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

    public SkaterAttributes? SkaterAttributes { get; init; }
    public GoaltenderAttributes? GoaltenderAttributes { get; init; }

    public static PlayerExportRecord FromEntity(PlayerInformation player) {
        ArgumentNullException.ThrowIfNull(player);
        return new PlayerExportRecord {
            UserId = player.UserId,
            PlayerId = player.PlayerId,
            Username = player.Username,
            CreationTime = player.CreationTime,
            Status = player.Status,
            Name = player.Name,
            Position = player.Position,
            Handedness = player.Handedness,
            TotalTpe = player.TotalTpe,
            AppliedTpe = player.AppliedTpe,
            BankedTpe = player.BankedTpe,
            BankBalance = player.BankBalance,
            TaskStatus = player.TaskStatus,
            RetirementDate = player.RetirementDate,
            JerseyNumber = player.JerseyNumber,
            HeightInches = player.Height?.TotalInches,
            Weight = player.Weight,
            Birthplace = player.Birthplace,
            Recruiter = player.Recruiter,
            Render = player.Render,
            CurrentLeague = player.CurrentLeague,
            CurrentTeamId = player.CurrentTeamId,
            ShlRightsTeamId = player.ShlRightsTeamId,
            SmjhlRightsTeamId = player.SmjhlRightsTeamId,
            IihfNation = player.IihfNation,
            PositionChanged = player.PositionChanged,
            DraftSeason = player.DraftSeason,
            UsedRedistribution = player.UsedRedistribution,
            CoachingPurchased = player.CoachingPurchased,
            TrainingPurchased = player.TrainingPurchased,
            ActivityCheckComplete = player.ActivityCheckComplete,
            TrainingCampComplete = player.TrainingCampComplete,
            IsSuspended = player.IsSuspended,
            Inactive = player.Inactive,
            SkaterAttributes = player.SkaterAttributes,
            GoaltenderAttributes = player.GoaltenderAttributes,
        };
    }
}
