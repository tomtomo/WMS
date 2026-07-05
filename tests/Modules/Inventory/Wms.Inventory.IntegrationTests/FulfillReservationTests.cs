using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Wms.BuildingBlocks.Domain.Results;
using Wms.Inventory.Domain;
using Wms.Inventory.Domain.Enums;
using Wms.Inventory.IntegrationTests.TestSupport;
using Wms.Outbound.Contracts;
using Xunit;

namespace Wms.Inventory.IntegrationTests;

// Consume PickingCompleted, reservasi Active, Fulfilled dan split Stock Available, Picked
// @staging (konservasi qty), idempotent.
[Collection(PostgresCollection.Name)]
public sealed class FulfillReservationTests(PostgresFixture postgres) : IAsyncLifetime
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
    public async Task Picking_fulfills_reservation_and_splits_stock_to_picked_at_staging()
    {
        var sourceStockId = await StockSeeder.SeedAvailableAsync(_provider, qty: 100m);
        var (waveId, reservation) = await AllocateSingleAsync(qty: 40m);
        var stagingId = Guid.NewGuid();
        var pickingTaskId = Guid.NewGuid();

        var result = await PipelineRunner.ConsumeAsync(
            _provider, Picking(waveId, reservation, pickingTaskId, stagingId, qty: 40m), Guid.NewGuid());

        result.IsSuccess.Should().BeTrue(result.IsFailure ? result.Error.Message : string.Empty);

        var fulfilled = (await PipelineRunner.ReservationsAsync(_provider)).Single();
        fulfilled.Status.Should().Be(ReservationStatus.Fulfilled);
        fulfilled.PickingTaskId.Should().Be(pickingTaskId);

        var stocks = await PipelineRunner.StocksAsync(_provider);
        var source = stocks.Single(stock => stock.Id.Value == sourceStockId);
        var picked = stocks.Single(stock => stock.Status == StockStatus.Picked);

        source.Status.Should().Be(StockStatus.Available);
        source.Qty.Should().Be(60m);
        source.AvailableQty.Should().Be(60m, "klaim dilepas saat pick → availableQty = qty sisa");
        picked.Qty.Should().Be(40m);
        picked.LocationId.Value.Should().Be(stagingId);
        picked.WaveId.Should().Be(waveId);
        picked.PickingTaskId.Should().Be(pickingTaskId);
        (source.Qty + picked.Qty).Should().Be(100m, "konservasi qty: rack turun 40 = staging naik 40");
    }

    [Fact]
    public async Task Picking_replay_same_event_id_is_no_op()
    {
        await StockSeeder.SeedAvailableAsync(_provider, qty: 100m);
        var (waveId, reservation) = await AllocateSingleAsync(qty: 40m);
        var picking = Picking(waveId, reservation, Guid.NewGuid(), Guid.NewGuid(), qty: 40m);
        var eventId = Guid.NewGuid();

        (await PipelineRunner.ConsumeAsync(_provider, picking, eventId)).IsSuccess.Should().BeTrue();
        (await PipelineRunner.ConsumeAsync(_provider, picking, eventId)).IsSuccess.Should().BeTrue();

        var stocks = await PipelineRunner.StocksAsync(_provider);
        stocks.Count(stock => stock.Status == StockStatus.Picked).Should().Be(1, "redelivery eventId sama = no-op");
    }

    [Fact]
    public async Task Picking_replay_different_event_id_is_no_op_via_reservation_status()
    {
        await StockSeeder.SeedAvailableAsync(_provider, qty: 100m);
        var (waveId, reservation) = await AllocateSingleAsync(qty: 40m);
        var picking = Picking(waveId, reservation, Guid.NewGuid(), Guid.NewGuid(), qty: 40m);

        (await PipelineRunner.ConsumeAsync(_provider, picking, Guid.NewGuid())).IsSuccess.Should().BeTrue();
        (await PipelineRunner.ConsumeAsync(_provider, picking, Guid.NewGuid())).IsSuccess.Should().BeTrue();

        var stocks = await PipelineRunner.StocksAsync(_provider);
        stocks.Count(stock => stock.Status == StockStatus.Picked).Should().Be(
            1, "reservasi sudah Fulfilled → split tak diulang (idempotent natural)");
    }

    [Fact]
    public async Task Picking_unknown_reservation_returns_not_found()
    {
        var picking = new PickingCompleted(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "SKU-MILK", "LOT-01", 10m, Guid.NewGuid(), null);

        var result = await PipelineRunner.ConsumeAsync(_provider, picking, Guid.NewGuid());

        result.ErrorType.Should().Be(ResultErrorType.NotFound);
    }

    private static PickingCompleted Picking(
        Guid waveId, StockReservation reservation, Guid pickingTaskId, Guid stagingLocationId, decimal qty) =>
        new(
            waveId,
            pickingTaskId,
            reservation.StockId.Value,
            reservation.Id.Value,
            reservation.Sku.Value,
            reservation.Batch.Value,
            qty,
            stagingLocationId,
            Guid.NewGuid());

    private async Task<(Guid WaveId, StockReservation Reservation)> AllocateSingleAsync(decimal qty)
    {
        var waveId = Guid.NewGuid();
        await PipelineRunner.ConsumeAsync(
            _provider, WaveReleasedFactory.With(waveId, WaveReleasedFactory.Line(Guid.NewGuid(), qty: qty)), Guid.NewGuid());
        return (waveId, (await PipelineRunner.ReservationsAsync(_provider)).Single());
    }
}
