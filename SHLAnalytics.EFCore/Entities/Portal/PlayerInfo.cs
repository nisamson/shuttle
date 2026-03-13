using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SHLAnalytics.Api.Models.Common;
using SHLAnalytics.Api.Models.Portal.V1;
using TaskStatus = SHLAnalytics.Api.Models.Portal.V1.TaskStatus;

namespace SHLAnalytics.EFCore.Entities.Portal;

[EntityTypeConfiguration(typeof(PlayerInfoEntityConfiguration))]
public class PlayerInfo {
    public required int UserId { get; set; }
    public required int PlayerId { get; set; }
    
    [MaxLength(80)]
    public required string Username { get; set; }
    
    public required DateTime CreationTime { get; set; }
    
    public required PlayerStatus Status { get; set; }
    
    [MaxLength(80)]
    public required string Name { get; set; }
    
    public required PlayerPosition Position { get; set; }
    
    public required PlayerHandedness Handedness { get; set; }
    
    public required int TotalTpe { get; set; }
    
    public required int AppliedTpe { get; set; }
    
    public required int BankedTpe { get; set; }

    public required int BankBalance { get; set; }
    
    public TaskStatus? TaskStatus { get; set; }
    
    public DateTime? RetirementData { get; set; }
    
    public int? JerseyNumber { get; set; }
    
    public Height? Height { get; set; }
    
    public int? Weight { get; set; }
    
    [MaxLength(80)]
    public string? Birthplace { get; set; }
    
    [MaxLength(80)]
    public string? Recruiter { get; set; }
    
    [MaxLength(80)]
    public string? Render { get; set; }
    
    public KnownLeague? KnownLeague { get; set; }
    
    public int? CurrentTeamId { get; set; }
    
    public int? ShlRightsTeamId { get; set; }
    
    public int? SmjhlRightsTeamId { get; set; }
    
    [MaxLength(32)]
    public string? IihfNation { get; set; }
    
    public bool PositionChanged { get; set; }
    
    public int? DraftSeason { get; set; }
    
    public int UsedRedistribution { get; set; }
    
    public int CoachingPurchased { get; set; }
    
    public int TrainingPurchased { get; set; }
    
    public bool ActivityCheckComplete { get; set; }
    
    public bool TrainingCampComplete { get; set; }
    
    public bool IsSuspended { get; set; }
    
    public bool Inactive { get; set; }

    public static readonly Expression<Func<PlayerInfo, PlayerInfo, bool>> ShouldUpdateExpression =
        (src, tgt) => src.PlayerId != tgt.PlayerId
            || src.Username != tgt.Username
            || src.CreationTime != tgt.CreationTime
            || src.Status != tgt.Status
            || src.Name != tgt.Name
            || src.Position != tgt.Position
            || src.Handedness != tgt.Handedness
            || src.TotalTpe != tgt.TotalTpe
            || src.AppliedTpe != tgt.AppliedTpe
            || src.BankedTpe != tgt.BankedTpe
            || src.BankBalance != tgt.BankBalance
            || src.TaskStatus != tgt.TaskStatus
            || src.RetirementData != tgt.RetirementData
            || src.JerseyNumber != tgt.JerseyNumber
            || src.Height != tgt.Height
            || src.Weight != tgt.Weight
            || src.Birthplace != tgt.Birthplace
            || src.Recruiter != tgt.Recruiter
            || src.Render != tgt.Render
            || src.KnownLeague != tgt.KnownLeague
            || src.CurrentTeamId != tgt.CurrentTeamId
            || src.ShlRightsTeamId != tgt.ShlRightsTeamId
            || src.SmjhlRightsTeamId != tgt.SmjhlRightsTeamId
            || src.IihfNation != tgt.IihfNation
            || src.PositionChanged != tgt.PositionChanged
            || src.DraftSeason != tgt.DraftSeason
            || src.UsedRedistribution != tgt.UsedRedistribution
            || src.CoachingPurchased != tgt.CoachingPurchased
            || src.TrainingPurchased != tgt.TrainingPurchased
            || src.ActivityCheckComplete != tgt.ActivityCheckComplete
            || src.TrainingCampComplete != tgt.TrainingCampComplete
            || src.IsSuspended != tgt.IsSuspended
            || src.Inactive != tgt.Inactive;
    
    public static readonly Expression<Func<PlayerInfo, PlayerInfo, PlayerInfo>> UpdateExpression =
        (src, tgt) => new PlayerInfo {
            UserId = src.UserId,
            PlayerId = tgt.PlayerId,
            Username = tgt.Username,
            CreationTime = tgt.CreationTime,
            Status = tgt.Status,
            Name = tgt.Name,
            Position = tgt.Position,
            Handedness = tgt.Handedness,
            TotalTpe = tgt.TotalTpe,
            AppliedTpe = tgt.AppliedTpe,
            BankedTpe = tgt.BankedTpe,
            BankBalance = tgt.BankBalance,
            TaskStatus = tgt.TaskStatus,
            RetirementData = tgt.RetirementData,
            JerseyNumber = tgt.JerseyNumber,
            Height = tgt.Height,
            Weight = tgt.Weight,
            Birthplace = tgt.Birthplace,
            Recruiter = tgt.Recruiter,
            Render = tgt.Render,
            KnownLeague = tgt.KnownLeague,
            CurrentTeamId = tgt.CurrentTeamId,
            ShlRightsTeamId = tgt.ShlRightsTeamId,
            SmjhlRightsTeamId = tgt.SmjhlRightsTeamId,
            IihfNation = tgt.IihfNation,
            PositionChanged = tgt.PositionChanged,
            DraftSeason = tgt.DraftSeason,
            UsedRedistribution = tgt.UsedRedistribution,
            CoachingPurchased = tgt.CoachingPurchased,
            TrainingPurchased = tgt.TrainingPurchased,
            ActivityCheckComplete = tgt.ActivityCheckComplete,
            TrainingCampComplete = tgt.TrainingCampComplete,
            IsSuspended = tgt.IsSuspended,
            Inactive = tgt.Inactive 
        };
    
    public static PlayerInfo From(Api.Models.Portal.V1.PlayerInfo original) {
        throw new NotImplementedException();
    }
    public Api.Models.Portal.V1.PlayerInfo To() {
        throw new NotImplementedException();
    }
}

public class PlayerInfoEntityConfiguration : IEntityTypeConfiguration<PlayerInfo> {

    public void Configure(EntityTypeBuilder<PlayerInfo> builder) {
        builder.HasKey(p => p.PlayerId);
        builder.HasIndex(p => p.UserId);
        builder.HasIndex(p => p.Name);
        builder.HasIndex(p => p.Username);
        builder.HasIndex(p => p.DraftSeason);
        builder.HasIndex(p => p.CurrentTeamId);
        builder.OwnsOne(p => p.Height);
        builder.AddTemporalTableSupport();
    }
}
