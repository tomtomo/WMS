using System.Collections.Immutable;
using Wms.BuildingBlocks.Application.Messaging;
using Wms.Inventory.Contracts.Enums;
using Wms.Inventory.Contracts.Payloads;

namespace Wms.Inventory.Contracts;

// Inventory ke Outbound/Notifications: satu outcome event alokasi wave.
public sealed record StockAllocationCompleted(
    Guid WaveId,
    AllocationStatus Status,
    IReadOnlyList<Allocation> Allocations,
    IReadOnlyList<Shortfall> Shortfalls) : IIntegrationEvent
{
    public const string LogicalName = "inventory.stock_allocation_completed.v1";

    public const DeliveryClass DeliveryClass = DeliveryClass.CoreFlow;

    public static readonly ImmutableArray<DeliveryClass> DeliveryClasses =
        [DeliveryClass.CoreFlow, DeliveryClass.Notification];
}
