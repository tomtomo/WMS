using Wms.Inbound.Contracts;
using Wms.Inbound.Contracts.Enums;
using Wms.Inbound.Contracts.Payloads;
using Wms.Inventory.Contracts;
using Wms.Inventory.Contracts.Payloads;
using Wms.Outbound.Contracts;

namespace Wms.Reporting.IntegrationTests.TestSupport;

// Factory event contract untuk test
internal static class SampleEvents
{
    public static ReceivedLine Received(string sku, decimal qty, string? batch = null, ReceivedLineStatus status = ReceivedLineStatus.Good)
        => new(sku, qty, batch, null, status);

    public static RejectedLine Rejected(string sku, decimal qty, RejectionReason reason = RejectionReason.OverDelivery)
        => new(sku, qty, reason);

    public static GRConfirmed GrConfirmed(Guid warehouseId, Guid supplierId, IReadOnlyList<ReceivedLine> received, IReadOnlyList<RejectedLine>? rejected = null)
        => new(Guid.NewGuid(), warehouseId, supplierId, received, rejected ?? []);

    public static StockRemoved Removed(Guid warehouseId, string sku, decimal qty, string? batch = null)
        => new(Guid.NewGuid(), [new StockRemovedLine(warehouseId, sku, batch, qty)]);

    public static PutawayCompleted Putaway(Guid warehouseId, string sku, Guid? operatorId)
        => new(Guid.NewGuid(), Guid.NewGuid(), sku, warehouseId, operatorId);

    public static PickingCompleted Picking(string sku, decimal qty, Guid? operatorId, string? batch = null)
        => new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), sku, batch, qty, Guid.NewGuid(), operatorId);
}
