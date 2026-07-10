using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Wms.BuildingBlocks.Application.Abstractions.Ports;
using Wms.BuildingBlocks.Application.Messaging;
using Wms.BuildingBlocks.Infrastructure.Messaging;
using Wms.Contracts.Abstractions;

namespace Wms.BuildingBlocks.Infrastructure.Outbox;

// Menyimpan integration event ke outbox dalam transaksi yang sama.
public sealed class IntegrationEventOutbox(DbContext dbContext, TimeProvider timeProvider) : IIntegrationEventOutbox
{
    public Task AddAsync<TIntegrationEvent>(
        TIntegrationEvent integrationEvent,
        DeliveryClass deliveryClass,
        CancellationToken cancellationToken = default)
        where TIntegrationEvent : IIntegrationEvent
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        var logicalName = IntegrationEventLogicalName.Resolve(integrationEvent.GetType());

        // Simpan trace context W3C jika tersedia.
        var activity = Activity.Current;
        var traceparent = activity?.IdFormat == ActivityIdFormat.W3C ? activity.Id : null;
        var tracestate = traceparent is not null ? activity!.TraceStateString : null;

        var envelope = MessageEnvelope.Create(
            integrationEvent,
            logicalName,
            deliveryClass,
            Guid.NewGuid(),
            timeProvider.GetUtcNow()) with
        {
            Traceparent = traceparent,
            Tracestate = tracestate,
        };

        dbContext.Set<OutboxRecord>().Add(new OutboxRecord
        {
            Id = envelope.EventId,
            LogicalName = envelope.LogicalName,
            DeliveryClass = envelope.DeliveryClass,
            OccurredAt = envelope.OccurredAt,
            Payload = envelope.Payload,
            Traceparent = envelope.Traceparent,
            Tracestate = envelope.Tracestate,
            PartitionKey = envelope.PartitionKey,
        });
        return Task.CompletedTask;
    }
}
