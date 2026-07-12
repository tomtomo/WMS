using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Wms.BuildingBlocks.Application.Abstractions;
using Wms.BuildingBlocks.Application.Abstractions.Ports;
using Wms.Outbound.Application.Features.CompletePickingTask;
using Wms.Outbound.Application.Features.CreateWave;
using Wms.Outbound.Contracts;
using Wms.Outbound.Domain;
using Wms.Outbound.Domain.Enums;
using Wms.Outbound.IntegrationTests.TestSupport;
using Xunit;

namespace Wms.Outbound.IntegrationTests;

// picking selesai, PickingCompleted. task terakhir selesai,  Wave Ready, WaveReady.
[Collection(PostgresCollection.Name)]
public sealed class CompletePickingTaskTests(PostgresFixture postgres) : IAsyncLifetime
{
    private const string PickingCompletedLogicalName = "outbound.picking_completed.v1";

    private const string WaveReadyLogicalName = "outbound.wave_ready.v1";

    private ServiceProvider _provider = null!;

    public async Task InitializeAsync()
    {
        var connectionString = await postgres.CreateFreshDatabaseAsync();
        _provider = OutboundTestHost.Build(connectionString);
        await OutboundTestHost.MigrateAsync(_provider);
    }

    public async Task DisposeAsync() => await _provider.DisposeAsync();

    [Fact]
    public async Task Completing_the_only_task_emits_picking_completed_and_readies_the_wave()
    {
        var (waveId, warehouseId, orderId) = await ReleaseWaveAsync();
        await PipelineRunner.ConsumeAsync(
            _provider,
            StockAllocationCompletedFactory.FullyAllocated(
                waveId, StockAllocationCompletedFactory.AllocationOf(orderId, "SKU-MILK", 10m, Guid.NewGuid())),
            Guid.NewGuid());
        var task = (await PipelineRunner.PickingTasksAsync(_provider)).Single();
        var stagingLocationId = Guid.NewGuid();
        var operatorId = Guid.NewGuid();

        var result = await PipelineRunner.SendAsync(
            _provider, new CompletePickingTaskCommand(task.Id.Value, task.Qty, stagingLocationId, operatorId));

        result.IsSuccess.Should().BeTrue(result.IsFailure ? result.Error.Message : string.Empty);

        (await PipelineRunner.PickingTasksAsync(_provider)).Single().Status.Should().Be(PickingTaskStatus.Completed);

        var completed = PipelineRunner.Payload<PickingCompleted>(
            (await PipelineRunner.OutboxRowsAsync(_provider, PickingCompletedLogicalName)).Should().ContainSingle().Subject);
        completed.OperatorId.Should().Be(operatorId);
        completed.ReservationId.Should().Be(task.ReservationId);
        completed.StockId.Should().Be(task.StockId);
        completed.StagingLocationId.Should().Be(stagingLocationId);

        (await PipelineRunner.WavesAsync(_provider)).Single().Status.Should().Be(WaveStatus.Ready);
        var ready = PipelineRunner.Payload<WaveReady>(
            (await PipelineRunner.OutboxRowsAsync(_provider, WaveReadyLogicalName)).Should().ContainSingle().Subject);
        ready.WarehouseId.Should().Be(warehouseId);
    }

    [Fact]
    public async Task Completing_a_task_emits_pick_completed_telemetry_with_operator_and_warehouse()
    {
        var (waveId, warehouseId, orderId) = await ReleaseWaveAsync();
        await PipelineRunner.ConsumeAsync(
            _provider,
            StockAllocationCompletedFactory.FullyAllocated(
                waveId, StockAllocationCompletedFactory.AllocationOf(orderId, "SKU-MILK", 10m, Guid.NewGuid())),
            Guid.NewGuid());
        var task = (await PipelineRunner.PickingTasksAsync(_provider)).Single();
        var operatorId = Guid.NewGuid();

        await PipelineRunner.SendAsync(
            _provider, new CompletePickingTaskCommand(task.Id.Value, task.Qty, Guid.NewGuid(), operatorId));

        var stream = (CapturingEventStreamPublisher)_provider.GetRequiredService<IEventStreamPublisher>();
        var records = stream.On<OperationalTelemetryRecord>(OperationalTelemetryStream.Name);
        records.Should().ContainSingle();
        records[0].EventType.Should().Be(OperationalTelemetryEventType.PickCompleted);
        records[0].WarehouseId.Should().Be(warehouseId);
        records[0].OperatorId.Should().Be(operatorId);
        records[0].EntityId.Should().Be(task.Id.Value);
        records[0].Quantity.Should().Be(task.Qty);
    }

    [Fact]
    public async Task Wave_stays_active_until_the_last_task_completes()
    {
        var (waveId, _, orderId) = await ReleaseWaveAsync();
        await PipelineRunner.ConsumeAsync(
            _provider,
            StockAllocationCompletedFactory.FullyAllocated(
                waveId,
                StockAllocationCompletedFactory.AllocationOf(orderId, "SKU-MILK", 5m, Guid.NewGuid()),
                StockAllocationCompletedFactory.AllocationOf(orderId, "SKU-MILK", 5m, Guid.NewGuid())),
            Guid.NewGuid());
        var tasks = await PipelineRunner.PickingTasksAsync(_provider);
        tasks.Should().HaveCount(2);

        await CompleteAsync(tasks[0]);
        (await PipelineRunner.WavesAsync(_provider)).Single().Status.Should().Be(WaveStatus.Active);
        (await PipelineRunner.OutboxRowsAsync(_provider, WaveReadyLogicalName)).Should().BeEmpty("belum semua task selesai");

        await CompleteAsync(tasks[1]);
        (await PipelineRunner.WavesAsync(_provider)).Single().Status.Should().Be(WaveStatus.Ready);
        (await PipelineRunner.OutboxRowsAsync(_provider, WaveReadyLogicalName)).Should().ContainSingle();
    }

    private async Task CompleteAsync(PickingTask task) =>
        (await PipelineRunner.SendAsync(
            _provider, new CompletePickingTaskCommand(task.Id.Value, task.Qty, Guid.NewGuid(), OperatorId: null)))
            .IsSuccess.Should().BeTrue();

    private async Task<(Guid WaveId, Guid WarehouseId, Guid OrderId)> ReleaseWaveAsync()
    {
        var orderId = await OutboundSeeder.SeedNewOrderAsync(_provider, "SKU-MILK", 10m);
        var warehouseId = Guid.NewGuid();
        var waveId = (await PipelineRunner.SendAsync(_provider, new CreateWaveCommand([orderId], warehouseId))).Value;
        return (waveId, warehouseId, orderId);
    }
}
