using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shuttle.EFCore.Entities.Portal;
using Shuttle.Shl.Api.Models.Common.Mixins;

namespace Shuttle.EFCore.Entities.Ratings;

public class PortalPlayerAttributes {
    public required int PlayerId { get; set; }
    
    /// <summary>
    /// Navigation property to the player
    /// </summary>
    public PlayerInformation Player { get; set; } = null!;
}

[EntityTypeConfiguration(typeof(PortalSkaterAttributesConfiguration))]
public class PortalSkaterAttributes : PortalPlayerAttributes, ISkaterRatings {
    public int Screening { get; set; }
    public int GettingOpen { get; set; }
    public int Passing { get; set; }
    public int Puckhandling { get; set; }
    public int ShootingAccuracy { get; set; }
    public int ShootingRange { get; set; }
    public int OffensiveRead { get; set; }
    public int Aggression { get; set; }
    public int Bravery { get; set; }
    public int Determination { get; set; }
    public int TeamPlayer { get; set; }
    public int Leadership { get; set; }
    public int Temperament { get; set; }
    public int Professionalism { get; set; }
    public int Checking { get; set; }
    public int Stickchecking { get; set; }
    public int Hitting { get; set; }
    public int Positioning { get; set; }
    public int ShotBlocking { get; set; }
    public int DefensiveRead { get; set; }
    public int Faceoffs { get; set; }
    public int Acceleration { get; set; }
    public int Agility { get; set; }
    public int Balance { get; set; }
    public int Speed { get; set; }
    public int Stamina { get; set; }
    public int Strength { get; set; }
    public int Fighting { get; set; }
}

[EntityTypeConfiguration(typeof(PortalGoaltenderAttributesConfiguration))]
public class PortalGoaltenderAttributes : PortalPlayerAttributes, IGoaltenderRatings {
    public int Aggression { get; set; }
    public int MentalToughness { get; set; }
    public int Determination { get; set; }
    public int TeamPlayer { get; set; }
    public int Leadership { get; set; }
    public int Stamina { get; set; }
    public int Professionalism { get; set; }
    public int Positioning { get; set; }
    public int Passing { get; set; }
    public int PokeCheck { get; set; }
    public int Blocker { get; set; }
    public int Glove { get; set; }
    public int Rebound { get; set; }
    public int Recovery { get; set; }
    public int Puckhandling { get; set; }
    public int LowShots { get; set; }
    public int Skating { get; set; }
    public int Reflexes { get; set; }
}

public static class AttrBaseConfiguration<T> where T : PortalPlayerAttributes {
    public static void Configure(EntityTypeBuilder<T> builder, string tableName) {
        builder.HasKey(p => p.PlayerId);
        builder.HasOne(p => p.Player)
            .WithOne()
            .HasForeignKey<T>(inf => new {
                    inf.PlayerId
                }
            )
            .HasPrincipalKey<PlayerInformation>(p => new { p.PlayerId })
            .OnDelete(DeleteBehavior.Cascade);
        builder.AddTemporalTableSupport(tableName);
    }
}

public class PortalSkaterAttributesConfiguration : IEntityTypeConfiguration<PortalSkaterAttributes> {
    public void Configure(EntityTypeBuilder<PortalSkaterAttributes> builder) {
        AttrBaseConfiguration<PortalSkaterAttributes>.Configure(builder, "PortalSkaterAttributes");
    }
}

public class PortalGoaltenderAttributesConfiguration : IEntityTypeConfiguration<PortalGoaltenderAttributes> {
    public void Configure(EntityTypeBuilder<PortalGoaltenderAttributes> builder) {
        AttrBaseConfiguration<PortalGoaltenderAttributes>.Configure(builder, "PortalGoaltenderAttributes");
    }
}