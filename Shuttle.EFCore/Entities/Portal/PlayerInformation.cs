using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shuttle.Shl.Api.Models.Common;
using Shuttle.Shl.Api.Models.Portal.V1;
using TaskStatus = Shuttle.Shl.Api.Models.Portal.V1.TaskStatus;

namespace Shuttle.EFCore.Entities.Portal;

[EntityTypeConfiguration(typeof(PlayerInfoEntityConfiguration))]
public class PlayerInformation {
    public required int UserId { get; set; }
    
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public required int PlayerId { get; set; }

    [MaxLength(80)] public required string Username { get; set; }

    public required DateTime CreationTime { get; set; }

    public required PlayerStatus Status { get; set; }

    [MaxLength(80)] public required string Name { get; set; }

    public required PlayerPosition Position { get; set; }

    public required PlayerHandedness Handedness { get; set; }

    public required int TotalTpe { get; set; }

    public required int AppliedTpe { get; set; }

    public required int BankedTpe { get; set; }

    public required int BankBalance { get; set; }

    public TaskStatus? TaskStatus { get; set; }

    public DateTime? RetirementDate { get; set; }

    public int? JerseyNumber { get; set; }

    public Height? Height { get; set; }

    public int? Weight { get; set; }

    [MaxLength(80)] public string? Birthplace { get; set; }

    [MaxLength(80)] public string? Recruiter { get; set; }

    [MaxLength(80)] public string? Render { get; set; }

    public KnownLeague? CurrentLeague { get; set; }

    public int? CurrentTeamId { get; set; }

    public int? ShlRightsTeamId { get; set; }

    public int? SmjhlRightsTeamId { get; set; }

    [MaxLength(32)] public string? IihfNation { get; set; }

    public bool PositionChanged { get; set; }

    public int? DraftSeason { get; set; }

    public int UsedRedistribution { get; set; }

    public int CoachingPurchased { get; set; }

    public int TrainingPurchased { get; set; }

    public bool ActivityCheckComplete { get; set; }

    public bool TrainingCampComplete { get; set; }

    public bool IsSuspended { get; set; }

    public bool Inactive { get; set; }

    // should not be part of update check
    public IList<IndexRecord> IndexRecords { get; set; } = null!;

    public SkaterAttributes? SkaterAttributes { get; set; }
    public GoaltenderAttributes? GoaltenderAttributes { get; set; }

    // not part of update check
    public ShlUser User { get; set; } = null!;

    public static PlayerInformation FromShlApi(PlayerInfo original) {
        return new() {
            UserId = original.UserId,
            PlayerId = original.PlayerId,
            Username = original.Username,
            CreationTime = original.CreationDate,
            Status = original.Status,
            Name = original.Name,
            Position = original.Position,
            Handedness = original.Handedness,
            TotalTpe = original.TotalTpe,
            AppliedTpe = original.AppliedTpe,
            BankedTpe = original.BankedTpe,
            BankBalance = original.BankBalance,
            TaskStatus = original.TaskStatus,
            RetirementDate = original.RetirementDate,
            JerseyNumber = original.JerseyNumber,
            Height = original.Height,
            Weight = original.Weight,
            Birthplace = original.Birthplace,
            Recruiter = original.Recruiter,
            Render = original.Render,
            CurrentLeague = original.CurrentLeague,
            CurrentTeamId = original.CurrentTeamId,
            ShlRightsTeamId = original.ShlRightsTeamId,
            SmjhlRightsTeamId = original.SmjhlRightsTeamId,
            IihfNation = original.IihfNation,
            PositionChanged = original.PositionChanged,
            DraftSeason = original.DraftSeason,
            UsedRedistribution = original.UsedRedistribution,
            CoachingPurchased = original.CoachingPurchased,
            TrainingPurchased = original.TrainingPurchased,
            ActivityCheckComplete = original.ActivityCheckComplete,
            TrainingCampComplete = original.TrainingCampComplete,
            IsSuspended = original.IsSuspended,
            Inactive = original.Inactive,
            SkaterAttributes = original.Attributes as SkaterAttributes,
            GoaltenderAttributes = original.Attributes as GoaltenderAttributes,
        };
    }

