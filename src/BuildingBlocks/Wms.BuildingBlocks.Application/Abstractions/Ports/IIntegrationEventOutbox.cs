using Wms.Contracts.Abstractions;

namespace Wms.BuildingBlocks.Application.Abstractions.Ports;

// Menulis baris outbox dalam transaksi EF yang sama dengan state, anti dual-write.
public interface IIntegrationEventOutbox
{
    Task AddAsync<TIntegrationEvent>(
        TIntegrationEvent integrationEvent,
        DeliveryClass deliveryClass,
        CancellationToken cancellationToken = default)
        where TIntegrationEvent : IIntegrationEvent;
}
