using Wms.Inbound.Contracts;
using Wms.Inventory.Contracts;
using Wms.Inventory.Contracts.Enums;
using Wms.Inventory.Contracts.Payloads;
using Wms.Outbound.Contracts;

namespace Wms.Notifications.IntegrationTests.TestSupport;

// Builder integration event contract dengan default valid.
internal static class SampleEvents
{
    public static GoodsReceiptPendingReview GrPendingReview(
        Guid warehouseId, bool hasOverDelivery = false, int discrepancyCount = 1) =>
        new(Guid.NewGuid(), warehouseId, hasOverDelivery, discrepancyCount);

    public static PutawayTaskAssigned PutawayAssigned(Guid assignedTo, Guid warehouseId) =>
        new(Guid.NewGuid(), Guid.NewGuid(), "SKU-MILK", assignedTo, warehouseId);

    public static PickingTaskAssigned PickingAssigned(Guid assignedTo, Guid warehouseId) =>
        new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "SKU-MILK", assignedTo, warehouseId);

    public static WaveReady WaveReady(Guid warehouseId) => new(Guid.NewGuid(), warehouseId);

    public static StockAllocationCompleted AllocationCompleted(bool withShortfall) =>
        new(
            Guid.NewGuid(),
            withShortfall ? AllocationStatus.PartiallyAllocated : AllocationStatus.FullyAllocated,
            [],
            withShortfall ? [new Shortfall(Guid.NewGuid(), "SKU-MILK", 100m, 60m, 40m)] : []);

    public static StockNearExpiry NearExpiry(Guid warehouseId) =>
        new(Guid.NewGuid(), "SKU-MILK", warehouseId, Guid.NewGuid(), "B1", new DateOnly(2026, 8, 1), 50m, 14);
}
