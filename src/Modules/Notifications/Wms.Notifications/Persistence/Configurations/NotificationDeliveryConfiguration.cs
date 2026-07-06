using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wms.BuildingBlocks.Infrastructure.Persistence;
using Wms.Notifications.Deliveries;

namespace Wms.Notifications.Persistence.Configurations;

// Konfigurasi mapping untuk NotificationDelivery
internal sealed class NotificationDeliveryConfiguration : IEntityTypeConfiguration<NotificationDelivery>
{
    public void Configure(EntityTypeBuilder<NotificationDelivery> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("notification_delivery");

        builder.HasKey(delivery => delivery.Id);
        builder.Property(delivery => delivery.Id)
            .HasConversion(id => id.Value, value => DeliveryId.Create(value).Value)
            .ValueGeneratedNever();

        builder.Property(delivery => delivery.SubscriptionId);
        builder.Property(delivery => delivery.UserId);
        builder.Property(delivery => delivery.Channel).HasConversion<string>().HasMaxLength(20);
        builder.Property(delivery => delivery.Title).HasMaxLength(200);
        builder.Property(delivery => delivery.Body).HasMaxLength(2000);
        builder.Property(delivery => delivery.EventType).HasMaxLength(100);
        builder.Property(delivery => delivery.WarehouseId);
        builder.Property(delivery => delivery.EventRef).HasMaxLength(100);
        builder.Property(delivery => delivery.State).HasConversion<string>().HasMaxLength(20);
        builder.Property(delivery => delivery.ProviderMessageId).HasMaxLength(200);
        builder.Property(delivery => delivery.FailureReason).HasMaxLength(1000);
        builder.Property(delivery => delivery.RetryCount);

        builder.Property(delivery => delivery.CreatedBy).HasMaxLength(200);
        builder.Property(delivery => delivery.ModifiedBy).HasMaxLength(200);

        builder.HasIndex(delivery => delivery.State);
        builder.HasIndex(delivery => new { delivery.UserId, delivery.Channel });

        builder.UseXminConcurrencyToken();
    }
}
