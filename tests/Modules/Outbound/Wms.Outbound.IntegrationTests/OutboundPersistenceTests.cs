using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Wms.BuildingBlocks.Application.Abstractions;
using Wms.Outbound.Application.Abstractions;
using Wms.Outbound.Domain;
using Wms.Outbound.Domain.Enums;
using Wms.Outbound.Domain.ValueObjects;
using Wms.Outbound.IntegrationTests.TestSupport;
using Xunit;

namespace Wms.Outbound.IntegrationTests;

[Collection(PostgresCollection.Name)]
public sealed class OutboundPersistenceTests(PostgresFixture postgres) : IAsyncLifetime
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
    public async Task Wave_round_trips_with_warehouse_and_id_collections()
    {
        var waveId = WaveId.Create(Guid.NewGuid()).Value;
        var warehouseId = Guid.NewGuid();
        var orderId = OutboundOrderId.Create(Guid.NewGuid()).Value;
        var taskId = PickingTaskId.Create(Guid.NewGuid()).Value;

        await PersistAsync(async provider =>
        {
            var wave = Wave.Create(waveId, warehouseId, [orderId], []).Value;
            wave.AttachPickingTask(taskId);
            wave.ClearDomainEvents();
            await provider.GetRequiredService<IWaveRepository>().AddAsync(wave);
        });

        using var read = _provider.CreateScope();
        var loaded = await read.ServiceProvider.GetRequiredService<IWaveRepository>().GetAsync(waveId);

        loaded.Should().NotBeNull();
        loaded!.WarehouseId.Should().Be(warehouseId);
        loaded.Status.Should().Be(WaveStatus.Active);
        loaded.OrderIds.Should().ContainSingle().Which.Should().Be(orderId);
        loaded.PickingTaskIds.Should().ContainSingle().Which.Should().Be(taskId);
    }

    [Fact]
    public async Task Outbound_order_round_trips_with_ship_to_and_lines()
    {
        var orderId = OutboundOrderId.Create(Guid.NewGuid()).Value;

        await PersistAsync(async provider =>
        {
            var order = OutboundOrder.Create(
                orderId,
                Guid.NewGuid(),
                ShipTo.Create("Toko Tom", "Jl. Merdeka 1", "Jakarta").Value,
                [OrderLine.Create("SKU-MILK", 10m, Uom.Create("CARTON").Value).Value]).Value;
            order.ClearDomainEvents();
            await provider.GetRequiredService<IOutboundOrderRepository>().AddAsync(order);
        });

        using var read = _provider.CreateScope();
        var loaded = await read.ServiceProvider.GetRequiredService<IOutboundOrderRepository>().GetAsync(orderId);

        loaded.Should().NotBeNull();
        loaded!.Status.Should().Be(OutboundOrderStatus.New);
        loaded.ShipTo.City.Should().Be("Jakarta");
        var line = loaded.OrderLines.Should().ContainSingle().Subject;
        line.Sku.Should().Be("SKU-MILK");
        line.Qty.Should().Be(10m);
        line.Uom.Value.Should().Be("CARTON");
        line.AllocationStatus.Should().Be(AllocationStatus.Pending);
    }

    [Fact]
    public async Task Picking_task_round_trips_with_its_fields()
    {
        var taskId = PickingTaskId.Create(Guid.NewGuid()).Value;
        var waveId = WaveId.Create(Guid.NewGuid()).Value;
        var reservationId = Guid.NewGuid();

        await PersistAsync(async provider =>
        {
            var task = PickingTask.Create(
                taskId,
                waveId,
                reservationId,
                stockId: Guid.NewGuid(),
                sourceLocationId: Guid.NewGuid(),
                sku: "SKU-MILK",
                batch: "LOT-1",
                qty: 10m,
                assignedTo: Guid.NewGuid()).Value;
            task.ClearDomainEvents();
            await provider.GetRequiredService<IPickingTaskRepository>().AddAsync(task);
        });

        using var read = _provider.CreateScope();
        var loaded = await read.ServiceProvider.GetRequiredService<IPickingTaskRepository>().GetAsync(taskId);

        loaded.Should().NotBeNull();
        loaded!.WaveId.Should().Be(waveId);
        loaded.ReservationId.Should().Be(reservationId);
        loaded.Sku.Should().Be("SKU-MILK");
        loaded.Batch.Should().Be("LOT-1");
        loaded.Status.Should().Be(PickingTaskStatus.Assigned);
    }

    private async Task PersistAsync(Func<IServiceProvider, Task> action)
    {
        using var scope = _provider.CreateScope();
        await action(scope.ServiceProvider);
        var result = await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync();
        result.IsSuccess.Should().BeTrue(result.IsFailure ? result.Error.Message : string.Empty);
    }
}