    public bool UpdateFrom(PlayerInformation playerInfo) {
        if (UserId != playerInfo.UserId || PlayerId != playerInfo.PlayerId) {
            throw new InvalidOperationException("Cannot update from a different player");
        }

        var hasChanges = false;

        if (Username != playerInfo.Username) {
            Username = playerInfo.Username;
            hasChanges = true;
        }

        if (CreationTime != playerInfo.CreationTime) {
            CreationTime = playerInfo.CreationTime;
            hasChanges = true;
        }

        if (Status != playerInfo.Status) {
            Status = playerInfo.Status;
            hasChanges = true;
        }

        if (Name != playerInfo.Name) {
            Name = playerInfo.Name;
            hasChanges = true;
        }

        if (Position != playerInfo.Position) {
            Position = playerInfo.Position;
            hasChanges = true;
        }

        if (Handedness != playerInfo.Handedness) {
            Handedness = playerInfo.Handedness;
            hasChanges = true;
        }

        if (TotalTpe != playerInfo.TotalTpe) {
            TotalTpe = playerInfo.TotalTpe;
            hasChanges = true;
        }

        if (AppliedTpe != playerInfo.AppliedTpe) {
            AppliedTpe = playerInfo.AppliedTpe;
            hasChanges = true;
        }

        if (BankedTpe != playerInfo.BankedTpe) {
            BankedTpe = playerInfo.BankedTpe;
            hasChanges = true;
        }

        if (BankBalance != playerInfo.BankBalance) {
            BankBalance = playerInfo.BankBalance;
            hasChanges = true;
        }

        if (TaskStatus != playerInfo.TaskStatus) {
            TaskStatus = playerInfo.TaskStatus;
            hasChanges = true;
        }

        if (RetirementDate != playerInfo.RetirementDate) {
            RetirementDate = playerInfo.RetirementDate;
            hasChanges = true;
        }

        if (JerseyNumber != playerInfo.JerseyNumber) {
            JerseyNumber = playerInfo.JerseyNumber;
            hasChanges = true;
        }

        if (Height != playerInfo.Height) {
            Height = playerInfo.Height;
            hasChanges = true;
        }

        if (Weight != playerInfo.Weight) {
            Weight = playerInfo.Weight;
            hasChanges = true;
        }

        if (Birthplace != playerInfo.Birthplace) {
            Birthplace = playerInfo.Birthplace;
            hasChanges = true;
        }

        if (Recruiter != playerInfo.Recruiter) {
            Recruiter = playerInfo.Recruiter;
            hasChanges = true;
        }

        if (Render != playerInfo.Render) {
            Render = playerInfo.Render;
            hasChanges = true;
        }

        if (CurrentLeague != playerInfo.CurrentLeague) {
            CurrentLeague = playerInfo.CurrentLeague;
            hasChanges = true;
        }

        if (CurrentTeamId != playerInfo.CurrentTeamId) {
            CurrentTeamId = playerInfo.CurrentTeamId;
            hasChanges = true;
        }

        if (ShlRightsTeamId != playerInfo.ShlRightsTeamId) {
            ShlRightsTeamId = playerInfo.ShlRightsTeamId;
            hasChanges = true;
        }

        if (SmjhlRightsTeamId != playerInfo.SmjhlRightsTeamId) {
            SmjhlRightsTeamId = playerInfo.SmjhlRightsTeamId;
            hasChanges = true;
        }

        if (IihfNation != playerInfo.IihfNation) {
            IihfNation = playerInfo.IihfNation;
            hasChanges = true;
        }

        if (PositionChanged != playerInfo.PositionChanged) {
            PositionChanged = playerInfo.PositionChanged;
            hasChanges = true;
        }

        if (DraftSeason != playerInfo.DraftSeason) {
            DraftSeason = playerInfo.DraftSeason;
            hasChanges = true;
        }

        if (UsedRedistribution != playerInfo.UsedRedistribution) {
            UsedRedistribution = playerInfo.UsedRedistribution;
            hasChanges = true;
        }

        if (CoachingPurchased != playerInfo.CoachingPurchased) {
            CoachingPurchased = playerInfo.CoachingPurchased;
            hasChanges = true;
        }

        if (TrainingPurchased != playerInfo.TrainingPurchased) {
            TrainingPurchased = playerInfo.TrainingPurchased;
            hasChanges = true;
        }

        if (ActivityCheckComplete != playerInfo.ActivityCheckComplete) {
            ActivityCheckComplete = playerInfo.ActivityCheckComplete;
            hasChanges = true;
        }

        if (TrainingCampComplete != playerInfo.TrainingCampComplete) {
            TrainingCampComplete = playerInfo.TrainingCampComplete;
            hasChanges = true;
        }

        if (IsSuspended != playerInfo.IsSuspended) {
            IsSuspended = playerInfo.IsSuspended;
            hasChanges = true;
        }

        if (Inactive != playerInfo.Inactive) {
            Inactive = playerInfo.Inactive;
            hasChanges = true;
        }

        if (SkaterAttributes != playerInfo.SkaterAttributes) {
            SkaterAttributes = playerInfo.SkaterAttributes;
            hasChanges = true;
        }

        if (GoaltenderAttributes != playerInfo.GoaltenderAttributes) {
            GoaltenderAttributes = playerInfo.GoaltenderAttributes;
            hasChanges = true;
        }

        return hasChanges;
    }
}

public class PlayerInfoEntityConfiguration : IEntityTypeConfiguration<PlayerInformation> {

    public void Configure(EntityTypeBuilder<PlayerInformation> builder) {
        builder.HasKey(p => p.PlayerId);
        builder.HasAlternateKey(p => new { p.UserId, p.PlayerId });
        builder.HasIndex(p => new { p.UserId, p.CreationTime, p.PlayerId });
        builder.HasIndex(p => p.Name);
        builder.HasIndex(p => p.Username);
        builder.HasIndex(p => p.DraftSeason);
        builder.HasIndex(p => p.CurrentTeamId);
        builder.HasOne(p => p.User)
            .WithMany(u => u.Players)
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();
        builder.ComplexProperty(
            p => p.Height,
            h => { h.ToJson(); }
        );
        builder.ComplexProperty(p => p.SkaterAttributes);
        builder.ComplexProperty(p => p.GoaltenderAttributes);
        builder.Navigation(p => p.User).AutoInclude();
        builder.AddTemporalTableSupport();
    }
}
