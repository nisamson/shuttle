using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Shuttle.EFCore.Entities.Scouting;

/// <summary>
/// A single player's placement on a <see cref="ScoutingBoard"/>. Players are referenced by their
/// upstream integer <c>PlayerId</c>. <see cref="Rank"/> is a contiguous 1..n ordering within the
/// board; reordering shifts the affected ranks so the sequence stays gap-free.
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

    /// <summary>The player's 1-based rank on the board. App-managed to stay contiguous within the board.</summary>
    public required int Rank { get; set; }

    /// <summary>When the player was added to the board.</summary>
    public required DateTimeOffset CreatedAt { get; set; }

    /// <summary>When the entry was last modified (rank change).</summary>
    public required DateTimeOffset UpdatedAt { get; set; }

    public ScoutingBoard Board { get; set; } = null!;

    public ICollection<ScoutingComment> Comments { get; set; } = new List<ScoutingComment>();
}

public class ScoutingBoardEntryEntityConfiguration : IEntityTypeConfiguration<ScoutingBoardEntry> {
    public void Configure(EntityTypeBuilder<ScoutingBoardEntry> builder) {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.HasIndex(e => new { e.ScoutingBoardId, e.PlayerId }).IsUnique();
        builder.HasIndex(e => new { e.ScoutingBoardId, e.Rank });
        builder.HasMany(e => e.Comments)
            .WithOne(c => c.Entry)
            .HasForeignKey(c => c.ScoutingBoardEntryId)
            .OnDelete(DeleteBehavior.ClientCascade);
    }
}
