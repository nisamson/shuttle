using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Shuttle.EFCore.Entities;

/// <summary>
/// A first-class account for an authenticated Shuttle user, distinct from the upstream
/// <see cref="ShlUser"/> forum/portal member. Created lazily the first time a user is seen on an
/// authenticated request and keyed to their Entra object id.
/// </summary>
[EntityTypeConfiguration(typeof(ShuttleUserEntityConfiguration))]
public class ShuttleUser {
    /// <summary>The generated primary key that identifies this account.</summary>
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public required Guid Id { get; set; }

    /// <summary>
    /// The caller's Entra object id (the <c>oid</c> claim). Unique per account; used to resolve the
    /// account from the authenticated principal.
    /// </summary>
    public required Guid ObjectId { get; set; }

    /// <summary>
    /// The user-facing username. Initialised from <see cref="Id"/> and freely changeable by the user
    /// within the allowed character set and length. The column allows up to 64 characters for future
    /// flexibility, even though input validation currently restricts it to a shorter range.
    /// </summary>
    [MaxLength(64)]
    public required string Username { get; set; }
}

public class ShuttleUserEntityConfiguration : IEntityTypeConfiguration<ShuttleUser> {
    public void Configure(EntityTypeBuilder<ShuttleUser> builder) {
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).ValueGeneratedNever();
        builder.HasIndex(u => u.ObjectId).IsUnique();
        builder.HasIndex(u => u.Username).IsUnique();
    }
}
