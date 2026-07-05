using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Wms.Outbound.Application.Abstractions;
using Wms.Outbound.Application.Features.CreateWave;
using Wms.Outbound.IntegrationTests.TestSupport;
using Xunit;

namespace Wms.Outbound.IntegrationTests;

// read port CQRS — AsNoTracking ke read DTO.
[Collection(PostgresCollection.Name)]
public sealed class ReadPortTests(PostgresFixture postgres) : IAsyncLifetime
{
    private ServiceProvider _provider = null!;

    public async Task InitializeAsync()
    {
        var connectionString = await postgres.CreateFreshDatabaseAsync();
        _provider = OutboundTestHost.Build(connectionString);
        await OutboundTestHost.MigrateAsync(_provider);
    }

    public async Task DisposeAsync() => await _provider.DisposeAsync();

    [Fact]
    public async Task Backlog_reader_returns_new_orders_with_per_line_allocation_status()
    {
        var orderId = await OutboundSeeder.SeedNewOrderAsync(_provider, "SKU-MILK", 10m);

        using var scope = _provider.CreateScope();
        var backlog = await scope.ServiceProvider.GetRequiredService<IOutboundOrderReader>().GetBacklogAsync();

        var order = backlog.Should().ContainSingle().Subject;
        order.OrderId.Should().Be(orderId);
        order.Status.Should().Be("New");
        order.Lines.Should().ContainSingle().Which.AllocationStatus.Should().Be("Pending");
    }

    [Fact]
    public async Task Wave_reader_returns_detail_with_rollup_and_wave_list_by_status()
    {
        var (waveId, warehouseId) = await ReleaseAndAllocateAsync();

        using var scope = _provider.CreateScope();
        var wave = await scope.ServiceProvider.GetRequiredService<IWaveReader>().GetByIdAsync(waveId);

        wave.Should().NotBeNull();
        wave!.WarehouseId.Should().Be(warehouseId);
        wave.Status.Should().Be("Active");
        wave.OrderIds.Should().ContainSingle().Which.Should().Be(wave.OrderIds[0]);
        wave.PickingTaskCount.Should().Be(1);
        wave.CompletedPickingTaskCount.Should().Be(0);

        var queue = await scope.ServiceProvider.GetRequiredService<IWaveListReader>()
            .GetByStatusAsync(warehouseId, "Active");
        queue.Should().ContainSingle().Which.WaveId.Should().Be(waveId);
    }

    [Fact]
    public async Task Picking_task_worklist_returns_assigned_tasks_for_the_operator()
    {
        await ReleaseAndAllocateAsync();

        using var scope = _provider.CreateScope();
        var worklist = await scope.ServiceProvider.GetRequiredService<IPickingTaskReader>()
            .GetWorklistAsync(FakePickAssignmentPolicy.Picker);

        var task = worklist.Should().ContainSingle().Subject;
        task.AssignedTo.Should().Be(FakePickAssignmentPolicy.Picker);
        task.Status.Should().Be("Assigned");
        task.Sku.Should().Be("SKU-MILK");
    }

    private async Task<(Guid WaveId, Guid WarehouseId)> ReleaseAndAllocateAsync()
    {
        var orderId = await OutboundSeeder.SeedNewOrderAsync(_provider, "SKU-MILK", 10m);
        var warehouseId = Guid.NewGuid();
        var waveId = (await PipelineRunner.SendAsync(_provider, new CreateWaveCommand([orderId], warehouseId))).Value;
        await PipelineRunner.ConsumeAsync(
            _provider,
            StockAllocationCompletedFactory.FullyAllocated(
                waveId, StockAllocationCompletedFactory.AllocationOf(orderId, "SKU-MILK", 10m, Guid.NewGuid())),
            Guid.NewGuid());
        return (waveId, warehouseId);
    }
}
