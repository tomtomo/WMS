using Wms.Contracts.Abstractions;
using Wms.Outbound.Contracts.Payloads;

namespace Wms.Outbound.Contracts;

// Outbound ke Inventory: wave dirilis. Inventory alokasi Stock Available (FEFO) via reservasi.
public sealed record WaveReleased(
    Guid WaveId,
    IReadOnlyList<WaveLine> Lines) : IIntegrationEvent
{
    public const string LogicalName = "outbound.wave_released.v1";

    public const DeliveryClass DeliveryClass = DeliveryClass.CoreFlow;
}
