using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text;
using System.Text.Unicode;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Shuttle.EFCore.SiteArchive;

public class ArchiveEntry {

    public const int MaxContentLength = 1024 * 1024;
    
    public required Uri Url { get; set; }
    
    [MaxLength(MaxContentLength)]
    public string? Content { get; set; }
    
    public byte[]? ContentHash { get; set; }

    public static ArchiveEntry From(Uri url, string? content) {
        if (content is null) {
            return new() {
                Url = url
            };
        }
        
        ArgumentOutOfRangeException.ThrowIfGreaterThan(content.Length, MaxContentLength);
        var strStream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        var hash = SHA256.HashData(strStream);
        
        return new() {
            Url = url,
            Content = content,
            ContentHash = hash
        };
    }

    public void UpdateContent(string? content) {
        if (ReferenceEquals(content, Content)) {
            return;
        }

        if (content is null) {
            Content = null;
            ContentHash = null;
            return;
        }
        
        ArgumentOutOfRangeException.ThrowIfGreaterThan(content.Length, MaxContentLength);
        var strStream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        var hash = SHA256.HashData(strStream);
        
        Content = content;
        ContentHash = hash;
    }

    public override string ToString() {
        return $"ArchiveEntry {{ Url = {Url}, Content = {(Content is not null ? Content.Length : "null")}, ContentHash = {(ContentHash is not null ? Convert.ToHexString(ContentHash) : "null")} }}";
    }
}

public class ArchiveEntryEntityConfiguration : IEntityTypeConfiguration<ArchiveEntry> {
    public void Configure(EntityTypeBuilder<ArchiveEntry> builder) {
        builder.HasKey(e => e.Url);
        builder.Property(e => e.Content);
        builder.Property(e => e.ContentHash);
        
        builder.AddTemporalTableSupport(config: tb => {
            tb.HasCheckConstraint("CK_ArchiveEntry_ContentHash_Content",
                "([ContentHash] IS NOT NULL AND [Content] IS NOT NULL) OR ([ContentHash] IS NULL AND [Content] IS NULL)");
        });
    }
}
