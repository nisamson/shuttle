using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SHLAnalytics.Api.Models.Index.V1;

namespace SHLAnalytics.EFCore.Entities;

public class PlayerRef : IEntityConvertible<PlayerRef, Api.Models.Index.V1.PlayerRef> {
    public required int Id { get; set; }
    [MaxLength(256)]
    public required string Name { get; set; }

    public static PlayerRef From(Api.Models.Index.V1.PlayerRef original) {
        return new() {
            Id = original.Id,
            Name = original.Name
        };
    }
    public Api.Models.Index.V1.PlayerRef To() {
        return new(Id, Name);
    }
}

public class PlayerEntityConfiguration : IEntityTypeConfiguration<PlayerRef> {
    public void Configure(EntityTypeBuilder<PlayerRef> builder) {
        builder.HasKey(p => p.Id);
        builder.HasIndex(p => p.Name);
    }
}
