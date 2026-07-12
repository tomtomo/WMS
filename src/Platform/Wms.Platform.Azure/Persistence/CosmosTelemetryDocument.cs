using System.Globalization;
using Wms.BuildingBlocks.Application.Abstractions;

namespace Wms.Platform.Azure.Persistence;

// Dokumen telemetry di Cosmos menggunakan format camelCase, dengan warehouseId sebagai partition key.
internal sealed class CosmosTelemetryDocument
{
    public string Id { get; set; } = string.Empty;

    public Guid WarehouseId { get; set; }

    public DateTimeOffset OccurredAt { get; set; }

    public Guid? OperatorId { get; set; }

    public string EventType { get; set; } = string.Empty;

    public Guid EntityId { get; set; }

    public decimal? Quantity { get; set; }

    public static CosmosTelemetryDocument From(OperationalTelemetryRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        return new CosmosTelemetryDocument
        {
            // id = {occurredAtTicks}:{guid-pendek}
            Id = string.Create(CultureInfo.InvariantCulture, $"{record.OccurredAt.UtcTicks}:{Guid.NewGuid():N}"),
            WarehouseId = record.WarehouseId,
            OccurredAt = record.OccurredAt,
            OperatorId = record.OperatorId,
            EventType = record.EventType.ToString(),
            EntityId = record.EntityId,
            Quantity = record.Quantity,
        };
    }

    public OperationalTelemetryRecord ToRecord() => new(
        OccurredAt,
        WarehouseId,
        OperatorId,
        Enum.Parse<OperationalTelemetryEventType>(EventType),
        EntityId,
        Quantity);
}
