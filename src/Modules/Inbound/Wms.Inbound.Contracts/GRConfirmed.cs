using Wms.BuildingBlocks.Application.Messaging;
using Wms.Inbound.Contracts.Payloads;

namespace Wms.Inbound.Contracts;

// Inbound ke Inventory/Reporting: GR diconfirm SPV.
public sealed record GRConfirmed(
    Guid GrId,
    Guid WarehouseId,
    Guid SupplierId,
    IReadOnlyList<ReceivedLine> ReceivedLines,
    IReadOnlyList<RejectedLine> RejectedLines) : IIntegrationEvent
{
    public const string LogicalName = "inbound.gr_confirmed.v1";

    public const DeliveryClass DeliveryClass = DeliveryClass.CoreFlow;
}
