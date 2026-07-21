using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shuttle.Models.Scouting;

namespace Shuttle.EFCore.Entities.Scouting;

/// <summary>
/// A single player's placement on a <see cref="ScoutingBoard"/>. Players are referenced by their
/// upstream integer <c>PlayerId</c>. <see cref="Rank"/> is a contiguous 1..n ordering within the
/// board's active (non-rejected) prospects; reordering shifts the affected ranks so the sequence
/// stays gap-free. Rejected prospects are unranked (<see cref="Rank"/> is <c>0</c>) and excluded
/// from that sequence.
/// </summary>
[EntityTypeConfiguration(typeof(ScoutingBoardEntryEntityConfiguration))]
public class ScoutingBoardEntry {
    /// <summary>The entry's primary key.</summary>
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public required Guid Id { get; set; }

    /// <summary>The board this entry belongs to.</summary>
    public required Guid ScoutingBoardId { get; set; }

    /// <summary>The upstream integer id of the ranked player.</summary>
    public required int PlayerId { get; set; }

    /// <summary>
    /// The player's 1-based rank among the board's active prospects, or <c>0</c> when the prospect is
    /// <see cref="ScoutingProspectStatus.Rejected"/>. App-managed to stay contiguous within the board.
    /// </summary>
    public required int Rank { get; set; }

    /// <summary>The prospect's scouting lifecycle status. Defaults to <see cref="ScoutingProspectStatus.Pending"/>.</summary>
    public ScoutingProspectStatus Status { get; set; } = ScoutingProspectStatus.Pending;

    /// <summary>The <see cref="ShuttleUser"/> assigned to scout this prospect, or <c>null</c> if unassigned.</summary>
    public Guid? AssignedToUserId { get; set; }

    /// <summary>When the current assignee was assigned, or <c>null</c> if unassigned.</summary>
    public DateTimeOffset? AssignedAt { get; set; }

    /// <summary>When the player was added to the board.</summary>
    public required DateTimeOffset CreatedAt { get; set; }

    /// <summary>When the entry was last modified (rank, status, or assignment change).</summary>
    public required DateTimeOffset UpdatedAt { get; set; }

    public ScoutingBoard Board { get; set; } = null!;

    public ShuttleUser? AssignedTo { get; set; }

    public ICollection<ScoutingComment> Comments { get; set; } = new List<ScoutingComment>();
}

public class ScoutingBoardEntryEntityConfiguration : IEntityTypeConfiguration<ScoutingBoardEntry> {
    public void Configure(EntityTypeBuilder<ScoutingBoardEntry> builder) {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(16);
        builder.HasIndex(e => new { e.ScoutingBoardId, e.PlayerId }).IsUnique();
        builder.HasIndex(e => new { e.ScoutingBoardId, e.Rank });
        builder.HasIndex(e => new { e.ScoutingBoardId, e.Status });
        builder.HasOne(e => e.AssignedTo)
            .WithMany()
            .HasForeignKey(e => e.AssignedToUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(e => e.Comments)
            .WithOne(c => c.Entry)
            .HasForeignKey(c => c.ScoutingBoardEntryId)
            .OnDelete(DeleteBehavior.ClientCascade);
    }
}
