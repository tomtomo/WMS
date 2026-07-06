namespace Wms.BuildingBlocks.Infrastructure.Eventing;

// Konfigurasi eventing rail untuk modul.
public sealed class EventingRailOptions
{
    public required string ModuleQueueName { get; init; }
}
