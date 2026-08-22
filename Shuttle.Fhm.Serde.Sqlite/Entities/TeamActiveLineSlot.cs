using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shuttle.Fhm.Serde.Domain.Model;

namespace Shuttle.Fhm.Serde.Sqlite.Entities;

/// <summary>One player slot in a team's active game lineup.</summary>
public sealed class TeamActiveLineSlot
{
    public int TeamId { get; set; }
    public FhmLineGroup Group { get; set; }
    public int SlotOrdinal { get; set; }
    public int? PlayerInternalId { get; set; }
    public Team Team { get; set; } = null!;
    public Player? Player { get; set; }
}

public sealed class TeamActiveLineSlotConfiguration : IEntityTypeConfiguration<TeamActiveLineSlot>
{
    public void Configure(EntityTypeBuilder<TeamActiveLineSlot> builder)
    {
        builder.ToTable(
            "TeamActiveLineSlots",
            table =>
            {
                table.HasCheckConstraint(
                    "CK_TeamActiveLineSlots_Group",
                    "\"Group\" >= 0 AND \"Group\" <= 12");
                table.HasCheckConstraint(
                    "CK_TeamActiveLineSlots_SlotOrdinal",
                    "SlotOrdinal >= 0");
            });
        builder.HasKey(value => new { value.TeamId, value.Group, value.SlotOrdinal });
        builder.Property(value => value.Group).HasConversion<int>();
        builder.HasOne(value => value.Team)
            .WithMany(value => value.ActiveLineSlots)
            .HasForeignKey(value => value.TeamId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(value => value.Player)
            .WithMany()
            .HasForeignKey(value => value.PlayerInternalId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
