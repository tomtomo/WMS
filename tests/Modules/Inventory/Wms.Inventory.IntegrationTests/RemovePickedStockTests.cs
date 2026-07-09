using System.Text.Json;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Wms.BuildingBlocks.Application.Messaging;
using Wms.BuildingBlocks.Infrastructure.Outbox;
using Wms.Contracts.Abstractions;
using Wms.Inventory.Contracts;
using Wms.Inventory.Domain.Enums;
using Wms.Inventory.IntegrationTests.TestSupport;
using Wms.Outbound.Contracts;
using Xunit;

namespace Wms.Inventory.IntegrationTests;

// consume ShipmentDispatched, hapus semua Stock Picked terikat wave, emit StockRemoved
// (CoreFlow ke Reporting), idempotent.
[Collection(PostgresCollection.Name)]
public sealed class RemovePickedStockTests(PostgresFixture postgres) : IAsyncLifetime
{
    private const string RemovedLogicalName = "inventory.stock_removed.v1";

    private ServiceProvider _provider = null!;

    public async Task InitializeAsync()
    {
        var connectionString = await postgres.CreateFreshDatabaseAsync();
        _provider = InventoryTestHost.Build(connectionString);
        await InventoryTestHost.MigrateAsync(_provider);
    }

    public async Task DisposeAsync() => await _provider.DisposeAsync();

    [Fact]
    public async Task Dispatch_removes_picked_stock_and_emits_stock_removed()
    {
        var warehouseId = Guid.NewGuid();
        var waveId = await SetupPickedWaveAsync(warehouseId, pickedQty: 40m);

        var result = await PipelineRunner.ConsumeAsync(_provider, new ShipmentDispatched(waveId), Guid.NewGuid());

        result.IsSuccess.Should().BeTrue(result.IsFailure ? result.Error.Message : string.Empty);

        var stocks = await PipelineRunner.StocksAsync(_provider);
        stocks.Should().NotContain(stock => stock.Status == StockStatus.Picked, "semua Picked wave terhapus saat dispatch");

        var rows = await PipelineRunner.OutboxRowsAsync(_provider, RemovedLogicalName);
        rows.Should().ContainSingle();
        rows[0].DeliveryClass.Should().Be(DeliveryClass.CoreFlow);

        var payload = Payload(rows[0]);
        payload.WaveId.Should().Be(waveId);
        var line = payload.Lines.Should().ContainSingle().Which;
        line.WarehouseId.Should().Be(warehouseId);
        line.Sku.Should().Be("SKU-MILK");
        line.Qty.Should().Be(40m);
    }

    [Fact]
    public async Task Dispatch_of_clean_wave_is_no_op()
    {
        var result = await PipelineRunner.ConsumeAsync(_provider, new ShipmentDispatched(Guid.NewGuid()), Guid.NewGuid());

        result.IsSuccess.Should().BeTrue();
        (await PipelineRunner.OutboxRowsAsync(_provider, RemovedLogicalName)).Should().BeEmpty(
            "wave tanpa Picked → tak emit StockRemoved kosong");
    }

    [Fact]
    public async Task Dispatch_replay_same_event_id_is_no_op()
    {
        var waveId = await SetupPickedWaveAsync(Guid.NewGuid(), pickedQty: 40m);
        var eventId = Guid.NewGuid();

        (await PipelineRunner.ConsumeAsync(_provider, new ShipmentDispatched(waveId), eventId)).IsSuccess.Should().BeTrue();
        (await PipelineRunner.ConsumeAsync(_provider, new ShipmentDispatched(waveId), eventId)).IsSuccess.Should().BeTrue();

        (await PipelineRunner.OutboxRowsAsync(_provider, RemovedLogicalName)).Should().ContainSingle("redelivery eventId sama = no-op");
    }

    [Fact]
    public async Task Dispatch_replay_different_event_id_is_no_op_via_clean_wave()
    {
        var waveId = await SetupPickedWaveAsync(Guid.NewGuid(), pickedQty: 40m);

        (await PipelineRunner.ConsumeAsync(_provider, new ShipmentDispatched(waveId), Guid.NewGuid())).IsSuccess.Should().BeTrue();
        (await PipelineRunner.ConsumeAsync(_provider, new ShipmentDispatched(waveId), Guid.NewGuid())).IsSuccess.Should().BeTrue();

        (await PipelineRunner.OutboxRowsAsync(_provider, RemovedLogicalName)).Should().ContainSingle(
            "dispatch kedua: wave sudah bersih → no-op (tak StockRemoved ganda)");
    }

    private static StockRemoved Payload(OutboxRecord row) =>
        JsonSerializer.Deserialize<StockRemoved>(row.Payload, MessageEnvelope.PayloadSerializerOptions)!;

    private async Task<Guid> SetupPickedWaveAsync(Guid warehouseId, decimal pickedQty)
    {
        await StockSeeder.SeedAvailableAsync(_provider, qty: pickedQty + 60m, warehouseId: warehouseId);
        var waveId = Guid.NewGuid();
        await PipelineRunner.ConsumeAsync(
            _provider, WaveReleasedFactory.With(waveId, WaveReleasedFactory.Line(Guid.NewGuid(), qty: pickedQty)), Guid.NewGuid());

        var reservation = (await PipelineRunner.ReservationsAsync(_provider)).Single();
        var picking = new PickingCompleted(
            waveId,
            Guid.NewGuid(),
            reservation.StockId.Value,
            reservation.Id.Value,
            reservation.Sku.Value,
            reservation.Batch.Value,
            pickedQty,
            Guid.NewGuid(),
            null);
        await PipelineRunner.ConsumeAsync(_provider, picking, Guid.NewGuid());
        return waveId;
    }
}
