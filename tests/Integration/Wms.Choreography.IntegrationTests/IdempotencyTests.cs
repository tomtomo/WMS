using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Wms.BuildingBlocks.Application.Messaging;
using Wms.Choreography.IntegrationTests.TestSupport;
using Wms.Inbound.Application.Features.CompleteScan;
using Wms.Inbound.Application.Features.ConfirmGoodsReceipt;
using Wms.Inbound.Application.Features.CreateGoodsReceiptHeader;
using Wms.Inbound.Application.Features.ScanReceiptLine;
using Wms.Inbound.Contracts;
using Wms.Inbound.Domain.Enums;
using Wms.Inventory.Contracts;
using Wms.Inventory.Domain;
using Wms.Inventory.Infrastructure;
using Wms.Outbound.Application.Features.CreateWave;
using Wms.Outbound.Contracts;
using Wms.Outbound.Domain;
using Wms.Outbound.Infrastructure;
using Xunit;

namespace Wms.Choreography.IntegrationTests;

// Test idempotency saat event diproses ulang.
[Collection(ChoreographyCollection.Name)]
public sealed class IdempotencyTests(ChoreographyFixture fixture)
{
    // Sesuai dengan HandlerType yang dipakai consumer.
    private const string ReceiveGoodsReceiptHandler = "ReceiveGoodsReceipt";
    private const string AllocateWaveHandler = "AllocateWave";
    private const string HandleStockAllocationHandler = "HandleStockAllocationCompleted";

    private static readonly TimeSpan _timeout = TimeSpan.FromSeconds(90);

    [Fact]
    public async Task Grconfirmed_redelivery_creates_stock_exactly_once_via_inbox_and_natural_key()
    {
        await using var world = await ChoreographyWorld.CreateAsync(fixture);

        var grId = (await ChoreographyWorld.SendAsync(world.Inbound, new CreateGoodsReceiptHeaderCommand(
            "PO-IDEMP", Guid.NewGuid(), Guid.NewGuid(), "DOCK-1", [new ExpectedLineInput("SKU-A", 100m, "EA")]))).Value;
        await ChoreographyWorld.SendAsync(world.Inbound, new ScanReceiptLineCommand(
            grId, "SKU-A", 100m, "B1", new DateOnly(2026, 12, 31), LineStatus.Good));
        await ChoreographyWorld.SendAsync(world.Inbound, new CompleteScanCommand(grId));
        (await ChoreographyWorld.SendAsync(world.Inbound, new ConfirmGoodsReceiptCommand(grId))).IsSuccess.Should().BeTrue();

        await world.PumpUntilAsync(() => StockExistsAsync(world), _timeout);

        (await StockCountAsync(world)).Should().Be(1, "efek pertama tepat sekali");
        (await ChoreographyWorld.InboxHandlerCountAsync(world.Inventory, ReceiveGoodsReceiptHandler)).Should().Be(1);

        var envelope = await ChoreographyWorld.EnvelopeAsync(world.Inbound, GRConfirmed.LogicalName);

        // Kirim event yang sama dua kali untuk memastikan redelivery tidak menambah efek.
        await ChoreographyWorld.PublishAsync(world.Inbound, envelope);
        await ChoreographyWorld.PublishAsync(world.Inbound, envelope);

        // Kirim ulang dengan eventId baru untuk memastikan dedup domain tetap bekerja.
        await ChoreographyWorld.PublishAsync(world.Inbound, envelope with { EventId = Guid.NewGuid() });

        // Tunggu sampai replay dengan eventId baru selesai diproses.
        await ChoreographyWorld.WaitUntilAsync(
            async () => await ChoreographyWorld.InboxHandlerCountAsync(world.Inventory, ReceiveGoodsReceiptHandler) >= 2,
            _timeout);

        (await StockCountAsync(world)).Should().Be(1, "kedua lapis dedup — nol Stock ganda");
        (await ChoreographyWorld.InboxHandlerCountAsync(world.Inventory, ReceiveGoodsReceiptHandler))
            .Should().Be(2, "redelivery eventId sama nol row Inbox; hanya replay eventId baru menambah satu");
    }

    [Fact]
    public async Task Outbound_replay_yields_no_duplicate_reservation_or_picking_task_via_natural_key()
    {
        await using var world = await ChoreographyWorld.CreateAsync(fixture);
        var warehouseId = Guid.NewGuid();
        await StockSeeder.SeedAvailableAsync(world.Inventory, "SKU-MILK", 10m, warehouseId);
        var orderId = await OutboundSeeder.SeedNewOrderAsync(world.Outbound, "SKU-MILK", 10m);

        (await ChoreographyWorld.SendAsync(world.Outbound, new CreateWaveCommand([orderId], warehouseId)))
            .IsSuccess.Should().BeTrue();

        await world.PumpUntilAsync(
            () => ChoreographyWorld.QueryAsync<OutboundDbContext, bool>(world.Outbound, context => context.Set<PickingTask>().AnyAsync()),
            _timeout);

        (await ReservationCountAsync(world)).Should().Be(1);
        (await PickingTaskCountAsync(world)).Should().Be(1);

        // Replay eventId baru
        var waveReleased = await ChoreographyWorld.EnvelopeAsync(world.Outbound, WaveReleased.LogicalName);
        var allocationCompleted = await ChoreographyWorld.EnvelopeAsync(world.Inventory, StockAllocationCompleted.LogicalName);
        await ChoreographyWorld.PublishAsync(world.Outbound, waveReleased with { EventId = Guid.NewGuid() });
        await ChoreographyWorld.PublishAsync(world.Inventory, allocationCompleted with { EventId = Guid.NewGuid() });

        await ChoreographyWorld.WaitUntilAsync(
            async () => await ChoreographyWorld.InboxHandlerCountAsync(world.Inventory, AllocateWaveHandler) >= 2
                && await ChoreographyWorld.InboxHandlerCountAsync(world.Outbound, HandleStockAllocationHandler) >= 2,
            _timeout);

        (await ReservationCountAsync(world)).Should().Be(1, "reservasi natural-key (waveId,orderId,sku) tak ganda");
        (await PickingTaskCountAsync(world)).Should().Be(1, "PickingTask natural-key (waveId,reservationId) tak ganda");
    }

    private static Task<bool> StockExistsAsync(ChoreographyWorld world) =>
        ChoreographyWorld.QueryAsync<InventoryDbContext, bool>(world.Inventory, context => context.Set<Stock>().AnyAsync());

    private static Task<int> StockCountAsync(ChoreographyWorld world) =>
        ChoreographyWorld.QueryAsync<InventoryDbContext, int>(world.Inventory, context => context.Set<Stock>().CountAsync());

    private static Task<int> ReservationCountAsync(ChoreographyWorld world) =>
        ChoreographyWorld.QueryAsync<InventoryDbContext, int>(world.Inventory, context => context.Set<StockReservation>().CountAsync());

    private static Task<int> PickingTaskCountAsync(ChoreographyWorld world) =>
        ChoreographyWorld.QueryAsync<OutboundDbContext, int>(world.Outbound, context => context.Set<PickingTask>().CountAsync());
}
