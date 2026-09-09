using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Shuttle.Fhm.Serde.Sqlite.Entities;

/// <summary>Classifies baseline source content.</summary>
public enum SaveFileKind
{
    /// <summary>A supported file represented by an <c>IFhmSaveFile</c>.</summary>
    Documented,
    /// <summary>An unsupported opaque file retained exactly.</summary>
    Opaque,
}

/// <summary>Exact source bytes for one normalized FHM save-folder file.</summary>
public sealed class SaveFile
{
    /// <summary>Gets or sets the normalized relative path.</summary>
    public string RelativePath { get; set; } = string.Empty;
    /// <summary>Gets or sets the source-file classification.</summary>
    public SaveFileKind Kind { get; set; }
    /// <summary>Gets or sets exact source bytes.</summary>
    public byte[] Content { get; set; } = [];
}

/// <summary>One ordered chunk of baseline content that exceeds SQLite's single-BLOB limit.</summary>
public sealed class SaveFileChunk
{
    /// <summary>Gets or sets the normalized relative path of the source file.</summary>
    public string RelativePath { get; set; } = string.Empty;
    /// <summary>Gets or sets the zero-based chunk ordinal.</summary>
    public int Ordinal { get; set; }
    /// <summary>Gets or sets the exact chunk bytes.</summary>
    public byte[] Content { get; set; } = [];
}

public sealed class SaveFileConfiguration : IEntityTypeConfiguration<SaveFile>
{
    public void Configure(EntityTypeBuilder<SaveFile> builder)
    {
        builder.ToTable("SaveFiles");
        builder.HasKey(value => value.RelativePath);
        builder.Property(value => value.RelativePath).UseCollation("NOCASE");
        builder.Property(value => value.Content).IsRequired();
        builder.HasIndex(value => value.RelativePath).IsUnique();
    }
}

public sealed class SaveFileChunkConfiguration : IEntityTypeConfiguration<SaveFileChunk>
{
    public void Configure(EntityTypeBuilder<SaveFileChunk> builder)
    {
        builder.ToTable("SaveFileChunks");
        builder.HasKey(value => new { value.RelativePath, value.Ordinal });
        builder.Property(value => value.RelativePath).UseCollation("NOCASE");
        builder.Property(value => value.Content).IsRequired();
        builder.HasOne<SaveFile>()
            .WithMany()
            .HasForeignKey(value => value.RelativePath)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
