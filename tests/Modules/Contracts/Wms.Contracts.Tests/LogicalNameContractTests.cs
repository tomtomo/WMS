using AwesomeAssertions;
using Wms.BuildingBlocks.Infrastructure.Messaging;
using Wms.Inbound.Contracts;
using Wms.Inventory.Contracts;
using Wms.Outbound.Contracts;
using Xunit;

namespace Wms.Contracts.Tests;

// Cek LogicalName tiap event
public sealed class LogicalNameContractTests
{
    private static readonly IReadOnlyDictionary<Type, string> _expected = new Dictionary<Type, string>
    {
        [typeof(GRConfirmed)] = "inbound.gr_confirmed.v1",
        [typeof(GoodsReceiptPendingReview)] = "inbound.goods_receipt_pending_review.v1",
        [typeof(StockAllocationCompleted)] = "inventory.stock_allocation_completed.v1",
        [typeof(PutawayTaskAssigned)] = "inventory.putaway_task_assigned.v1",
        [typeof(PutawayCompleted)] = "inventory.putaway_completed.v1",
        [typeof(StockRemoved)] = "inventory.stock_removed.v1",
        [typeof(StockNearExpiry)] = "inventory.stock_near_expiry.v1",
        [typeof(WaveReleased)] = "outbound.wave_released.v1",
        [typeof(PickingTaskAssigned)] = "outbound.picking_task_assigned.v1",
        [typeof(WaveReady)] = "outbound.wave_ready.v1",
        [typeof(PickingCompleted)] = "outbound.picking_completed.v1",
        [typeof(ShipmentDispatched)] = "outbound.shipment_dispatched.v1",
    };

    [Fact]
    public void Exactly_the_twelve_declared_events_are_published()
    {
        ContractCatalog.EventTypes.Should().BeEquivalentTo(
            _expected.Keys,
            "himpunan event published = 12 kontrak yang di-deklarasi (LogicalNameFormat non-vacuous)");
    }

    [Fact]
    public void Each_event_resolves_to_its_declared_logical_name()
    {
        foreach (var (eventType, expectedName) in _expected)
        {
            IntegrationEventLogicalName.Resolve(eventType).Should().Be(expectedName);
        }
    }

    [Fact]
    public void Logical_name_prefix_equals_emitter_module()
    {
        foreach (var eventType in ContractCatalog.EventTypes)
        {
            // namespace 'Wms.<Module>.Contracts', prefix broker '<module>'
            var emitterModule = eventType.Namespace!.Split('.')[1].ToLowerInvariant();
            var logicalName = IntegrationEventLogicalName.Resolve(eventType);

            logicalName.Split('.')[0].Should().Be(
                emitterModule,
                $"{eventType.Name}: prefix LogicalName wajib = modul emitter");
        }
    }
}
