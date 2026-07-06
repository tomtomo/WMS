using Wms.Notifications.Abstractions;
using Wms.Notifications.Deliveries;
using Wms.Notifications.Subscriptions;

namespace Wms.Notifications.Consumers;

// Membuat delivery notifikasi untuk setiap penerima.
public sealed class NotificationFanout(SubscriptionResolver resolver, INotificationDeliveryRepository repository)
{
    // Resolve penerima lalu enqueue delivery untuk setiap user dan channel.
    public async Task FanOutAsync(
        string topic,
        NotificationContent content,
        Guid? warehouseId,
        string eventRef,
        CancellationToken cancellationToken = default)
    {
        var targets = await resolver.ResolveAsync(topic, warehouseId, cancellationToken);
        foreach (var target in targets)
        {
            await EnqueueAsync(target.SubscriptionId, target.UserId, target.Channel, content, warehouseId, eventRef, cancellationToken);
        }
    }

    // Enqueue delivery untuk satu user tertentu.
    public async Task EnqueueDirectAsync(
        Guid userId,
        IReadOnlyList<Channel> channels,
        NotificationContent content,
        Guid? warehouseId,
        string eventRef,
        CancellationToken cancellationToken = default)
    {
        foreach (var channel in channels)
        {
            await EnqueueAsync(subscriptionId: null, userId, channel, content, warehouseId, eventRef, cancellationToken);
        }
    }

    private async Task EnqueueAsync(
        Guid? subscriptionId,
        Guid userId,
        Channel channel,
        NotificationContent content,
        Guid? warehouseId,
        string eventRef,
        CancellationToken cancellationToken)
    {
        var delivery = NotificationDelivery.Enqueue(
            DeliveryId.Create(Guid.NewGuid()).Value,
            subscriptionId,
            userId,
            channel,
            content.Title,
            content.Body,
            content.SourceEventType,
            warehouseId,
            eventRef);

        // Simpan delivery jika berhasil dibuat.
        if (delivery.IsSuccess)
        {
            await repository.AddAsync(delivery.Value, cancellationToken);
        }
    }
}
