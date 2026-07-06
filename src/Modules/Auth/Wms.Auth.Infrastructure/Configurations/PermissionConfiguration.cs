using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wms.Auth.Domain;
using Wms.Auth.Domain.ValueObjects;

namespace Wms.Auth.Infrastructure.Configurations;

// Konfigurasi mapping untuk entity Permission.
public sealed class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("permissions");

        builder.HasKey(permission => permission.Id);
        builder.Property(permission => permission.Id)
            .HasConversion(id => id.Value, value => PermissionId.Create(value).Value)
            .ValueGeneratedNever();

        builder.Property(permission => permission.Code)
            .HasConversion(code => code.Value, value => PermissionCode.Create(value).Value)
            .HasMaxLength(100);
        builder.Property(permission => permission.Description).HasMaxLength(256);

        builder.HasIndex(permission => permission.Code).IsUnique();
    }
}
