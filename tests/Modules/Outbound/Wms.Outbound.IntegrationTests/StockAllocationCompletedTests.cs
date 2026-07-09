using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Wms.Contracts.Abstractions;
using Wms.Outbound.Application.Features.CreateWave;
using Wms.Outbound.Contracts;
using Wms.Outbound.Domain.Enums;
using Wms.Outbound.IntegrationTests.TestSupport;
using Xunit;

namespace Wms.Outbound.IntegrationTests;

// reaksi StockAllocationCompleted
[Collection(PostgresCollection.Name)]
public sealed class StockAllocationCompletedTests(PostgresFixture postgres) : IAsyncLifetime
{
    private const string PickingTaskAssignedLogicalName = "outbound.picking_task_assigned.v1";

    private ServiceProvider _provider = null!;

    public async Task InitializeAsync()
    {
        var connectionString = await postgres.CreateFreshDatabaseAsync();
        _provider = OutboundTestHost.Build(connectionString);
        await OutboundTestHost.MigrateAsync(_provider);
    }

    public async Task DisposeAsync() => await _provider.DisposeAsync();

    [Fact]
    public async Task Oo_full_creates_a_picking_task_per_allocation_with_zero_backorder()
    {
        var (waveId, warehouseId, orderId) = await ReleaseWaveAsync(qty: 10m);
        var reservationId = Guid.NewGuid();

        var result = await PipelineRunner.ConsumeAsync(
            _provider,
            StockAllocationCompletedFactory.FullyAllocated(
                waveId, StockAllocationCompletedFactory.AllocationOf(orderId, "SKU-MILK", 10m, reservationId)),
            Guid.NewGuid());

        result.IsSuccess.Should().BeTrue(result.IsFailure ? result.Error.Message : string.Empty);

        var task = (await PipelineRunner.PickingTasksAsync(_provider)).Should().ContainSingle().Subject;
        task.ReservationId.Should().Be(reservationId);
        task.Status.Should().Be(PickingTaskStatus.Assigned);
        task.AssignedTo.Should().Be(FakePickAssignmentPolicy.Picker);

        var order = (await PipelineRunner.OrdersAsync(_provider)).Should().ContainSingle().Subject;
        order.OrderLines.Single().AllocationStatus.Should().Be(AllocationStatus.Allocated);
        order.Backorders.Should().BeEmpty();

        var row = (await PipelineRunner.OutboxRowsAsync(_provider, PickingTaskAssignedLogicalName)).Should().ContainSingle().Subject;
        row.DeliveryClass.Should().Be(DeliveryClass.Notification);
        PipelineRunner.Payload<PickingTaskAssigned>(row).WarehouseId.Should().Be(warehouseId);
    }

    [Fact]
    public async Task Oo_partial_creates_task_for_allocated_and_records_backorder_for_shortfall()
    {
        var (waveId, _, orderId) = await ReleaseWaveAsync(qty: 10m);
        var reservationId = Guid.NewGuid();

        var result = await PipelineRunner.ConsumeAsync(
            _provider,
            StockAllocationCompletedFactory.PartiallyAllocated(
                waveId,
                [StockAllocationCompletedFactory.AllocationOf(orderId, "SKU-MILK", 8m, reservationId)],
                [StockAllocationCompletedFactory.ShortfallOf(orderId, "SKU-MILK", 10m, 8m)]),
            Guid.NewGuid());

        result.IsSuccess.Should().BeTrue(result.IsFailure ? result.Error.Message : string.Empty);

        (await PipelineRunner.PickingTasksAsync(_provider)).Should().ContainSingle().Which.Qty.Should().Be(8m);

        var order = (await PipelineRunner.OrdersAsync(_provider)).Should().ContainSingle().Subject;
        order.OrderLines.Single().AllocationStatus.Should().Be(AllocationStatus.PartiallyAllocated);
        order.Backorders.Should().ContainSingle().Which.ShortQty.Should().Be(2m);
    }

