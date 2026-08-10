using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shuttle.Fhm.Entities.Names;
using Shuttle.Shl.Api.Models.Common;
using Shuttle.Shl.Api.Models.Index.V1;

namespace Shuttle.Fhm.Entities.Players;

public sealed class FhmPlayer {
    public required int PlayerId { get; init; }

    public FhmName? First { get; set; }
    public FhmName? Last { get; set; }
    public FhmName? Common { get; set; }

    [NotNullIfNotNull(nameof(First))] public string? FirstName => First?.Name;
    [NotNullIfNotNull(nameof(Last))] public string? LastName => Last?.Name;
    [NotNullIfNotNull(nameof(Common))] public string? CommonName => Common?.Name;

    public DateOnly BirthDate { get; set; }
    public required FhmPositionAffinity PositionAffinity { get; set; }

    public PlayerPosition Position => PositionAffinity.PrimaryPosition;
    
    public FhmPlayerOpaqueData OpaqueData { get; set; } = null!;
    public FhmPlayerAttributes Attributes { get; set; } = null!;
}

public sealed class FhmPositionAffinity {
    public PlayerPosition PrimaryPosition => PlayerPosition.SkaterPositions().MaxBy(PositionAffinity);

    public int PositionAffinity(PlayerPosition position) {
        switch (position) {
            case PlayerPosition.Goalie:
                return Goalie;
            case PlayerPosition.Center:
                return Center;
            case PlayerPosition.LeftWing:
                return LeftWing;
            case PlayerPosition.RightWing:
                return RightWing;
            case PlayerPosition.LeftDefense:
                return LeftDefense;
            case PlayerPosition.RightDefense:
                return RightDefense;
            default:
                throw new ArgumentOutOfRangeException(
                    nameof(position),
                    position,
                    "Must specify a single unambiguous position."
                );
        }
    }

    [Range(0, 20)]
    public int Goalie {
        get;
        set  => field = Math.Clamp(value, 0, 20);
    }

    [Range(0, 20)]
    public int LeftWing {
        get;
        set => field = Math.Clamp(value, 0, 20);
    }

    [Range(0, 20)]
    public int RightWing {
        get;
        set => field = Math.Clamp(value, 0, 20);
    }

    [Range(0, 20)]
    public int Center {
        get;
        set => field = Math.Clamp(value, 0, 20);
    }

    [Range(0, 20)]
    public int RightDefense {
        get;
        set => field = Math.Clamp(value, 0, 20);
    }

    [Range(0, 20)]
    public int LeftDefense {
        get;
        set => field = Math.Clamp(value, 0, 20);
    }

}


public sealed class FhmPlayerOpaqueData {
    public int PlayerId { get; set; }

    // TODO: Implement me!
}

public class FhmPlayerEntityConfiguration : IEntityTypeConfiguration<FhmPlayer> {

    public void Configure(EntityTypeBuilder<FhmPlayer> builder) {
        builder.Property(p => p.PlayerId)
            .IsRequired()
            .ValueGeneratedNever();
        builder.HasKey(p => p.PlayerId);
        
        builder.HasOne(p => p.First)
            .WithMany()
            .OnDelete(DeleteBehavior.SetNull);
        builder.Navigation(p => p.First)
            .AutoInclude();
        
        builder.HasOne(p => p.Last)
            .WithMany()
            .OnDelete(DeleteBehavior.SetNull);
        builder.Navigation(p => p.Last)
            .AutoInclude();
        
        builder.HasOne(p => p.Common)
            .WithMany()
            .OnDelete(DeleteBehavior.SetNull);
        builder.Navigation(p => p.Common)
            .AutoInclude();
        
        builder.HasOne(p => p.OpaqueData)
            .WithOne()
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasOne(p => p.Attributes)
            .WithOne()
            .OnDelete(DeleteBehavior.Cascade);
    }
}
