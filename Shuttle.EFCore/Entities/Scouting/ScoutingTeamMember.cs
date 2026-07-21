using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shuttle.Models.Scouting;

namespace Shuttle.EFCore.Entities.Scouting;

/// <summary>
/// A <see cref="ShuttleUser"/>'s membership of a <see cref="ScoutingTeam"/>, carrying their
/// <see cref="ScoutingTeamRole"/>. A user has at most one membership row per team.
/// </summary>
[EntityTypeConfiguration(typeof(ScoutingTeamMemberEntityConfiguration))]
public class ScoutingTeamMember {
    /// <summary>The membership's primary key.</summary>
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public required Guid Id { get; set; }

    /// <summary>The team this membership belongs to.</summary>
    public required Guid ScoutingTeamId { get; set; }

    /// <summary>The member <see cref="ShuttleUser"/>.</summary>
    public required Guid ShuttleUserId { get; set; }

    /// <summary>The member's role within the team.</summary>
    public required ScoutingTeamRole Role { get; set; }

    /// <summary>When the user was added to the team.</summary>
    public required DateTimeOffset CreatedAt { get; set; }

    public ScoutingTeam Team { get; set; } = null!;

    public ShuttleUser User { get; set; } = null!;
}

public class ScoutingTeamMemberEntityConfiguration : IEntityTypeConfiguration<ScoutingTeamMember> {
    public void Configure(EntityTypeBuilder<ScoutingTeamMember> builder) {
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).ValueGeneratedNever();
        builder.Property(m => m.Role).HasConversion<string>().HasMaxLength(16);
        builder.HasIndex(m => new { m.ScoutingTeamId, m.ShuttleUserId }).IsUnique();
        builder.HasIndex(m => m.ShuttleUserId);
        builder.HasOne(m => m.User)
            .WithMany()
            .HasForeignKey(m => m.ShuttleUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
