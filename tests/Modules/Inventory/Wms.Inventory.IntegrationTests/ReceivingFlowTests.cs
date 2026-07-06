using System.Text.Json;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Wms.BuildingBlocks.Application.Messaging;
using Wms.Inbound.Contracts.Enums;
using Wms.Inbound.Contracts.Payloads;
using Wms.Inventory.Contracts;
using Wms.Inventory.Domain;
using Wms.Inventory.Domain.Enums;
using Wms.Inventory.IntegrationTests.TestSupport;
using Xunit;

namespace Wms.Inventory.IntegrationTests;

// Consumer GRConfirmed: state (Stock/PutawayTask), Outbox, Inbox satu transaksi, idempotent dua lapis.
[Collection(PostgresCollection.Name)]
public sealed class ReceivingFlowTests(PostgresFixture postgres) : IAsyncLifetime
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
    public async Task Good_line_creates_on_hand_stock_putaway_task_and_outbox_assigned()
    {
        var grId = Guid.NewGuid();

        var result = await PipelineRunner.ConsumeAsync(
            _provider,
            GrConfirmedFactory.With(grId, GrConfirmedFactory.Good(qty: 100m)),
            Guid.NewGuid());

        result.IsSuccess.Should().BeTrue(result.IsFailure ? result.Error.Message : string.Empty);

        var stock = await SingleStockAsync();
        stock.Status.Should().Be(StockStatus.OnHand);
        stock.Sku.Value.Should().Be("SKU-MILK");
        stock.Qty.Should().Be(100m);
        stock.WarehouseId.Should().Be(GrConfirmedFactory.WarehouseId);
        stock.SourceGrId.Should().Be(grId);
        stock.Line.Should().Be(0);
        stock.LocationId.Value.Should().Be(FakeReceivingPolicy.ReceivingLocationId);

        var task = await SingleTaskAsync();
        task.Status.Should().Be(PutawayStatus.Assigned);
        task.StockId.Should().Be(stock.Id);
        task.SuggestedDestinationId.Value.Should().Be(FakeReceivingPolicy.PutawayDestinationId);
        task.AssignedTo.Should().Be(FakeReceivingPolicy.PutawayAssignee);

        var outbox = await PipelineRunner.OutboxRowsAsync(_provider, PutawayTaskAssigned.LogicalName);
        outbox.Should().ContainSingle();
        outbox[0].DeliveryClass.Should().Be(DeliveryClass.Notification);

        var payload = JsonSerializer.Deserialize<PutawayTaskAssigned>(
            outbox[0].Payload,
            MessageEnvelope.PayloadSerializerOptions)!;
        payload.StockId.Should().Be(stock.Id.Value);
        payload.PutawayTaskId.Should().Be(task.Id.Value);
        payload.Sku.Should().Be("SKU-MILK");
        payload.WarehouseId.Should().Be(GrConfirmedFactory.WarehouseId);
        payload.AssignedTo.Should().Be(FakeReceivingPolicy.PutawayAssignee);
    }

    [Fact]
    public async Task QcHold_line_creates_quarantine_stock_without_task_or_outbox()
    {
        var grId = Guid.NewGuid();

        var result = await PipelineRunner.ConsumeAsync(
            _provider,
            GrConfirmedFactory.With(grId, GrConfirmedFactory.QcHold(qty: 20m)),
            Guid.NewGuid());

        result.IsSuccess.Should().BeTrue(result.IsFailure ? result.Error.Message : string.Empty);

        var stock = await SingleStockAsync();
        stock.Status.Should().Be(StockStatus.Quarantine);
        stock.Qty.Should().Be(20m);
        stock.LocationId.Value.Should().Be(FakeReceivingPolicy.QuarantineLocationId);

        (await AllTasksAsync()).Should().BeEmpty("QcHold tidak generate PutawayTask");
        (await PipelineRunner.OutboxRowsAsync(_provider)).Should().BeEmpty("QcHold tidak emit integration event");
    }

    [Fact]
    public async Task Mixed_lines_create_stock_per_line_only_good_generates_task_and_outbox()
    {
        var grId = Guid.NewGuid();

        var result = await PipelineRunner.ConsumeAsync(
            _provider,
            GrConfirmedFactory.With(grId, GrConfirmedFactory.Good(qty: 100m), GrConfirmedFactory.QcHold(qty: 20m)),
            Guid.NewGuid());

        result.IsSuccess.Should().BeTrue(result.IsFailure ? result.Error.Message : string.Empty);

        var stocks = await AllStocksAsync();
        stocks.Should().HaveCount(2);
        stocks.Should().ContainSingle(stock => stock.Status == StockStatus.OnHand && stock.Line == 0);
        stocks.Should().ContainSingle(stock => stock.Status == StockStatus.Quarantine && stock.Line == 1);

        (await AllTasksAsync()).Should().ContainSingle("hanya line Good generate PutawayTask");
        (await PipelineRunner.OutboxRowsAsync(_provider)).Should()
            .ContainSingle(row => row.LogicalName == PutawayTaskAssigned.LogicalName);
    }

    [Fact]
    public async Task Replay_same_event_id_is_no_op_via_inbox_guard()
    {
        var grId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var integrationEvent = GrConfirmedFactory.With(grId, GrConfirmedFactory.Good(), GrConfirmedFactory.QcHold());

        (await PipelineRunner.ConsumeAsync(_provider, integrationEvent, eventId)).IsSuccess.Should().BeTrue();
        (await PipelineRunner.ConsumeAsync(_provider, integrationEvent, eventId)).IsSuccess.Should().BeTrue();

        (await AllStocksAsync()).Should().HaveCount(2, "redelivery eventId sama = no-op (Inbox guard)");
        (await AllTasksAsync()).Should().ContainSingle();
        (await PipelineRunner.OutboxRowsAsync(_provider)).Should().ContainSingle();
    }

    [Fact]
    public async Task Replay_different_event_id_is_no_op_via_natural_key()
    {
        var grId = Guid.NewGuid();
        var integrationEvent = GrConfirmedFactory.With(grId, GrConfirmedFactory.Good(), GrConfirmedFactory.QcHold());

        (await PipelineRunner.ConsumeAsync(_provider, integrationEvent, Guid.NewGuid())).IsSuccess.Should().BeTrue();
        (await PipelineRunner.ConsumeAsync(_provider, integrationEvent, Guid.NewGuid())).IsSuccess.Should().BeTrue();

        (await AllStocksAsync()).Should().HaveCount(2, "natural key (sourceGrId, line) cegah stok hantu meski eventId beda");
        (await AllTasksAsync()).Should().ContainSingle();
        (await PipelineRunner.OutboxRowsAsync(_provider)).Should().ContainSingle();
    }

    [Fact]
    public async Task Malformed_line_aborts_whole_event_no_stock_no_outbox()
    {
        var grId = Guid.NewGuid();
        var integrationEvent = GrConfirmedFactory.With(
            grId,
            GrConfirmedFactory.Good(qty: 100m),
            new ReceivedLine("SKU-MILK", 50m, null, new DateOnly(2026, 12, 31), ReceivedLineStatus.Good));

        var result = await PipelineRunner.ConsumeAsync(_provider, integrationEvent, Guid.NewGuid());

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("receiving.batch_required");
        (await AllStocksAsync()).Should().BeEmpty("line 0 valid pun tidak tercommit");
        (await PipelineRunner.OutboxRowsAsync(_provider)).Should().BeEmpty();
    }

    private async Task<Stock> SingleStockAsync()
    {
        var stocks = await AllStocksAsync();
        stocks.Should().ContainSingle();
        return stocks[0];
    }

    private async Task<PutawayTask> SingleTaskAsync()
    {
        var tasks = await AllTasksAsync();
        tasks.Should().ContainSingle();
        return tasks[0];
    }

    private Task<List<Stock>> AllStocksAsync() =>
        PipelineRunner.QueryDbAsync(_provider, context => context.Set<Stock>().AsNoTracking().ToListAsync());

    private Task<List<PutawayTask>> AllTasksAsync() =>
        PipelineRunner.QueryDbAsync(_provider, context => context.Set<PutawayTask>().AsNoTracking().ToListAsync());
}
