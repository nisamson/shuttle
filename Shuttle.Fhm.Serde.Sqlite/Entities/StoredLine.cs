using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Shuttle.Fhm.Serde.Sqlite.Entities;

/// <summary>One stored-line header.</summary>
public sealed class StoredLine
{
    /// <summary>Gets or sets the serialized line ordinal.</summary>
    public int LineOrdinal { get; set; }
    /// <summary>Gets or sets the line name.</summary>
    public string? Name { get; set; }
    /// <summary>Gets or sets the inferred owning team when every populated slot belongs to that team.</summary>
    public int? TeamRecordOrdinal { get; set; }
    /// <summary>Gets or sets the inferred owning team.</summary>
    public Team? Team { get; set; }
    /// <summary>Gets the player slots in this stored line.</summary>
    public ICollection<StoredLineSlot> Slots { get; } = [];
}

/// <summary>One stored-line player slot and matching lock value.</summary>
public sealed class StoredLineSlot
{
    /// <summary>Gets or sets the line ordinal.</summary>
    public int LineOrdinal { get; set; }
    /// <summary>Gets or sets the fixed group ordinal.</summary>
    public int GroupOrdinal { get; set; }
    /// <summary>Gets or sets the slot ordinal within the group.</summary>
    public int SlotOrdinal { get; set; }
    /// <summary>Gets or sets the player internal identity.</summary>
    public int? PlayerInternalId { get; set; }
    /// <summary>Gets or sets the raw lock flag.</summary>
    public int? UnitLock { get; set; }
    /// <summary>Gets or sets the containing stored line.</summary>
    public StoredLine StoredLine { get; set; } = null!;
    /// <summary>Gets or sets the referenced player.</summary>
    public Player? Player { get; set; }
}

public sealed class StoredLineConfiguration : IEntityTypeConfiguration<StoredLine>
{
    public void Configure(EntityTypeBuilder<StoredLine> builder)
    {
        builder.ToTable("StoredLines");
        builder.HasKey(value => value.LineOrdinal);
        builder.Property(value => value.LineOrdinal).ValueGeneratedNever();
        builder.HasOne(value => value.Team)
            .WithMany(value => value.StoredLines)
            .HasForeignKey(value => value.TeamRecordOrdinal)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class StoredLineSlotConfiguration : IEntityTypeConfiguration<StoredLineSlot>
{
    public void Configure(EntityTypeBuilder<StoredLineSlot> builder)
    {
        builder.ToTable("StoredLineSlots");
        builder.HasKey(value => new { value.LineOrdinal, value.GroupOrdinal, value.SlotOrdinal });
        builder.HasOne(value => value.StoredLine)
            .WithMany(value => value.Slots)
            .HasForeignKey(value => value.LineOrdinal)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(value => value.Player)
            .WithMany()
            .HasForeignKey(value => value.PlayerInternalId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(value => value.Player).AutoInclude();
    }
}
