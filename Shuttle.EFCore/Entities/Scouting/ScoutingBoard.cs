using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shuttle.Models.Scouting;

namespace Shuttle.EFCore.Entities.Scouting;

/// <summary>
/// A named draft board owned by a <see cref="ScoutingTeam"/> (for example, one per draft class or
/// season). Holds a ranked list of <see cref="ScoutingBoardEntry"/> players and a board-level
/// comment thread.
/// </summary>
[EntityTypeConfiguration(typeof(ScoutingBoardEntityConfiguration))]
public class ScoutingBoard {
    /// <summary>The board's primary key.</summary>
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public required Guid Id { get; set; }

    /// <summary>The team that owns this board.</summary>
    public required Guid ScoutingTeamId { get; set; }

    /// <summary>The board's display name.</summary>
    [MaxLength(ScoutingLimits.BoardNameMaxLength)]
    public required string Name { get; set; }

    /// <summary>Optional draft season this board targets, for display/filtering only.</summary>
    public int? DraftSeason { get; set; }

    /// <summary>When the board was created.</summary>
    public required DateTimeOffset CreatedAt { get; set; }

    /// <summary>When the board or its entries were last modified.</summary>
    public required DateTimeOffset UpdatedAt { get; set; }

    /// <summary>
    /// Optimistic-concurrency token. Every entry mutation (add/remove/move) touches the owning board
    /// row, so concurrent reorders or additions collide here and one is rejected, keeping the ranks
    /// contiguous instead of silently corrupting them.
    /// </summary>
    [Timestamp]
    public byte[]? RowVersion { get; set; }

    public ScoutingTeam Team { get; set; } = null!;

    public ICollection<ScoutingBoardEntry> Entries { get; set; } = new List<ScoutingBoardEntry>();

    public ICollection<ScoutingComment> Comments { get; set; } = new List<ScoutingComment>();
}

public class ScoutingBoardEntityConfiguration : IEntityTypeConfiguration<ScoutingBoard> {
    public void Configure(EntityTypeBuilder<ScoutingBoard> builder) {
        builder.HasKey(b => b.Id);
        builder.Property(b => b.Id).ValueGeneratedNever();
        builder.Property(b => b.Name).HasMaxLength(ScoutingLimits.BoardNameMaxLength);
        builder.HasIndex(b => b.ScoutingTeamId);
        builder.HasMany(b => b.Entries)
            .WithOne(e => e.Board)
            .HasForeignKey(e => e.ScoutingBoardId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(b => b.Comments)
            .WithOne(c => c.Board)
            .HasForeignKey(c => c.ScoutingBoardId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
