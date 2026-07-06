using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Wms.BuildingBlocks.Infrastructure.Persistence;
using Wms.Notifications.Deliveries;
using Wms.Notifications.Subscriptions;

namespace Wms.Notifications.Persistence.Configurations;

// Mapping NotificationSubscription.
internal sealed class NotificationSubscriptionConfiguration : IEntityTypeConfiguration<NotificationSubscription>
{
    private static readonly JsonSerializerOptions _json = new()
    {
        Converters = { new JsonStringEnumConverter() },
    };

    public void Configure(EntityTypeBuilder<NotificationSubscription> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("notification_subscription");

        builder.HasKey(subscription => subscription.Id);
        builder.Property(subscription => subscription.Id)
            .HasConversion(id => id.Value, value => SubscriptionId.Create(value).Value)
            .ValueGeneratedNever();

        builder.Property(subscription => subscription.SubscriberType).HasConversion<string>().HasMaxLength(20);
        builder.Property(subscription => subscription.SubscriberId);
        builder.Property(subscription => subscription.EventType).HasMaxLength(100);
        builder.Property(subscription => subscription.WarehouseScope);
        builder.Property(subscription => subscription.IsActive);

        // Simpan daftar channel sebagai JSON.
        var channelsConverter = new ValueConverter<List<Channel>, string>(
            channels => JsonSerializer.Serialize(channels, _json),
            json => JsonSerializer.Deserialize<List<Channel>>(json, _json) ?? new List<Channel>());
        var channelsComparer = new ValueComparer<List<Channel>>(
            (left, right) => left!.SequenceEqual(right!),
            channels => channels.Aggregate(0, (hash, channel) => HashCode.Combine(hash, channel.GetHashCode())),
            channels => channels.ToList());
        builder.Property<List<Channel>>("_channels")
            .HasColumnName("channels")
            .HasColumnType("jsonb")
            .HasConversion(channelsConverter, channelsComparer);
        builder.Ignore(subscription => subscription.Channels);

        builder.Property(subscription => subscription.CreatedBy).HasMaxLength(200);
        builder.Property(subscription => subscription.ModifiedBy).HasMaxLength(200);

        // Hanya tampilkan subscription yang masih aktif
        builder.HasQueryFilter(subscription => subscription.IsActive);
        builder.HasIndex(subscription => subscription.EventType);

        builder.UseXminConcurrencyToken();
    }
}
