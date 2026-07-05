using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Wms.Inventory.Application.Abstractions;
using Wms.Inventory.Application.Features.CompletePutaway;
using Wms.Inventory.IntegrationTests.TestSupport;
using Xunit;

namespace Wms.Inventory.IntegrationTests;

// Read port: AsNoTracking, read DTO, availableQty.
[Collection(PostgresCollection.Name)]
public sealed class ReadPortTests(PostgresFixture postgres) : IAsyncLifetime
{
    private ServiceProvider _provider = null!;

    public async Task InitializeAsync()
    {
        var connectionString = await postgres.CreateFreshDatabaseAsync();
        _provider = InventoryTestHost.Build(connectionString);
        await InventoryTestHost.MigrateAsync(_provider);
    }

    public async Task DisposeAsync() => await _provider.DisposeAsync();

    [Fact]
    public async Task StockReader_returns_available_balance_with_available_qty()
    {
        await ReceiveAndPutawayAsync(qty: 80m);

        using var scope = _provider.CreateScope();
        var reader = scope.ServiceProvider.GetRequiredService<IStockReader>();
        var available = await reader.GetAvailableAsync(GrConfirmedFactory.WarehouseId, null);

        available.Should().ContainSingle();
        available[0].Status.Should().Be("Available");
        available[0].Sku.Should().Be("SKU-MILK");
        available[0].AvailableQty.Should().Be(80m, "belum ada reservasi, availableQty = qty");
        available[0].Qty.Should().Be(80m);
    }

    [Fact]
    public async Task StockReader_get_available_excludes_quarantine()
    {
        await PipelineRunner.ConsumeAsync(
            _provider,
            GrConfirmedFactory.With(Guid.NewGuid(), GrConfirmedFactory.QcHold(qty: 10m)),
            Guid.NewGuid());

        using var scope = _provider.CreateScope();
        var reader = scope.ServiceProvider.GetRequiredService<IStockReader>();
        (await reader.GetAvailableAsync(GrConfirmedFactory.WarehouseId, null)).Should().BeEmpty("Quarantine bukan Available");
    }

    [Fact]
    public async Task StockReader_get_available_filters_by_sku()
    {
        await ReceiveAndPutawayAsync(qty: 50m);

        using var scope = _provider.CreateScope();
        var reader = scope.ServiceProvider.GetRequiredService<IStockReader>();
        (await reader.GetAvailableAsync(GrConfirmedFactory.WarehouseId, "SKU-MILK")).Should().ContainSingle();
        (await reader.GetAvailableAsync(GrConfirmedFactory.WarehouseId, "SKU-OTHER")).Should().BeEmpty();
    }

    [Fact]
    public async Task PutawayTaskReader_returns_assigned_queue()
    {
        await PipelineRunner.ConsumeAsync(
            _provider,
            GrConfirmedFactory.With(Guid.NewGuid(), GrConfirmedFactory.Good()),
            Guid.NewGuid());

        using var scope = _provider.CreateScope();
        var reader = scope.ServiceProvider.GetRequiredService<IPutawayTaskReader>();
        var queue = await reader.GetQueueAsync(null);

        queue.Should().ContainSingle();
        queue[0].Status.Should().Be("Assigned");
        queue[0].AssignedTo.Should().Be(FakeReceivingPolicy.PutawayAssignee);
    }

    [Fact]
    public async Task StockReservationReader_returns_active_reservations_for_wave()
    {
        await StockSeeder.SeedAvailableAsync(_provider, qty: 100m);
        var waveId = Guid.NewGuid();
        await PipelineRunner.ConsumeAsync(
            _provider, WaveReleasedFactory.With(waveId, WaveReleasedFactory.Line(Guid.NewGuid(), qty: 30m)), Guid.NewGuid());

        using var scope = _provider.CreateScope();
        var reader = scope.ServiceProvider.GetRequiredService<IStockReservationReader>();
        var reservations = await reader.GetByWaveAsync(waveId);

        reservations.Should().ContainSingle();
        reservations[0].WaveId.Should().Be(waveId);
        reservations[0].Qty.Should().Be(30m);
        reservations[0].Status.Should().Be("Active");
        (await reader.GetByWaveAsync(Guid.NewGuid())).Should().BeEmpty("wave lain tidak punya reservasi");
    }

    // Receive Good (OnHand) lalu CompletePutaway (Available) agar balance masuk read availability.
    private async Task ReceiveAndPutawayAsync(decimal qty)
    {
        await PipelineRunner.ConsumeAsync(
            _provider,
            GrConfirmedFactory.With(Guid.NewGuid(), GrConfirmedFactory.Good(qty: qty)),
            Guid.NewGuid());
        var task = (await PipelineRunner.TasksAsync(_provider)).Single();
        await PipelineRunner.SendAsync(_provider, new CompletePutawayCommand(task.Id.Value, Guid.NewGuid(), null));
    }
}
