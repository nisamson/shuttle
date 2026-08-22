using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Shuttle.Fhm.Serde.Sqlite.Entities;

/// <summary>One set-play formation or trailing record.</summary>
public sealed class SetPlay
{
    /// <summary>Gets or sets the normalized set-play source file path.</summary>
    public string RelativePath { get; set; } = string.Empty;
    /// <summary>Gets or sets whether this is a trailing extra record.</summary>
    public bool IsExtraRecord { get; set; }
    /// <summary>Gets or sets the ordinal within formations or extra records.</summary>
    public int RecordOrdinal { get; set; }
    /// <summary>Gets or sets the opaque payload after its wire length.</summary>
    public byte[] Data { get; set; } = [];
}

public sealed class SetPlayConfiguration : IEntityTypeConfiguration<SetPlay>
{
    public void Configure(EntityTypeBuilder<SetPlay> builder)
    {
        builder.ToTable("SetPlays");
        builder.HasKey(value => new { value.RelativePath, value.IsExtraRecord, value.RecordOrdinal });
        builder.Property(value => value.RelativePath).UseCollation("NOCASE");
        builder.Property(value => value.Data).IsRequired();
    }
}
