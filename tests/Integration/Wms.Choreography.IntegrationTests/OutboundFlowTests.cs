using System.Text.Json;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Wms.BuildingBlocks.Application.Abstractions.Ports;
using Wms.BuildingBlocks.Application.Messaging;
using Wms.Choreography.IntegrationTests.TestSupport;
using Wms.Contracts.Abstractions;
using Wms.Inventory.Contracts;
using Wms.Inventory.Domain;
using Wms.Inventory.Domain.Enums;
using Wms.Inventory.Infrastructure;
using Wms.Notifications.Deliveries;
using Wms.Notifications.Persistence;
using Wms.Notifications.Subscriptions;
using Wms.Outbound.Application.Features.CompletePickingTask;
using Wms.Outbound.Application.Features.CreateWave;
using Wms.Outbound.Application.Features.DispatchWave;
using Wms.Outbound.Contracts;
using Wms.Outbound.Domain;
using Wms.Outbound.Domain.Enums;
using Wms.Outbound.Infrastructure;
using Xunit;

namespace Wms.Choreography.IntegrationTests;

// Test end to end proses outbound.
[Collection(ChoreographyCollection.Name)]
public sealed class OutboundFlowTests(ChoreographyFixture fixture)
{
    private const string Sku = "SKU-MILK";

    // Topik notifikasi untuk stock shortfall.
    private const string StockShortfallTopic = "StockShortfall";

    private static readonly TimeSpan _timeout = TimeSpan.FromSeconds(90);

    [Fact]
    public async Task Oo_full_flows_across_process_order_closed_stock_removed()
    {
        await using var world = await ChoreographyWorld.CreateAsync(fixture);
        using var recorder = new ChoreographyRecorder(fixture.RabbitMqConnectionString, world.Exchange);
        await recorder.StartAsync($"q.recorder.{Guid.NewGuid():N}", OutboundEventSubscriptions());

        var warehouseId = Guid.NewGuid();
        await StockSeeder.SeedAvailableAsync(world.Inventory, Sku, 10m, warehouseId);
        var orderId = await OutboundSeeder.SeedNewOrderAsync(world.Outbound, Sku, 10m);

        await DriveAllocateToDispatchAsync(world, warehouseId, orderId, recorder);

        // Order Closed, fully allocated, wave Dispatched.
        (await SingleOrderAsync(world)).Status.Should().Be(OutboundOrderStatus.Closed);
        (await SingleWaveAsync(world)).Status.Should().Be(WaveStatus.Dispatched);

        // Stok −10: StockRemoved membawa qty 10.
        (await RemovedQtyAsync(world)).Should().Be(10m);

        // Urutan event lengkap
        recorder.ObservedLogicalNames().Should().ContainInOrder(
            WaveReleased.LogicalName,
            StockAllocationCompleted.LogicalName,
            PickingTaskAssigned.LogicalName,
            PickingCompleted.LogicalName,
            WaveReady.LogicalName,
            ShipmentDispatched.LogicalName,
            StockRemoved.LogicalName);
    }

    [Fact]
    public async Task Oo_partial_ships_eight_backorders_two_and_alerts_shortfall()
    {
        await using var world = await ChoreographyWorld.CreateAsync(fixture);
        using var recorder = new ChoreographyRecorder(fixture.RabbitMqConnectionString, world.Exchange);
        await recorder.StartAsync($"q.recorder.{Guid.NewGuid():N}", OutboundEventSubscriptions());
        await GivenShortfallSubscriberAsync(world);

        var warehouseId = Guid.NewGuid();
        await StockSeeder.SeedAvailableAsync(world.Inventory, Sku, 8m, warehouseId);
        var orderId = await OutboundSeeder.SeedNewOrderAsync(world.Outbound, Sku, 10m);

        await DriveAllocateToDispatchAsync(world, warehouseId, orderId, recorder);

        // 8 terkirim, 2 backorder, order balik backlog.
        (await RemovedQtyAsync(world)).Should().Be(8m);
        var order = await SingleOrderAsync(world);
        order.Status.Should().Be(OutboundOrderStatus.New);
        order.WaveId.Should().BeNull();

        // Notif alert short terkirim.
        await world.PumpUntilAsync(() => AnyDeliveryAsync(world), _timeout);
        (await AnyDeliveryAsync(world)).Should().BeTrue();
    }

