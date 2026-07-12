using Wms.Contracts.Abstractions;

namespace Wms.BuildingBlocks.Application.Abstractions;

// Data aktivitas gudang berfrekuensi tinggi seperti scan, putaway, dan picking.
// Data ini bukan integration event, boleh hilang, dan tidak melalui outbox atau kontrak AsyncAPI.
public sealed record OperationalTelemetryRecord(
    DateTimeOffset OccurredAt,
    Guid WarehouseId,
    Guid? OperatorId,
    OperationalTelemetryEventType EventType,
    Guid EntityId,
    decimal? Quantity) : IHasPartitionKey
{
    // Gunakan WarehouseId sebagai partition key tanpa menambah field partitionKey ke payload JSON.
    string IHasPartitionKey.PartitionKey => WarehouseId.ToString();
}
