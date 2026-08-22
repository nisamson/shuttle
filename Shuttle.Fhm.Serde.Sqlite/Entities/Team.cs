using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shuttle.Fhm.Serde.Domain.Files;

namespace Shuttle.Fhm.Serde.Sqlite.Entities;

/// <summary>One editable teams.dat top-level record.</summary>
public sealed class Team
{
    /// <summary>Gets personnel currently employed by this team.</summary>
    public ICollection<Personnel> Staff { get; } = [];
    /// <summary>Gets the team's general manager or combined GM/head coach.</summary>
    public Personnel? GeneralManager => Staff.SingleOrDefault(
        value => value.Job is FhmPersonnelJob.GeneralManager or FhmPersonnelJob.GmHeadCoach);
    /// <summary>Gets the team's head coach or combined GM/head coach.</summary>
    public Personnel? HeadCoach => Staff.SingleOrDefault(
        value => value.Job is FhmPersonnelJob.HeadCoach or FhmPersonnelJob.GmHeadCoach);
    /// <summary>Gets players currently assigned to this team.</summary>
    public ICollection<Player> Players { get; } = [];
    /// <summary>Gets stored lines inferred to belong to this team.</summary>
    public ICollection<StoredLine> StoredLines { get; } = [];
    /// <summary>Gets the player slots in this team's active game lineup.</summary>
    public ICollection<TeamActiveLineSlot> ActiveLineSlots { get; } = [];
    /// <summary>Gets or sets the serialized ordinal.</summary>
    public int RecordOrdinal { get; set; }
    /// <summary>Gets or sets the record's persisted ordinal field.</summary>
    public int RecordIndex { get; set; }
    /// <summary>Gets or sets the stable team identity.</summary>
    public int TeamId { get; set; }
    public string? InternalCode { get; set; }
    public string? InternalCode2 { get; set; }
    public int Flag1 { get; set; }
    public string? City { get; set; }
    public string? Nickname { get; set; }
    public int NicknamePlacement { get; set; }
    public int? AffiliateParentId { get; set; }
    public int? AffiliateParentId2 { get; set; }
    public Team? AffiliateParent { get; set; }
    public Team? SecondaryAffiliateParent { get; set; }
    public int LeagueId { get; set; }
    public int ConferenceId { get; set; }
    public int DivisionId { get; set; }
    public int LocationId { get; set; }
    public int MarketSize { get; set; }
    public int FanLoyalty { get; set; }
    public int Finance1 { get; set; }
    public int Finance2 { get; set; }
    public int Finance3 { get; set; }
    public int Finance4 { get; set; }
    /// <summary>Gets or sets the team's tactical settings.</summary>
    public TeamTactic Tactics { get; set; } = null!;
}

/// <summary>One teams.dat embedded tactical-settings payload.</summary>
public sealed class TeamTactic
{
    /// <summary>Gets or sets the owning team record ordinal.</summary>
    public int TeamId { get; set; }
    /// <summary>Gets or sets the complete 1,303-byte editable tactical-settings payload.</summary>
    public byte[] SerializedSettings { get; set; } = [];
    /// <summary>Gets or sets the owning team.</summary>
    public Team Team { get; set; } = null!;
}

public sealed class TeamConfiguration : IEntityTypeConfiguration<Team>
{
    public void Configure(EntityTypeBuilder<Team> builder)
    {
        builder.ToTable("Teams");
        builder.HasKey(value => value.TeamId);
        builder.HasIndex(value => value.RecordOrdinal).IsUnique();
        builder.Property(value => value.TeamId).ValueGeneratedNever();
        builder.Property(value => value.RecordOrdinal).ValueGeneratedNever();
        builder.HasOne(value => value.Tactics)
            .WithOne(value => value.Team)
            .HasForeignKey<TeamTactic>(value => value.TeamId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(value => value.AffiliateParent)
            .WithMany()
            .HasForeignKey(value => value.AffiliateParentId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(value => value.SecondaryAffiliateParent)
            .WithMany()
            .HasForeignKey(value => value.AffiliateParentId2)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(value => value.Tactics).AutoInclude();
        builder.Navigation(value => value.Staff).AutoInclude();
    }
}

public sealed class TeamTacticConfiguration : IEntityTypeConfiguration<TeamTactic>
{
    public void Configure(EntityTypeBuilder<TeamTactic> builder)
    {
        builder.ToTable("TeamTactics");
        builder.HasKey(value => value.TeamId);
        builder.Property(value => value.TeamId).ValueGeneratedNever();
        builder.Property(value => value.SerializedSettings).IsRequired();
    }
}
