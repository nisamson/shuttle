using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shuttle.Shl.Api.Models.Portal.V1;

namespace Shuttle.EFCore.Entities.Portal;

/// <summary>
/// A single point on a player's TPE timeline, recording the player's cumulative
/// <see cref="TotalTpe"/> at the moment a TPE-earning (or -adjusting) task was applied.
/// Sourced from the portal <c>GET /tpeevents/timeline?pid={pid}</c> endpoint. The player's name is
/// intentionally omitted here since it is already tracked on <see cref="PlayerInformation"/>.
/// </summary>
[EntityTypeConfiguration(typeof(TpeEventConfiguration))]
public class TpeEvent {
    /// <summary>The id of the player this event belongs to.</summary>
    public required int PlayerId { get; set; }

    /// <summary>The UTC timestamp at which the task was applied.</summary>
    public required DateTime TaskDate { get; set; }

    /// <summary>The player's cumulative total TPE immediately after the task.</summary>
    public required int TotalTpe { get; set; }

    /// <summary>The player this event belongs to.</summary>
    public PlayerInformation Player { get; set; } = null!;

    /// <summary>
    /// Builds a <see cref="TpeEvent"/> from a portal timeline entry for the given player id.
    /// The entry's name is discarded (tracked on <see cref="PlayerInformation"/>).
    /// </summary>
    public static TpeEvent FromShlApi(int playerId, TpeTimelineEntry entry) {
        return new() {
            PlayerId = playerId,
            TaskDate = entry.TaskDate,
            TotalTpe = entry.TotalTpe,
        };
    }
}

public class TpeEventConfiguration : IEntityTypeConfiguration<TpeEvent> {
    public void Configure(EntityTypeBuilder<TpeEvent> builder) {
        builder.HasKey(e => new { e.PlayerId, e.TaskDate });
        builder.HasOne(e => e.Player)
            .WithMany(p => p.TpeEvents)
            .HasForeignKey(e => e.PlayerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
