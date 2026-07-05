using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wms.BuildingBlocks.Infrastructure.Persistence;
using Wms.Inventory.Domain;
using Wms.Inventory.Domain.ValueObjects;

namespace Wms.Inventory.Infrastructure.Configurations;

// Mapping aggregate StockReservation (klaim kuantitas terhadap Stock Available untuk satu wave).
public sealed class StockReservationConfiguration : IEntityTypeConfiguration<StockReservation>
{
    private const int SkuMaxLength = 64;

    private const int BatchMaxLength = 64;

    private const int EnumMaxLength = 24;

    private const int ReasonMaxLength = 128;

    public void Configure(EntityTypeBuilder<StockReservation> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("stock_reservation");

        builder.HasKey(reservation => reservation.Id);
        builder.Property(reservation => reservation.Id)
            .HasConversion(id => id.Value, value => StockReservationId.Create(value).Value)
            .ValueGeneratedNever();

        // VO get only wajib di map eksplisit.
        builder.Property(reservation => reservation.StockId)
            .HasConversion(id => id.Value, value => StockId.Create(value).Value);
        builder.Property(reservation => reservation.WaveId);
        builder.Property(reservation => reservation.OrderId);
        builder.Property(reservation => reservation.Sku)
            .HasConversion(sku => sku.Value, value => Sku.Create(value).Value)
            .HasMaxLength(SkuMaxLength);
        builder.Property(reservation => reservation.Batch)
            .HasConversion(batch => batch.Value, value => Batch.Create(value).Value)
            .HasMaxLength(BatchMaxLength);
        builder.Property(reservation => reservation.Qty).HasPrecision(18, 3);
        builder.Property(reservation => reservation.Status).HasConversion<string>().HasMaxLength(EnumMaxLength);
        builder.Property(reservation => reservation.PickingTaskId);
        builder.Property(reservation => reservation.ReleaseReason)
            .HasConversion(reason => reason!.Value, value => ReleaseReason.Create(value).Value)
            .HasMaxLength(ReasonMaxLength);

        builder.Property(reservation => reservation.CreatedBy).HasMaxLength(200);
        builder.Property(reservation => reservation.ModifiedBy).HasMaxLength(200);

        builder.HasIndex(reservation => new { reservation.WaveId, reservation.OrderId, reservation.Sku });

        // Reservasi per wave
        builder.HasIndex(reservation => reservation.WaveId);

        builder.UseXminConcurrencyToken();
    }
}
