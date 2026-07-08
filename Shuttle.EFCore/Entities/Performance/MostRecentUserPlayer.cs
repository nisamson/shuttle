using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shuttle.EFCore.Entities.Portal;

namespace Shuttle.EFCore.Entities.Performance;

[EntityTypeConfiguration(typeof(MostRecentUserPlayerEntityConfiguration))]
public class MostRecentUserPlayer {
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public required int UserId { get; set; }
    public required int PlayerId { get; set; }
    
    public PlayerInformation Player { get; } = null!;
    public ShlUser User => Player.User;
}

public class MostRecentUserPlayerEntityConfiguration : IEntityTypeConfiguration<MostRecentUserPlayer> {

    public void Configure(EntityTypeBuilder<MostRecentUserPlayer> builder) {
        builder.HasKey(x => new {x.UserId});
        builder.HasOne<PlayerInformation>(x => x.Player)
            .WithOne()
            .HasForeignKey<MostRecentUserPlayer>(m => m.PlayerId)
            .HasPrincipalKey<PlayerInformation>(p => p.PlayerId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.Player).AutoInclude();
    }
}
