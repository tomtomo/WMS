using Wms.BuildingBlocks.Application.Messaging;
using Wms.Inbound.Contracts;
using Wms.Inventory.Contracts;
using Wms.Notifications.Consumers;
using Wms.Outbound.Contracts;

namespace Microsoft.Extensions.DependencyInjection;

// Registrasi consumer rail untuk modul Notifications.
public static class NotificationsRailConsumersExtensions
{
    public static IServiceCollection AddNotificationsRailConsumers(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddRailConsumer<GoodsReceiptPendingReview, GoodsReceiptPendingReviewConsumer>(
            DeliveryClass.Notification, (consumer, integrationEvent, envelope, ct) =>
                consumer.ConsumeAsync(integrationEvent, envelope.EventId, ct));

        services.AddRailConsumer<WaveReady, WaveReadyConsumer>(
            DeliveryClass.Notification, (consumer, integrationEvent, envelope, ct) =>
                consumer.ConsumeAsync(integrationEvent, envelope.EventId, ct));

        services.AddRailConsumer<StockAllocationCompleted, StockAllocationCompletedConsumer>(
            DeliveryClass.Notification, (consumer, integrationEvent, envelope, ct) =>
                consumer.ConsumeAsync(integrationEvent, envelope.EventId, ct));

        services.AddRailConsumer<StockNearExpiry, StockNearExpiryConsumer>(
            DeliveryClass.Notification, (consumer, integrationEvent, envelope, ct) =>
                consumer.ConsumeAsync(integrationEvent, envelope.EventId, ct));

        services.AddRailConsumer<PutawayTaskAssigned, PutawayTaskAssignedConsumer>(
            DeliveryClass.Notification, (consumer, integrationEvent, envelope, ct) =>
                consumer.ConsumeAsync(integrationEvent, envelope.EventId, ct));

        services.AddRailConsumer<PickingTaskAssigned, PickingTaskAssignedConsumer>(
            DeliveryClass.Notification, (consumer, integrationEvent, envelope, ct) =>
                consumer.ConsumeAsync(integrationEvent, envelope.EventId, ct));

        return services;
    }
}
