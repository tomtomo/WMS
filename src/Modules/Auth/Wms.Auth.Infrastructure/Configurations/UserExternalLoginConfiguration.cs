using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wms.Auth.Domain;
using Wms.BuildingBlocks.Infrastructure.Persistence;

namespace Wms.Auth.Infrastructure.Configurations;

// Mapping identitas eksternal. Tabel 'user_external_logins'.
public sealed class UserExternalLoginConfiguration : IEntityTypeConfiguration<UserExternalLogin>
{
    public void Configure(EntityTypeBuilder<UserExternalLogin> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("user_external_logins");

        builder.HasKey(login => login.Id);
        builder.Property(login => login.Id)
            .HasConversion(id => id.Value, value => UserExternalLoginId.Create(value).Value)
            .ValueGeneratedNever();

        builder.Property(login => login.UserId)
            .HasConversion(id => id.Value, value => UserId.Create(value).Value);

        builder.Property(login => login.Provider).HasMaxLength(64);
        builder.Property(login => login.Subject).HasMaxLength(256);

        builder.Property(login => login.CreatedBy).HasMaxLength(200);
        builder.Property(login => login.ModifiedBy).HasMaxLength(200);

        // Pastikan satu akun eksternal hanya dapat dihubungkan ke satu user
        builder.HasIndex(login => new { login.Provider, login.Subject }).IsUnique();
        builder.HasIndex(login => login.UserId);

        builder.UseXminConcurrencyToken();
    }
}
