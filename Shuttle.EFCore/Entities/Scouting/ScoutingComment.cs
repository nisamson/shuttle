using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shuttle.Models.Scouting;

namespace Shuttle.EFCore.Entities.Scouting;

/// <summary>
/// A comment in a scouting discussion thread. Always scoped to a <see cref="ScoutingBoard"/>; when
/// <see cref="ScoutingBoardEntryId"/> is <c>null</c> the comment belongs to the board-level thread,
/// otherwise it belongs to that specific player entry's thread.
/// </summary>
[EntityTypeConfiguration(typeof(ScoutingCommentEntityConfiguration))]
public class ScoutingComment {
    /// <summary>The comment's primary key.</summary>
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public required Guid Id { get; set; }

    /// <summary>The board whose thread this comment belongs to.</summary>
    public required Guid ScoutingBoardId { get; set; }

    /// <summary>
    /// The board entry this comment is attached to, or <c>null</c> for the board-level thread.
    /// </summary>
    public Guid? ScoutingBoardEntryId { get; set; }

    /// <summary>The <see cref="ShuttleUser"/> who authored the comment.</summary>
    public required Guid AuthorUserId { get; set; }

    /// <summary>The comment text.</summary>
    [MaxLength(ScoutingLimits.CommentBodyMaxLength)]
    public required string Body { get; set; }

    /// <summary>When the comment was posted.</summary>
    public required DateTimeOffset CreatedAt { get; set; }

    /// <summary>When the comment was last edited, or <c>null</c> if never edited.</summary>
    public DateTimeOffset? EditedAt { get; set; }

    public ScoutingBoard Board { get; set; } = null!;

    public ScoutingBoardEntry? Entry { get; set; }

    public ShuttleUser Author { get; set; } = null!;
}

public class ScoutingCommentEntityConfiguration : IEntityTypeConfiguration<ScoutingComment> {
    public void Configure(EntityTypeBuilder<ScoutingComment> builder) {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();
        builder.Property(c => c.Body).HasMaxLength(ScoutingLimits.CommentBodyMaxLength);
        builder.HasIndex(c => new { c.ScoutingBoardId, c.ScoutingBoardEntryId, c.CreatedAt });
        builder.HasOne(c => c.Author)
            .WithMany()
            .HasForeignKey(c => c.AuthorUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
