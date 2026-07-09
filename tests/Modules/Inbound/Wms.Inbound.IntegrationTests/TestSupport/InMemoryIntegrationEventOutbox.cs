using Wms.BuildingBlocks.Application.Abstractions.Ports;
using Wms.Contracts.Abstractions;

namespace Wms.Inbound.IntegrationTests.TestSupport;

// Double outbox untuk test translator
internal sealed class InMemoryIntegrationEventOutbox : IIntegrationEventOutbox
{
    private readonly List<(IIntegrationEvent Event, DeliveryClass DeliveryClass)> _entries = [];

    public IReadOnlyList<(IIntegrationEvent Event, DeliveryClass DeliveryClass)> Entries => _entries;

    public Task AddAsync<TIntegrationEvent>(
        TIntegrationEvent integrationEvent,
        DeliveryClass deliveryClass,
        CancellationToken cancellationToken = default)
        where TIntegrationEvent : IIntegrationEvent
    {
        _entries.Add((integrationEvent, deliveryClass));
        return Task.CompletedTask;
    }
}