    [Fact]
    public async Task Oo_unfulfilled_auto_cancels_wave_and_returns_order_to_backlog_silently()
    {
        var (waveId, _, orderId) = await ReleaseWaveAsync(qty: 10m);

        var result = await PipelineRunner.ConsumeAsync(
            _provider,
            StockAllocationCompletedFactory.Unfulfilled(
                waveId, StockAllocationCompletedFactory.ShortfallOf(orderId, "SKU-MILK", 10m, 0m)),
            Guid.NewGuid());

        result.IsSuccess.Should().BeTrue(result.IsFailure ? result.Error.Message : string.Empty);

        (await PipelineRunner.WavesAsync(_provider)).Should().ContainSingle().Which.Status.Should().Be(WaveStatus.Cancelled);

        var order = (await PipelineRunner.OrdersAsync(_provider)).Should().ContainSingle().Subject;
        order.Status.Should().Be(OutboundOrderStatus.New);
        order.WaveId.Should().BeNull();
        order.OrderLines.Single().AllocationStatus.Should().Be(AllocationStatus.Pending);

        (await PipelineRunner.PickingTasksAsync(_provider)).Should().BeEmpty("auto-cancel tak buat PickingTask");
        (await PipelineRunner.OutboxRowsAsync(_provider, PickingTaskAssignedLogicalName))
            .Should().BeEmpty("choreography silent — tak ada event lanjutan");
    }

    [Fact]
    public async Task Replay_same_event_id_is_no_op_via_inbox_guard()
    {
        var (waveId, _, orderId) = await ReleaseWaveAsync(qty: 10m);
        var evt = StockAllocationCompletedFactory.FullyAllocated(
            waveId, StockAllocationCompletedFactory.AllocationOf(orderId, "SKU-MILK", 10m, Guid.NewGuid()));
        var eventId = Guid.NewGuid();

        (await PipelineRunner.ConsumeAsync(_provider, evt, eventId)).IsSuccess.Should().BeTrue();
        (await PipelineRunner.ConsumeAsync(_provider, evt, eventId)).IsSuccess.Should().BeTrue();

        (await PipelineRunner.PickingTasksAsync(_provider)).Should().ContainSingle("redelivery eventId sama = no-op");
        (await PipelineRunner.OutboxRowsAsync(_provider, PickingTaskAssignedLogicalName)).Should().ContainSingle();
    }

    [Fact]
    public async Task Replay_different_event_id_same_reservation_is_no_op_via_natural_key()
    {
        var (waveId, _, orderId) = await ReleaseWaveAsync(qty: 10m);
        var evt = StockAllocationCompletedFactory.FullyAllocated(
            waveId, StockAllocationCompletedFactory.AllocationOf(orderId, "SKU-MILK", 10m, Guid.NewGuid()));

        (await PipelineRunner.ConsumeAsync(_provider, evt, Guid.NewGuid())).IsSuccess.Should().BeTrue();
        (await PipelineRunner.ConsumeAsync(_provider, evt, Guid.NewGuid())).IsSuccess.Should().BeTrue();

        (await PipelineRunner.PickingTasksAsync(_provider))
            .Should().ContainSingle("natural key (waveId, reservationId) → tak duplikat task");
        (await PipelineRunner.OutboxRowsAsync(_provider, PickingTaskAssignedLogicalName)).Should().ContainSingle();
    }

    // Rilis wave dari satu order backlog. Balik (waveId, warehouseId, orderId).
    private async Task<(Guid WaveId, Guid WarehouseId, Guid OrderId)> ReleaseWaveAsync(string sku = "SKU-MILK", decimal qty = 10m)
    {
        var orderId = await OutboundSeeder.SeedNewOrderAsync(_provider, sku, qty);
        var warehouseId = Guid.NewGuid();
        var waveId = (await PipelineRunner.SendAsync(_provider, new CreateWaveCommand([orderId], warehouseId))).Value;
        return (waveId, warehouseId, orderId);
    }
}
