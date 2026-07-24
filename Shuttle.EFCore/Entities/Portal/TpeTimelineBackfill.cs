using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Shuttle.EFCore.Entities.Portal;

/// <summary>
/// Marker recording that a player's full TPE timeline has been backfilled (or at least
/// attempted) by the rolling backfill in <c>PortalUpdater</c>. The presence of a row is
/// intentionally decoupled from whether the player actually had any <see cref="TpeEvent"/>s:
/// players with empty timelines still get a marker so the bounded backfill batch never
/// re-fetches them and never starves later players.
/// </summary>
/// <remarks>
/// Deliberately has no foreign key to <see cref="PlayerInformation"/>: it is a standalone
/// progress ledger keyed by portal player id, and keeping it decoupled avoids involving the
/// system-versioned <see cref="PlayerInformation"/> table.
/// </remarks>
[EntityTypeConfiguration(typeof(TpeTimelineBackfillConfiguration))]
public class TpeTimelineBackfill {
    /// <summary>The portal id of the player whose timeline has been backfilled.</summary>
    public required int PlayerId { get; set; }

    /// <summary>The UTC time at which the backfill for this player was completed.</summary>
    public required DateTime BackfilledAt { get; set; }
}

public class TpeTimelineBackfillConfiguration : IEntityTypeConfiguration<TpeTimelineBackfill> {
    public void Configure(EntityTypeBuilder<TpeTimelineBackfill> builder) {
        builder.HasKey(e => e.PlayerId);
        builder.Property(e => e.PlayerId)
            .ValueGeneratedNever();
    }
}
