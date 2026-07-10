using Wms.Contracts.Abstractions;

namespace Wms.Outbound.Contracts;

// Outbound ke Inventory: wave di dispatch. Inventory remove Stock Picked lalu emit StockRemoved.
public sealed record ShipmentDispatched(
    Guid WaveId) : IIntegrationEvent, IHasPartitionKey
{
    public const string LogicalName = "outbound.shipment_dispatched.v1";

    public const DeliveryClass DeliveryClass = DeliveryClass.CoreFlow;

    string IHasPartitionKey.PartitionKey => WaveId.ToString();
}
