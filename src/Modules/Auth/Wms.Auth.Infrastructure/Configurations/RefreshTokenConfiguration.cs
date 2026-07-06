using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wms.Auth.Domain;
using Wms.BuildingBlocks.Infrastructure.Persistence;

namespace Wms.Auth.Infrastructure.Configurations;

// Konfigurasi mapping untuk aggregate RefreshToken.
public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("refresh_tokens");

        builder.HasKey(token => token.Id);
        builder.Property(token => token.Id)
            .HasConversion(id => id.Value, value => RefreshTokenId.Create(value).Value)
            .ValueGeneratedNever();

        builder.Property(token => token.UserId)
            .HasConversion(id => id.Value, value => UserId.Create(value).Value);

        builder.Property(token => token.TokenHash).HasMaxLength(128);
        builder.Property(token => token.IssuedAt);
        builder.Property(token => token.ExpiresAt);
        builder.Property(token => token.RevokedAt);

        builder.Property(token => token.ReplacedByTokenId)
            .HasConversion(id => id!.Value, value => RefreshTokenId.Create(value).Value);

        builder.Property(token => token.CreatedBy).HasMaxLength(200);
        builder.Property(token => token.ModifiedBy).HasMaxLength(200);

        builder.HasIndex(token => token.TokenHash).IsUnique();
        builder.HasIndex(token => token.UserId);

        builder.UseXminConcurrencyToken();
    }
}
