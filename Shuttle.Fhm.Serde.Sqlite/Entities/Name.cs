using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Shuttle.Fhm.Serde.Sqlite.Entities;

public enum NameListKind
{
    FirstName,
    Surname,
}

public enum NameScalarKind
{
    ScalarA,
    ScalarB,
}

/// <summary>One editable master name from names.dat.</summary>
public sealed class Name
{
    public int NameId { get; set; }
    public int Ordinal { get; set; }
    public string? Text { get; set; }
    public int GroupId { get; set; }
    public int CategoryWeight { get; set; }
    public int FlagA { get; set; }
    public int FlagB { get; set; }
    public int FlagC { get; set; }
}

/// <summary>One nation's ordered first-name or surname reference.</summary>
public sealed class NameListEntry
{
    public int Id { get; set; }
    public NameListKind Kind { get; set; }
    public int NationIndex { get; set; }
    public int Ordinal { get; set; }
    public int? NameId { get; set; }
    public Name? Name { get; set; }
}

/// <summary>One nation-indexed scalar from names.dat.</summary>
public sealed class NameScalar
{
    public int Id { get; set; }
    public NameScalarKind Kind { get; set; }
    public int Ordinal { get; set; }
    public int? Value { get; set; }
}

public sealed class NameConfiguration : IEntityTypeConfiguration<Name>
{
    public void Configure(EntityTypeBuilder<Name> builder)
    {
        builder.ToTable("Names");
        builder.HasKey(value => value.NameId);
        builder.Property(value => value.NameId).ValueGeneratedNever();
        builder.HasIndex(value => value.Ordinal).IsUnique();
    }
}

public sealed class NameListEntryConfiguration : IEntityTypeConfiguration<NameListEntry>
{
    public void Configure(EntityTypeBuilder<NameListEntry> builder)
    {
        builder.ToTable("NameListEntries");
        builder.HasKey(value => value.Id);
        builder.Property(value => value.Kind).HasConversion<string>();
        builder.HasIndex(value => new { value.Kind, value.NationIndex, value.Ordinal }).IsUnique();
        builder.HasOne(value => value.Name)
            .WithMany()
            .HasForeignKey(value => value.NameId)
            .OnDelete(DeleteBehavior.SetNull);
        builder.Navigation(value => value.Name).AutoInclude();
    }
}

public sealed class NameScalarConfiguration : IEntityTypeConfiguration<NameScalar>
{
    public void Configure(EntityTypeBuilder<NameScalar> builder)
    {
        builder.ToTable("NameScalars");
        builder.HasKey(value => value.Id);
        builder.Property(value => value.Kind).HasConversion<string>();
        builder.HasIndex(value => new { value.Kind, value.Ordinal }).IsUnique();
    }
}
