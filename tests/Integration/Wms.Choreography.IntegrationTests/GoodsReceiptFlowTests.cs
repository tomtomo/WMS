using System.Text.Json;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Wms.BuildingBlocks.Application.Messaging;
using Wms.Choreography.IntegrationTests.TestSupport;
using Wms.Contracts.Abstractions;
using Wms.Inbound.Application.Features.CompleteScan;
using Wms.Inbound.Application.Features.ConfirmGoodsReceipt;
using Wms.Inbound.Application.Features.CreateGoodsReceiptHeader;
using Wms.Inbound.Application.Features.ScanReceiptLine;
using Wms.Inbound.Contracts;
using Wms.Inbound.Contracts.Enums;
using Wms.Inbound.Contracts.Payloads;
using Wms.Inventory.Application.Features.CompletePutaway;
using Wms.Inventory.Contracts;
using Wms.Inventory.Domain;
using Wms.Inventory.Domain.Enums;
using Wms.Inventory.Infrastructure;
using Wms.Reporting.Persistence;
using Wms.Reporting.ReadModels;
using Xunit;

namespace Wms.Choreography.IntegrationTests;

// Test alur Goods Receipt antar modul.
[Collection(ChoreographyCollection.Name)]
public sealed class GoodsReceiptFlowTests(ChoreographyFixture fixture)
{
    private static readonly TimeSpan _timeout = TimeSpan.FromSeconds(60);

    [Fact]
    public async Task Gr_ok_flows_across_process_with_event_order_and_final_stock_available()
    {
        await using var world = await ChoreographyWorld.CreateAsync(fixture);
        using var recorder = new ChoreographyRecorder(fixture.RabbitMqConnectionString, world.Exchange);
        await recorder.StartAsync(
            $"q.recorder.{Guid.NewGuid():N}",
            [
                new RailSubscription(GoodsReceiptPendingReview.LogicalName, DeliveryClass.Notification),
                new RailSubscription(GRConfirmed.LogicalName, DeliveryClass.CoreFlow),
                new RailSubscription(PutawayTaskAssigned.LogicalName, DeliveryClass.Notification),
                new RailSubscription(PutawayCompleted.LogicalName, DeliveryClass.CoreFlow),
            ]);

        var supplierId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        var expiry = new DateOnly(2026, 12, 31);

        var grId = (await ChoreographyWorld.SendAsync(world.Inbound, new CreateGoodsReceiptHeaderCommand(
            "PO-2026-001", supplierId, warehouseId, "DOCK-1", [new ExpectedLineInput("SKU-A", 100m, "EA")]))).Value;
        await ChoreographyWorld.SendAsync(world.Inbound, new ScanReceiptLineCommand(
            grId, "SKU-A", 100m, "B1", expiry, Wms.Inbound.Domain.Enums.LineStatus.Good));
        await ChoreographyWorld.SendAsync(world.Inbound, new CompleteScanCommand(grId));
        (await ChoreographyWorld.SendAsync(world.Inbound, new ConfirmGoodsReceiptCommand(grId)))
            .IsSuccess.Should().BeTrue();

        // Inventory consume GRConfirmed
        await world.PumpUntilAsync(
            () => ChoreographyWorld.QueryAsync<InventoryDbContext, bool>(
                world.Inventory, context => context.Set<PutawayTask>().AnyAsync()),
            _timeout);

        var putawayTaskId = await ChoreographyWorld.QueryAsync<InventoryDbContext, Guid>(
            world.Inventory, async context => (await context.Set<PutawayTask>().AsNoTracking().SingleAsync()).Id.Value);

        // Selesaikan putaway
        (await ChoreographyWorld.SendAsync(world.Inventory, new CompletePutawayCommand(
            putawayTaskId, FakeReceivingPolicy.PutawayDestinationId, Guid.NewGuid()))).IsSuccess.Should().BeTrue();

        await world.PumpUntilAsync(
            async () => await ChoreographyWorld.QueryAsync<InventoryDbContext, bool>(
                world.Inventory, context => context.Set<Stock>().AnyAsync(stock => stock.Status == StockStatus.Available))
                && recorder.ObservedLogicalNames().Contains(PutawayCompleted.LogicalName),
            _timeout);

        // Stok akhir: 100 Available.
        var finalStock = await ChoreographyWorld.QueryAsync<InventoryDbContext, Stock>(
            world.Inventory, context => context.Set<Stock>().AsNoTracking().SingleAsync());
        finalStock.Status.Should().Be(StockStatus.Available);
        finalStock.Qty.Should().Be(100m);

        // Urutan event lintas proses
        recorder.ObservedLogicalNames().Should().ContainInOrder(
            GoodsReceiptPendingReview.LogicalName,
            GRConfirmed.LogicalName,
            PutawayTaskAssigned.LogicalName,
            PutawayCompleted.LogicalName);

        // Payload GRConfirmed
        var grConfirmedRow = (await ChoreographyWorld.OutboxRowsAsync(world.Inbound, GRConfirmed.LogicalName)).Single();
        var payload = JsonSerializer.Deserialize<GRConfirmed>(grConfirmedRow.Payload, MessageEnvelope.PayloadSerializerOptions)!;
        payload.ReceivedLines.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new ReceivedLine("SKU-A", 100m, "B1", expiry, ReceivedLineStatus.Good));
        payload.RejectedLines.Should().BeEmpty();

        // Reporting projeksi StockOnHandView terupdate
        var projectedQty = await ChoreographyWorld.QueryAsync<ReportingDbContext, decimal>(
            world.Reporting, context => context.Set<StockOnHandView>().Where(view => view.Sku == "SKU-A").SumAsync(view => view.QtyOnHand));
        projectedQty.Should().Be(100m);
    }
}
