using Wms.BuildingBlocks.Application.Messaging;

namespace Wms.BuildingBlocks.Infrastructure.Outbox;

public sealed class OutboxRecord
{
    public Guid Id { get; set; }
    public string LogicalName { get; set; } = string.Empty;
    public DeliveryClass DeliveryClass { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
    public string Payload { get; set; } = string.Empty;
    public string? Traceparent { get; set; }
    public string? Tracestate { get; set; }
    public DateTimeOffset? ProcessedAt { get; set; }
    public int AttemptCount { get; set; }
}
