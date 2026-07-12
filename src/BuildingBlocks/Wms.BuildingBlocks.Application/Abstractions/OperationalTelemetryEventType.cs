namespace Wms.BuildingBlocks.Application.Abstractions;

// Jenis aktivitas operasional yang direkam. Extensible tanpa memecah kontrak stream.
public enum OperationalTelemetryEventType
{
    ScanCompleted,
    PutawayCompleted,
    PickCompleted,
}
