using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Shuttle.EFCore.Entities;

public class PlayerRef : IEntityConvertible<PlayerRef, Shl.Api.Models.Index.V1.PlayerRef> {
    public required int Id { get; set; }
    [MaxLength(256)]
    public required string Name { get; set; }

    public static PlayerRef From(Shl.Api.Models.Index.V1.PlayerRef original) {
        return new() {
            Id = original.Id,
            Name = original.Name
        };
    }
    public Shl.Api.Models.Index.V1.PlayerRef To() {
        return new(Id, Name);
    }
}

public class PlayerEntityConfiguration : IEntityTypeConfiguration<PlayerRef> {
    public void Configure(EntityTypeBuilder<PlayerRef> builder) {
        builder.HasKey(p => p.Id);
        builder.HasIndex(p => p.Name);
    }
}
