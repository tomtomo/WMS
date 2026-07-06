using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wms.Auth.Domain;
using Wms.BuildingBlocks.Infrastructure.Persistence;

namespace Wms.Auth.Infrastructure.Configurations;

// Konfigurasi mapping untuk aggregate Role.
public sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("roles");

        builder.HasKey(role => role.Id);
        builder.Property(role => role.Id)
            .HasConversion(id => id.Value, value => RoleId.Create(value).Value)
            .ValueGeneratedNever();

        builder.Property(role => role.Code).HasMaxLength(50);
        builder.Property(role => role.Name).HasMaxLength(100);
        builder.Property(role => role.IsActive);
        builder.Property(role => role.PermissionIds)
            .HasConversion(GuidCollectionMapping.Converter, GuidCollectionMapping.Comparer)
            .HasColumnType("jsonb");

        builder.Property(role => role.CreatedBy).HasMaxLength(200);
        builder.Property(role => role.ModifiedBy).HasMaxLength(200);

        builder.HasIndex(role => role.Code).IsUnique();

        builder.HasQueryFilter(role => role.IsActive);

        builder.UseXminConcurrencyToken();
    }
}
