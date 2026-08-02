using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shuttle.EFCore.Entities.Portal;

namespace Shuttle.EFCore.Entities;

[EntityTypeConfiguration(typeof(PortalUserEntityConfiguration))]
public class ShlUser {
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public required int UserId { get; set; }
    
    [MaxLength(80)]
    public required string Name { get; set; }
    
    [MaxLength(32)]
    public string? DiscordId { get; set; }

    public ICollection<PlayerInformation> Players { get; } = null!;

    public PlayerInformation? MostRecentPlayer => Players.MaxBy(pl => pl.CreationTime);
}

public class PortalUserEntityConfiguration : IEntityTypeConfiguration<ShlUser> {

    public void Configure(EntityTypeBuilder<ShlUser> builder) {
        builder.HasKey(p => p.UserId);
        builder.HasIndex(p => p.Name);
    }
}