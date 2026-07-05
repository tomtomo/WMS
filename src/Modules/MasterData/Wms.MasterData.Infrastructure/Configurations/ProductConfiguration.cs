using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wms.BuildingBlocks.Infrastructure.Persistence;
using Wms.MasterData.Domain;

namespace Wms.MasterData.Infrastructure.Configurations;

// Mapping Product
public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    private const int SkuMaxLength = 64;

    public void Configure(EntityTypeBuilder<Product> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("product");

        builder.HasKey(product => product.Id);
        builder.Property(product => product.Id)
            .HasConversion(sku => sku.Value, value => Sku.Create(value).Value)
            .HasMaxLength(SkuMaxLength)
            .ValueGeneratedNever();

        builder.Ignore(product => product.Sku);

        builder.Property(product => product.Name).HasMaxLength(200);
        builder.Property(product => product.Uom).HasMaxLength(32);
        builder.Property(product => product.BatchTrackingRequired);
        builder.Property(product => product.ExpiryTrackingRequired);
        builder.Property(product => product.QcRequiredOnReceipt);
        builder.Property(product => product.ShelfLifeDays);
        builder.Property(product => product.IsActive);

        builder.Property(product => product.CreatedBy).HasMaxLength(200);
        builder.Property(product => product.ModifiedBy).HasMaxLength(200);

        builder.HasQueryFilter(product => product.IsActive);

        builder.UseXminConcurrencyToken();
    }
}
