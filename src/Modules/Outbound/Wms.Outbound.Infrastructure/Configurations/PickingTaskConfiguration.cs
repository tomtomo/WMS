using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wms.BuildingBlocks.Infrastructure.Persistence;
using Wms.Outbound.Domain;

namespace Wms.Outbound.Infrastructure.Configurations;

// Mapping aggregate PickingTask (ambil stock dari satu lokasi rak ke staging, satu per allocation).
public sealed class PickingTaskConfiguration : IEntityTypeConfiguration<PickingTask>
{
    private const int SkuMaxLength = 64;

    private const int BatchMaxLength = 64;

    private const int EnumMaxLength = 24;

    public void Configure(EntityTypeBuilder<PickingTask> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("picking_task");

        builder.HasKey(task => task.Id);
        builder.Property(task => task.Id)
            .HasConversion(id => id.Value, value => PickingTaskId.Create(value).Value)
            .ValueGeneratedNever();

        builder.Property(task => task.WaveId)
            .HasConversion(id => id.Value, value => WaveId.Create(value).Value);
        builder.Property(task => task.ReservationId);
        builder.Property(task => task.StockId);
        builder.Property(task => task.SourceLocationId);
        builder.Property(task => task.Sku).HasMaxLength(SkuMaxLength);
        builder.Property(task => task.Batch).HasMaxLength(BatchMaxLength);
        builder.Property(task => task.Qty).HasPrecision(18, 3);
        builder.Property(task => task.AssignedTo);
        builder.Property(task => task.Status).HasConversion<string>().HasMaxLength(EnumMaxLength);
        builder.Property(task => task.ActualQty).HasPrecision(18, 3);
        builder.Property(task => task.StagingLocationId);

        builder.Property(task => task.CreatedBy).HasMaxLength(200);
        builder.Property(task => task.ModifiedBy).HasMaxLength(200);

        builder.HasIndex(task => new { task.WaveId, task.ReservationId }).IsUnique();

        builder.HasIndex(task => task.AssignedTo);

        builder.UseXminConcurrencyToken();
    }
}
