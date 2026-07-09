using Wms.Contracts.Abstractions;
using Wms.Inventory.Contracts;
using Wms.Outbound.Application.Features.HandleStockAllocationCompleted;

namespace Microsoft.Extensions.DependencyInjection;

// Registrasi consumer rail untuk modul Outbound
public static class OutboundRailConsumersExtensions
{
    public static IServiceCollection AddOutboundRailConsumers(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddRailConsumer<StockAllocationCompleted, StockAllocationCompletedConsumer>(
            DeliveryClass.CoreFlow, (consumer, integrationEvent, envelope, ct) =>
                consumer.ConsumeAsync(integrationEvent, envelope.EventId, ct));

        return services;
    }
}
