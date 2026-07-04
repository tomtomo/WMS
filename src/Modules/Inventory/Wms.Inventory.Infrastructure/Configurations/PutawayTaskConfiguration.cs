using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wms.BuildingBlocks.Infrastructure.Persistence;
using Wms.Inventory.Domain;
using Wms.Inventory.Domain.ValueObjects;

namespace Wms.Inventory.Infrastructure.Configurations;

// Mapping aggregate PutawayTask
public sealed class PutawayTaskConfiguration : IEntityTypeConfiguration<PutawayTask>
{
    private const int EnumMaxLength = 24;

    public void Configure(EntityTypeBuilder<PutawayTask> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("putaway_task");

        builder.HasKey(task => task.Id);
        builder.Property(task => task.Id)
            .HasConversion(id => id.Value, value => PutawayTaskId.Create(value).Value)
            .ValueGeneratedNever();

        // VO get only wajib di map eksplisit.
        builder.Property(task => task.StockId)
            .HasConversion(id => id.Value, value => StockId.Create(value).Value);
        builder.Property(task => task.SourceLocationId)
            .HasConversion(location => location.Value, value => LocationId.Create(value).Value);
        builder.Property(task => task.SuggestedDestinationId)
            .HasConversion(location => location.Value, value => LocationId.Create(value).Value);
        builder.Property(task => task.AssignedTo);
        builder.Property(task => task.Status).HasConversion<string>().HasMaxLength(EnumMaxLength);
        builder.Property(task => task.ActualDestinationId)
            .HasConversion(location => location!.Value, value => LocationId.Create(value).Value);

        builder.Property(task => task.CreatedBy).HasMaxLength(200);
        builder.Property(task => task.ModifiedBy).HasMaxLength(200);

        // Antrean tugas Assigned per operator.
        builder.HasIndex(task => new { task.Status, task.AssignedTo });

        builder.UseXminConcurrencyToken();
    }
}
