using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Wms.BuildingBlocks.Application.Messaging;
using Wms.BuildingBlocks.Domain.Results;
using Wms.Outbound.Application.Abstractions;
using Wms.Outbound.Application.Features.CreateWave;
using Wms.Outbound.Contracts;
using Wms.Outbound.Domain.Enums;
using Wms.Outbound.IntegrationTests.TestSupport;
using Xunit;

namespace Wms.Outbound.IntegrationTests;

// rilis wave — order New, InProgress, Wave Active, Outbox WaveReleased dalam satu transaksi.
[Collection(PostgresCollection.Name)]
public sealed class CreateWaveTests(PostgresFixture postgres) : IAsyncLifetime
{
    private const string WaveReleasedLogicalName = "outbound.wave_released.v1";

    private ServiceProvider _provider = null!;

    public async Task InitializeAsync()
    {
        var connectionString = await postgres.CreateFreshDatabaseAsync();
        _provider = OutboundTestHost.Build(connectionString);
        await OutboundTestHost.MigrateAsync(_provider);
    }

    public async Task DisposeAsync() => await _provider.DisposeAsync();

    [Fact]
    public async Task Create_wave_moves_orders_in_progress_active_wave_and_emits_wave_released()
    {
        var orderId = await OutboundSeeder.SeedNewOrderAsync(_provider, sku: "SKU-MILK", qty: 10m);
        var warehouseId = Guid.NewGuid();

        var result = await PipelineRunner.SendAsync(_provider, new CreateWaveCommand([orderId], warehouseId));

        result.IsSuccess.Should().BeTrue(result.IsFailure ? result.Error.Message : string.Empty);
        var waveId = result.Value;

        var order = (await PipelineRunner.OrdersAsync(_provider)).Should().ContainSingle().Subject;
        order.Status.Should().Be(OutboundOrderStatus.InProgress);
        order.WaveId!.Value.Should().Be(waveId);

        var wave = (await PipelineRunner.WavesAsync(_provider)).Should().ContainSingle().Subject;
        wave.Status.Should().Be(WaveStatus.Active);
        wave.WarehouseId.Should().Be(warehouseId);
        wave.OrderIds.Should().ContainSingle().Which.Value.Should().Be(orderId);

        var row = (await PipelineRunner.OutboxRowsAsync(_provider, WaveReleasedLogicalName)).Should().ContainSingle().Subject;
        row.DeliveryClass.Should().Be(DeliveryClass.CoreFlow);
        var payload = PipelineRunner.Payload<WaveReleased>(row);
        payload.WaveId.Should().Be(waveId);
        var line = payload.Lines.Should().ContainSingle().Subject;
        line.OrderId.Should().Be(orderId);
        line.Sku.Should().Be("SKU-MILK");
        line.Qty.Should().Be(10m);
    }

    [Fact]
    public async Task Create_wave_is_rejected_when_an_order_is_not_new()
    {
        var orderId = await OutboundSeeder.SeedNewOrderAsync(_provider);
        (await PipelineRunner.SendAsync(_provider, new CreateWaveCommand([orderId], Guid.NewGuid()))).IsSuccess
            .Should().BeTrue();

        var result = await PipelineRunner.SendAsync(_provider, new CreateWaveCommand([orderId], Guid.NewGuid()));

        result.ErrorType.Should().Be(ResultErrorType.Conflict);
        (await PipelineRunner.WavesAsync(_provider)).Should().ContainSingle("order sudah di wave lain — tak boleh double-wave");
    }

    [Fact]
    public async Task Create_wave_is_rejected_when_warehouse_unknown_in_master_data()
    {
        var orderId = await OutboundSeeder.SeedNewOrderAsync(_provider);
        var unknownWarehouse = Guid.NewGuid();
        ((FakeWarehouseReader)_provider.GetRequiredService<IWarehouseReader>()).MarkUnknown(unknownWarehouse);

        var result = await PipelineRunner.SendAsync(_provider, new CreateWaveCommand([orderId], unknownWarehouse));

        result.ErrorType.Should().Be(ResultErrorType.Validation);
        result.Error.Code.Should().Be("wave.warehouse_unknown");
        (await PipelineRunner.WavesAsync(_provider)).Should().BeEmpty();
    }

    [Fact]
    public async Task A_failed_create_wave_rolls_back_state_and_outbox_atomically()
    {
        var newOrder = await OutboundSeeder.SeedNewOrderAsync(_provider);
        var wavedOrder = await OutboundSeeder.SeedNewOrderAsync(_provider);
        await PipelineRunner.SendAsync(_provider, new CreateWaveCommand([wavedOrder], Guid.NewGuid()));

        // Order kedua sudah InProgress, command gabungan gagal. order pertama harus ikut rollback.
        var result = await PipelineRunner.SendAsync(_provider, new CreateWaveCommand([newOrder, wavedOrder], Guid.NewGuid()));

        result.IsFailure.Should().BeTrue();
        (await PipelineRunner.OrdersAsync(_provider)).Single(order => order.Id.Value == newOrder).Status
            .Should().Be(OutboundOrderStatus.New, "abort, order pertama tak jadi di-wave (anti dual-write)");
        (await PipelineRunner.WavesAsync(_provider)).Should().ContainSingle("hanya wave pertama yang commit");
        (await PipelineRunner.OutboxRowsAsync(_provider, WaveReleasedLogicalName)).Should().ContainSingle();
    }
}
