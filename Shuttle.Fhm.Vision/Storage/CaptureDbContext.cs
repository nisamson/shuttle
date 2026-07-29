using Microsoft.EntityFrameworkCore;

namespace Shuttle.Fhm.Vision.Storage;

/// <summary>EF Core context over the local SQLite capture database.</summary>
public sealed class CaptureDbContext : DbContext {
    public CaptureDbContext(DbContextOptions<CaptureDbContext> options) : base(options) {
    }

    public DbSet<CaptureRecordEntity> Captures => Set<CaptureRecordEntity>();
    public DbSet<AttributeValueEntity> Attributes => Set<AttributeValueEntity>();
    public DbSet<RoleValueEntity> RoleRatings => Set<RoleValueEntity>();
    public DbSet<NumericValueEntity> Numbers => Set<NumericValueEntity>();
    public DbSet<TextValueEntity> TextFields => Set<TextValueEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.Entity<CaptureRecordEntity>(entity => {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ContentHash).IsUnique();
            entity.Property(e => e.ContentHash).IsRequired();
            entity.Property(e => e.Name).IsRequired();

            entity.HasMany(e => e.Attributes)
                .WithOne()
                .HasForeignKey(a => a.CaptureRecordId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.RoleRatings)
                .WithOne()
                .HasForeignKey(r => r.CaptureRecordId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.Numbers)
                .WithOne()
                .HasForeignKey(n => n.CaptureRecordId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.TextFields)
                .WithOne()
                .HasForeignKey(t => t.CaptureRecordId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AttributeValueEntity>().HasIndex(a => new { a.CaptureRecordId, a.Key });
        modelBuilder.Entity<RoleValueEntity>().HasIndex(r => new { r.CaptureRecordId, r.Key });
        modelBuilder.Entity<NumericValueEntity>().HasIndex(n => new { n.CaptureRecordId, n.Key });
        modelBuilder.Entity<TextValueEntity>().HasIndex(t => new { t.CaptureRecordId, t.Key });
    }
}
