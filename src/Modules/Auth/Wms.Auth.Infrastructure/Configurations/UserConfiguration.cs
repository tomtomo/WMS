using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wms.Auth.Domain;
using Wms.BuildingBlocks.Infrastructure.Persistence;

namespace Wms.Auth.Infrastructure.Configurations;

// Mapping aggregate User. Tabel 'users' (jamak) — 'user' reserved di Postgres.
public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("users");

        builder.HasKey(user => user.Id);
        builder.Property(user => user.Id)
            .HasConversion(id => id.Value, value => UserId.Create(value).Value)
            .ValueGeneratedNever();

        builder.Property(user => user.Username).HasMaxLength(100);
        builder.Property(user => user.Email).HasMaxLength(256);
        builder.Property(user => user.PasswordHash).HasMaxLength(512);
        builder.Property(user => user.Status).HasConversion<string>().HasMaxLength(24);
        builder.Property(user => user.FailedLoginCount);
        builder.Property(user => user.LockedAt);
        builder.Property(user => user.IsActive);

        // Ref lintas-aggregate sebagai koleksi Guid → jsonb (converter eksplisit; read-only collection).
        builder.Property(user => user.RoleIds)
            .HasConversion(GuidCollectionMapping.Converter, GuidCollectionMapping.Comparer)
            .HasColumnType("jsonb");
        builder.Property(user => user.AssignedWarehouseIds)
            .HasConversion(GuidCollectionMapping.Converter, GuidCollectionMapping.Comparer)
            .HasColumnType("jsonb");

        builder.Property(user => user.CreatedBy).HasMaxLength(200);
        builder.Property(user => user.ModifiedBy).HasMaxLength(200);

        // Login by username → unik (termasuk yang Disabled agar reject-nya distinct).
        builder.HasIndex(user => user.Username).IsUnique();

        // Soft-delete (Disabled → isActive=false) tersembunyi dari query default.
        builder.HasQueryFilter(user => user.IsActive);

        builder.UseXminConcurrencyToken();
    }
}
