using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SHLAnalytics.Api.Models.Index.V1;

namespace SHLAnalytics.EFCore.Entities;

public class Player : IEntityConvertible<Player, PlayerRef> {
    public required int Id { get; set; }
    [MaxLength(256)]
    public required string Name { get; set; }

    public static Player From(PlayerRef original) {
        return new() {
            Id = original.Id,
            Name = original.Name
        };
    }
    public PlayerRef To() {
        return new(Id, Name);
    }
}

public class PlayerEntityConfiguration : IEntityTypeConfiguration<Player> {
    public void Configure(EntityTypeBuilder<Player> builder) {
        builder.HasKey(p => p.Id);
        builder.HasIndex(p => p.Name);
    }
}
