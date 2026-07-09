using Wms.Contracts.Abstractions;
using Wms.Inbound.Contracts;
using Wms.Inventory.Contracts;
using Wms.Outbound.Contracts;
using Wms.Reporting.Consumers;

namespace Microsoft.Extensions.DependencyInjection;

// Registrasi consumer rail untuk modul Reporting
public static class ReportingRailConsumersExtensions
{
    public static IServiceCollection AddReportingRailConsumers(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddRailConsumer<GRConfirmed, ReceivingSummaryConsumer>(
            DeliveryClass.CoreFlow, (consumer, integrationEvent, envelope, ct) =>
                consumer.ConsumeAsync(integrationEvent, envelope.EventId, envelope.OccurredAt, ct));

        services.AddRailConsumer<GRConfirmed, StockOnHandFromReceiptConsumer>(
            DeliveryClass.CoreFlow, (consumer, integrationEvent, envelope, ct) =>
                consumer.ConsumeAsync(integrationEvent, envelope.EventId, ct));

        services.AddRailConsumer<StockRemoved, StockRemovedConsumer>(
            DeliveryClass.CoreFlow, (consumer, integrationEvent, envelope, ct) =>
                consumer.ConsumeAsync(integrationEvent, envelope.EventId, envelope.OccurredAt, ct));

        services.AddRailConsumer<PutawayCompleted, PutawayCompletedConsumer>(
            DeliveryClass.CoreFlow, (consumer, integrationEvent, envelope, ct) =>
                consumer.ConsumeAsync(integrationEvent, envelope.EventId, envelope.OccurredAt, ct));

        services.AddRailConsumer<PickingCompleted, PickingCompletedConsumer>(
            DeliveryClass.CoreFlow, (consumer, integrationEvent, envelope, ct) =>
                consumer.ConsumeAsync(integrationEvent, envelope.EventId, envelope.OccurredAt, ct));

        return services;
    }
}
