using Wms.Contracts.Abstractions;
using Wms.Inbound.Contracts;
using Wms.Inventory.Application.Features.AllocateWave;
using Wms.Inventory.Application.Features.FulfillReservation;
using Wms.Inventory.Application.Features.ReceiveGoodsReceipt;
using Wms.Inventory.Application.Features.RemovePickedStock;
using Wms.Outbound.Contracts;

namespace Microsoft.Extensions.DependencyInjection;

// Registrasi consumer rail untuk modul Inventory.
public static class InventoryRailConsumersExtensions
{
    public static IServiceCollection AddInventoryRailConsumers(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddRailConsumer<GRConfirmed, GRConfirmedConsumer>(
            DeliveryClass.CoreFlow, (consumer, integrationEvent, envelope, ct) =>
                consumer.ConsumeAsync(integrationEvent, envelope.EventId, ct));

        services.AddRailConsumer<WaveReleased, WaveReleasedConsumer>(
            DeliveryClass.CoreFlow, (consumer, integrationEvent, envelope, ct) =>
                consumer.ConsumeAsync(integrationEvent, envelope.EventId, ct));

        services.AddRailConsumer<PickingCompleted, PickingCompletedConsumer>(
            DeliveryClass.CoreFlow, (consumer, integrationEvent, envelope, ct) =>
                consumer.ConsumeAsync(integrationEvent, envelope.EventId, ct));

        services.AddRailConsumer<ShipmentDispatched, ShipmentDispatchedConsumer>(
            DeliveryClass.CoreFlow, (consumer, integrationEvent, envelope, ct) =>
                consumer.ConsumeAsync(integrationEvent, envelope.EventId, ct));

        return services;
    }
}
