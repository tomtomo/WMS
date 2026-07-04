using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wms.BuildingBlocks.Infrastructure.Persistence;
using Wms.Inventory.Domain;
using Wms.Inventory.Domain.ValueObjects;

namespace Wms.Inventory.Infrastructure.Configurations;

// Mapping aggregate Stock (balance fisik).
public sealed class StockConfiguration : IEntityTypeConfiguration<Stock>
{
    private const int SkuMaxLength = 64;

    private const int BatchMaxLength = 64;

    private const int EnumMaxLength = 24;

    public void Configure(EntityTypeBuilder<Stock> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("stock");

        builder.HasKey(stock => stock.Id);
        builder.Property(stock => stock.Id)
            .HasConversion(id => id.Value, value => StockId.Create(value).Value)
            .ValueGeneratedNever();

        // VO get only wajib di map eksplisit
        builder.Property(stock => stock.Sku)
            .HasConversion(sku => sku.Value, value => Sku.Create(value).Value)
            .HasMaxLength(SkuMaxLength);
        builder.Property(stock => stock.LocationId)
            .HasConversion(location => location.Value, value => LocationId.Create(value).Value);
        builder.Property(stock => stock.Batch)
            .HasConversion(batch => batch.Value, value => Batch.Create(value).Value)
            .HasMaxLength(BatchMaxLength);
        builder.Property(stock => stock.Expiry)
            .HasConversion(expiry => expiry.Value, value => Expiry.Create(value).Value);
        builder.Property(stock => stock.Qty).HasPrecision(18, 3);
        builder.Property(stock => stock.Status).HasConversion<string>().HasMaxLength(EnumMaxLength);

        builder.Property(stock => stock.SourceGrId);
        builder.Property(stock => stock.Line);
        builder.Property(stock => stock.WarehouseId);

        builder.Property(stock => stock.CreatedBy).HasMaxLength(200);
        builder.Property(stock => stock.ModifiedBy).HasMaxLength(200);

        // AvailableQty computed dari klaim reservasi
        builder.Ignore(stock => stock.AvailableQty);

        // Natural key idempotent consumer. Hanya balance receiving. Pick mensplit balance turunan yang
        // sengaja mewarisi (sourceGrId, line) parent, jadi Picked dikecualikan dari keunikan.
        builder.HasIndex(stock => new { stock.SourceGrId, stock.Line })
            .IsUnique()
            .HasFilter("status <> 'Picked'");

        // Antrean availability read per warehouse.
        builder.HasIndex(stock => new { stock.WarehouseId, stock.Status });

        builder.UseXminConcurrencyToken();

        // Owned collection
    }
}
