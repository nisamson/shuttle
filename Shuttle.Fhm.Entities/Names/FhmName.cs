using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Shuttle.Fhm.Entities.Names;

public sealed record FhmName(
    int NameId,
    string Name,
    int GroupId,
    ushort CategoryWeight,
    bool FlagA,
    bool FlagB,
    bool FlagC);

public sealed class FhmNameEntityConfiguration : IEntityTypeConfiguration<FhmName>
{
    public void Configure(EntityTypeBuilder<FhmName> builder)
    {
        builder.HasKey(name => name.NameId);
        builder.Property(name => name.NameId).ValueGeneratedNever();
        builder.Property(name => name.Name).IsRequired();
    }
}