    [Fact]
    public async Task Oo_unfulfilled_auto_cancels_without_orchestrator_and_alerts()
    {
        var saga = new RecordingSagaOrchestrator();
        await using var world = await ChoreographyWorld.CreateAsync(
            fixture, services => services.AddSingleton<ISagaOrchestrator>(saga));
        using var recorder = new ChoreographyRecorder(fixture.RabbitMqConnectionString, world.Exchange);
        await recorder.StartAsync($"q.recorder.{Guid.NewGuid():N}", OutboundEventSubscriptions());
        await GivenShortfallSubscriberAsync(world);

        var warehouseId = Guid.NewGuid();

        // Tidak ada stok, Unfulfilled.
        var orderId = await OutboundSeeder.SeedNewOrderAsync(world.Outbound, Sku, 10m);
        (await ChoreographyWorld.SendAsync(world.Outbound, new CreateWaveCommand([orderId], warehouseId)))
            .IsSuccess.Should().BeTrue();

        // Auto cancel, Wave Cancelled, notif alert.
        await world.PumpUntilAsync(
            async () => (await SingleWaveAsync(world)).Status == WaveStatus.Cancelled && await AnyDeliveryAsync(world),
            _timeout);

        var wave = await SingleWaveAsync(world);
        wave.Status.Should().Be(WaveStatus.Cancelled);

        var order = await SingleOrderAsync(world);
        order.Status.Should().Be(OutboundOrderStatus.New, "order re-waveable pasca auto-cancel");
        order.WaveId.Should().BeNull();

        // Tanpa PickingTask/dispatch
        (await ChoreographyWorld.QueryAsync<OutboundDbContext, bool>(world.Outbound, context => context.Set<PickingTask>().AnyAsync()))
            .Should().BeFalse("Unfulfilled tak pernah membuat PickingTask");
        recorder.ObservedLogicalNames().Should().NotContain(PickingTaskAssigned.LogicalName);
        saga.Started.Should().Be(0, "auto-cancel = reaksi consumer langsung, bukan orchestrated saga");
    }

    private static RailSubscription[] OutboundEventSubscriptions() =>
    [
        new(WaveReleased.LogicalName, DeliveryClass.CoreFlow),
        new(StockAllocationCompleted.LogicalName, DeliveryClass.CoreFlow),
        new(PickingTaskAssigned.LogicalName, DeliveryClass.Notification),
        new(PickingCompleted.LogicalName, DeliveryClass.CoreFlow),
        new(WaveReady.LogicalName, DeliveryClass.Notification),
        new(ShipmentDispatched.LogicalName, DeliveryClass.CoreFlow),
        new(StockRemoved.LogicalName, DeliveryClass.CoreFlow),
    ];

    // CreateWave, alokasi FEFO, complete picking, tunggu split Picked, dispatch, StockRemoved.
    private static async Task DriveAllocateToDispatchAsync(
        ChoreographyWorld world, Guid warehouseId, Guid orderId, ChoreographyRecorder recorder)
    {
        var waveId = (await ChoreographyWorld.SendAsync(world.Outbound, new CreateWaveCommand([orderId], warehouseId))).Value;

        await world.PumpUntilAsync(
            () => ChoreographyWorld.QueryAsync<OutboundDbContext, bool>(world.Outbound, context => context.Set<PickingTask>().AnyAsync()),
            _timeout);

        var task = await ChoreographyWorld.QueryAsync<OutboundDbContext, PickingTask>(
            world.Outbound, context => context.Set<PickingTask>().AsNoTracking().SingleAsync());

        (await ChoreographyWorld.SendAsync(world.Outbound, new CompletePickingTaskCommand(
            task.Id.Value, task.Qty, Guid.NewGuid(), Guid.NewGuid()))).IsSuccess.Should().BeTrue();

        // Inventory FulfillReservation
        await world.PumpUntilAsync(
            () => ChoreographyWorld.QueryAsync<InventoryDbContext, bool>(
                world.Inventory, context => context.Set<Stock>().AnyAsync(stock => stock.Status == StockStatus.Picked)),
            _timeout);

        (await ChoreographyWorld.SendAsync(world.Outbound, new DispatchWaveCommand(waveId))).IsSuccess.Should().BeTrue();

        await world.PumpUntilAsync(
            () => Task.FromResult(recorder.ObservedLogicalNames().Contains(StockRemoved.LogicalName)),
            _timeout);
    }

    private static Task<OutboundOrder> SingleOrderAsync(ChoreographyWorld world) =>
        ChoreographyWorld.QueryAsync<OutboundDbContext, OutboundOrder>(
            world.Outbound, context => context.Set<OutboundOrder>().AsNoTracking().SingleAsync());

    private static Task<Wave> SingleWaveAsync(ChoreographyWorld world) =>
        ChoreographyWorld.QueryAsync<OutboundDbContext, Wave>(
            world.Outbound, context => context.Set<Wave>().AsNoTracking().SingleAsync());

    private static async Task<decimal> RemovedQtyAsync(ChoreographyWorld world)
    {
        var row = (await ChoreographyWorld.OutboxRowsAsync(world.Inventory, StockRemoved.LogicalName)).Single();
        var payload = JsonSerializer.Deserialize<StockRemoved>(row.Payload, MessageEnvelope.PayloadSerializerOptions)!;
        return payload.Lines.Sum(line => line.Qty);
    }

    private static Task<bool> AnyDeliveryAsync(ChoreographyWorld world) =>
        ChoreographyWorld.QueryAsync<NotificationsDbContext, bool>(
            world.Notifications, context => context.Set<NotificationDelivery>().AnyAsync());

    private static async Task GivenShortfallSubscriberAsync(ChoreographyWorld world)
    {
        var role = Guid.NewGuid();
        var supervisor = Guid.NewGuid();
        world.UserDirectory.SetRoleMembers(role, supervisor);
        await NotificationSeeder.SeedSubscriptionAsync(
            world.Notifications, SubscriberType.Role, role, StockShortfallTopic, [Channel.InApp]);
    }
}
