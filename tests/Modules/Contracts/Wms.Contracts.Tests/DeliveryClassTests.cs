using System.Reflection;
using AwesomeAssertions;
using Wms.Contracts.Abstractions;
using Wms.Inbound.Contracts;
using Wms.Inventory.Contracts;
using Wms.Inventory.Contracts.Enums;
using Wms.Outbound.Contracts;
using Xunit;

namespace Wms.Contracts.Tests;

// deliveryClass tiap event
public sealed class DeliveryClassTests
{
    private static readonly IReadOnlyDictionary<Type, DeliveryClass> _expectedPrimary = new Dictionary<Type, DeliveryClass>
    {
        [typeof(GRConfirmed)] = DeliveryClass.CoreFlow,
        [typeof(GoodsReceiptPendingReview)] = DeliveryClass.Notification,
        [typeof(StockAllocationCompleted)] = DeliveryClass.CoreFlow,
        [typeof(PutawayTaskAssigned)] = DeliveryClass.Notification,
        [typeof(PutawayCompleted)] = DeliveryClass.CoreFlow,
        [typeof(StockRemoved)] = DeliveryClass.CoreFlow,
        [typeof(StockNearExpiry)] = DeliveryClass.Notification,
        [typeof(WaveReleased)] = DeliveryClass.CoreFlow,
        [typeof(PickingTaskAssigned)] = DeliveryClass.Notification,
        [typeof(WaveReady)] = DeliveryClass.Notification,
        [typeof(PickingCompleted)] = DeliveryClass.CoreFlow,
        [typeof(ShipmentDispatched)] = DeliveryClass.CoreFlow,
    };

    [Fact]
    public void Each_event_declares_its_primary_delivery_class()
    {
        foreach (var (eventType, expected) in _expectedPrimary)
        {
            ReadConstDeliveryClass(eventType).Should().Be(expected, $"{eventType.Name} deliveryClass");
        }
    }

    [Fact]
    public void Stock_allocation_completed_is_dual_class_core_flow_and_notification()
    {
        StockAllocationCompleted.DeliveryClass.Should().Be(
            DeliveryClass.CoreFlow,
            "rail primer wajib-diproses = CoreFlow ke Outbound");
        StockAllocationCompleted.DeliveryClasses.Should().BeEquivalentTo(
            new[] { DeliveryClass.CoreFlow, DeliveryClass.Notification },
            "dual-rail: CoreFlow ke Outbound + Notification ke alert shortfall");
    }

    [Fact]
    public void Allocation_status_models_the_three_explicit_outcomes()
    {
        Enum.GetValues<AllocationStatus>().Should().BeEquivalentTo(
            new[] { AllocationStatus.FullyAllocated, AllocationStatus.PartiallyAllocated, AllocationStatus.Unfulfilled },
            "outcome eksplisit (Unfulfilled bukan via ketiadaan event)");
    }

    private static DeliveryClass ReadConstDeliveryClass(Type eventType)
    {
        var field = eventType.GetField(
            "DeliveryClass",
            BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
        field.Should().NotBeNull($"{eventType.Name} wajib punya const DeliveryClass");
        return (DeliveryClass)field!.GetValue(null)!;
    }
}
