using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wms.BuildingBlocks.Infrastructure.Persistence;
using Wms.Outbound.Domain;
using Wms.Outbound.Domain.ValueObjects;
using Wms.Outbound.Infrastructure.Persistence;

namespace Wms.Outbound.Infrastructure.Configurations;

// Mapping aggregate Wave (grouping order yang diproses dan didispatch bersama, terikat satu warehouse).
public sealed class WaveConfiguration : IEntityTypeConfiguration<Wave>
{
    private const int ReasonMaxLength = 256;

    private const int EnumMaxLength = 24;

    public void Configure(EntityTypeBuilder<Wave> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("wave");

        builder.HasKey(wave => wave.Id);
        builder.Property(wave => wave.Id)
            .HasConversion(id => id.Value, value => WaveId.Create(value).Value)
            .ValueGeneratedNever();

        builder.Property(wave => wave.WarehouseId);
        builder.Property(wave => wave.Status).HasConversion<string>().HasMaxLength(EnumMaxLength);
        builder.Property(wave => wave.CancelReason)
            .HasConversion(reason => reason!.Value, value => CancelReason.Create(value).Value)
            .HasMaxLength(ReasonMaxLength);

        builder.Property(wave => wave.CreatedBy).HasMaxLength(200);
        builder.Property(wave => wave.ModifiedBy).HasMaxLength(200);

        // Antrean wave per warehouse dan status (Active/Ready).
        builder.HasIndex(wave => new { wave.WarehouseId, wave.Status });

        builder.UseXminConcurrencyToken();

        // Colection id (typed id/Guid)
        builder.Ignore(wave => wave.OrderIds);
        builder.Property<List<OutboundOrderId>>("_orderIds")
            .HasConversion(
                GuidBackedCollection.Converter<OutboundOrderId>(id => id.Value, value => OutboundOrderId.Create(value).Value),
                GuidBackedCollection.Comparer<OutboundOrderId>())
            .HasColumnName("order_ids")
            .HasColumnType("jsonb");

        builder.Ignore(wave => wave.PickingTaskIds);
        builder.Property<List<PickingTaskId>>("_pickingTaskIds")
            .HasConversion(
                GuidBackedCollection.Converter<PickingTaskId>(id => id.Value, value => PickingTaskId.Create(value).Value),
                GuidBackedCollection.Comparer<PickingTaskId>())
            .HasColumnName("picking_task_ids")
            .HasColumnType("jsonb");

        // Referensi reservasi milik Inventory (bukan salinan detail alokasi).
        builder.Ignore(wave => wave.ReservationIds);
        builder.Property<List<Guid>>("_reservationIds")
            .HasConversion(
                GuidBackedCollection.Converter<Guid>(id => id, value => value),
                GuidBackedCollection.Comparer<Guid>())
            .HasColumnName("reservation_ids")
            .HasColumnType("jsonb");
    }
}
