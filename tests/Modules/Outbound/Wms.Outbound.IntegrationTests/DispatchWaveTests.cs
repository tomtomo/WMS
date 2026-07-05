using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Wms.BuildingBlocks.Application.Messaging;
using Wms.BuildingBlocks.Domain.Results;
using Wms.Outbound.Application.Features.CompletePickingTask;
using Wms.Outbound.Application.Features.CreateWave;
using Wms.Outbound.Application.Features.DispatchWave;
using Wms.Outbound.Contracts;
using Wms.Outbound.Domain.Enums;
using Wms.Outbound.IntegrationTests.TestSupport;
using Xunit;

namespace Wms.Outbound.IntegrationTests;

// dispatch wave ready, ShipmentDispatched dan penutupan order (Closed / balik backlog).
[Collection(PostgresCollection.Name)]
public sealed class DispatchWaveTests(PostgresFixture postgres) : IAsyncLifetime
{
    private const string ShipmentDispatchedLogicalName = "outbound.shipment_dispatched.v1";

    private ServiceProvider _provider = null!;

    public async Task InitializeAsync()
    {
        var connectionString = await postgres.CreateFreshDatabaseAsync();
        _provider = OutboundTestHost.Build(connectionString);
        await OutboundTestHost.MigrateAsync(_provider);
    }

    public async Task DisposeAsync() => await _provider.DisposeAsync();

    [Fact]
    public async Task Dispatch_emits_shipment_dispatched_and_closes_a_fully_fulfilled_order()
    {
        var (waveId, _) = await ReadyWaveAsync(allocatedQty: 10m, demandQty: 10m);

        var result = await PipelineRunner.SendAsync(_provider, new DispatchWaveCommand(waveId));

        result.IsSuccess.Should().BeTrue(result.IsFailure ? result.Error.Message : string.Empty);
        (await PipelineRunner.WavesAsync(_provider)).Single().Status.Should().Be(WaveStatus.Dispatched);
        (await PipelineRunner.OrdersAsync(_provider)).Single().Status.Should().Be(OutboundOrderStatus.Closed);

        var row = (await PipelineRunner.OutboxRowsAsync(_provider, ShipmentDispatchedLogicalName)).Should().ContainSingle().Subject;
        row.DeliveryClass.Should().Be(DeliveryClass.CoreFlow);
        PipelineRunner.Payload<ShipmentDispatched>(row).WaveId.Should().Be(waveId);
    }

    [Fact]
    public async Task Dispatch_returns_a_backordered_order_to_backlog_re_waveable()
    {
        var (waveId, _) = await ReadyWaveAsync(allocatedQty: 8m, demandQty: 10m);

        var result = await PipelineRunner.SendAsync(_provider, new DispatchWaveCommand(waveId));

        result.IsSuccess.Should().BeTrue(result.IsFailure ? result.Error.Message : string.Empty);
        (await PipelineRunner.WavesAsync(_provider)).Single().Status.Should().Be(WaveStatus.Dispatched);

        var order = (await PipelineRunner.OrdersAsync(_provider)).Single();
        order.Status.Should().Be(OutboundOrderStatus.New);
        order.WaveId.Should().BeNull();
        var line = order.OrderLines.Should().ContainSingle().Subject;
        line.Qty.Should().Be(2m, "sisa demand backorder");
        line.AllocationStatus.Should().Be(AllocationStatus.Pending);
        (await PipelineRunner.OutboxRowsAsync(_provider, ShipmentDispatchedLogicalName)).Should().ContainSingle();
    }

    [Fact]
    public async Task Dispatch_is_rejected_before_the_wave_is_ready()
    {
        var orderId = await OutboundSeeder.SeedNewOrderAsync(_provider);
        var waveId = (await PipelineRunner.SendAsync(_provider, new CreateWaveCommand([orderId], Guid.NewGuid()))).Value;

        var result = await PipelineRunner.SendAsync(_provider, new DispatchWaveCommand(waveId));

        result.ErrorType.Should().Be(ResultErrorType.Conflict);
        result.Error.Code.Should().Be("wave.not_ready");
        (await PipelineRunner.OutboxRowsAsync(_provider, ShipmentDispatchedLogicalName)).Should().BeEmpty();
    }

    // Rilis wave, alokasi (penuh/parsial), selesaikan picking, wave Ready. Balik (waveId, orderId).
    private async Task<(Guid WaveId, Guid OrderId)> ReadyWaveAsync(decimal allocatedQty, decimal demandQty)
    {
        var orderId = await OutboundSeeder.SeedNewOrderAsync(_provider, "SKU-MILK", demandQty);
        var waveId = (await PipelineRunner.SendAsync(_provider, new CreateWaveCommand([orderId], Guid.NewGuid()))).Value;

        var allocationEvent = allocatedQty >= demandQty
            ? StockAllocationCompletedFactory.FullyAllocated(
                waveId, StockAllocationCompletedFactory.AllocationOf(orderId, "SKU-MILK", allocatedQty, Guid.NewGuid()))
            : StockAllocationCompletedFactory.PartiallyAllocated(
                waveId,
                [StockAllocationCompletedFactory.AllocationOf(orderId, "SKU-MILK", allocatedQty, Guid.NewGuid())],
                [StockAllocationCompletedFactory.ShortfallOf(orderId, "SKU-MILK", demandQty, allocatedQty)]);
        await PipelineRunner.ConsumeAsync(_provider, allocationEvent, Guid.NewGuid());

        var task = (await PipelineRunner.PickingTasksAsync(_provider)).Single();
        await PipelineRunner.SendAsync(
            _provider, new CompletePickingTaskCommand(task.Id.Value, task.Qty, Guid.NewGuid(), OperatorId: null));
        return (waveId, orderId);
    }
}
