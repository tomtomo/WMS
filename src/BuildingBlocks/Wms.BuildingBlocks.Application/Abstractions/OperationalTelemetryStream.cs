namespace Wms.BuildingBlocks.Application.Abstractions;

// Nama stream telemetry operasional: Event Hub (Azure), in proc (Local), Pub-Sub topic (GCP).
public static class OperationalTelemetryStream
{
    public const string Name = "wms-operational-telemetry";
}
