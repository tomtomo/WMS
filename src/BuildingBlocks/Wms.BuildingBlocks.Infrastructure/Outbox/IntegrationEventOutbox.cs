using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Wms.BuildingBlocks.Application.Abstractions.Ports;
using Wms.BuildingBlocks.Application.Messaging;
using Wms.BuildingBlocks.Infrastructure.Messaging;

namespace Wms.BuildingBlocks.Infrastructure.Outbox;

// Outbox Writer: Add ke DbContext modul dalam transaksi bisnis yang sama.
public sealed class IntegrationEventOutbox(DbContext dbContext, TimeProvider timeProvider) : IIntegrationEventOutbox
{
    public Task AddAsync<TIntegrationEvent>(
        TIntegrationEvent integrationEvent,
        DeliveryClass deliveryClass,
        CancellationToken cancellationToken = default)
        where TIntegrationEvent : IIntegrationEvent
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        var logicalName = ResolveLogicalName(integrationEvent.GetType());
        var envelope = MessageEnvelope.Create(
            integrationEvent,
            logicalName,
            deliveryClass,
            Guid.NewGuid(),
            timeProvider.GetUtcNow());

        dbContext.Set<OutboxRecord>().Add(new OutboxRecord
        {
            Id = envelope.EventId,
            LogicalName = envelope.LogicalName,
            DeliveryClass = envelope.DeliveryClass,
            OccurredAt = envelope.OccurredAt,
            Payload = envelope.Payload,
            Traceparent = envelope.Traceparent,
            Tracestate = envelope.Tracestate,
        });
        return Task.CompletedTask;
    }

    private static string ResolveLogicalName(Type eventType)
    {
        var field = eventType.GetField(
            "LogicalName",
            BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
        if (field is null || field.GetValue(null) is not string logicalName)
        {
            throw new InvalidOperationException(
                $"Integration event '{eventType.Name}' wajib punya 'public const string LogicalName'");
        }

        return logicalName;
    }
}
