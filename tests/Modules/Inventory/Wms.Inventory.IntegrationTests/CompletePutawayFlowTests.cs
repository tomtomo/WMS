using System.Text.Json;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Wms.BuildingBlocks.Application.Abstractions;
using Wms.BuildingBlocks.Application.Abstractions.Ports;
using Wms.BuildingBlocks.Application.Messaging;
using Wms.BuildingBlocks.Domain.Results;
using Wms.Contracts.Abstractions;
using Wms.Inventory.Application.Abstractions;
using Wms.Inventory.Application.Features.CompletePutaway;
using Wms.Inventory.Contracts;
using Wms.Inventory.Domain;
using Wms.Inventory.Domain.Enums;
using Wms.Inventory.Domain.ValueObjects;
using Wms.Inventory.IntegrationTests.TestSupport;
using Xunit;

namespace Wms.Inventory.IntegrationTests;

// Slice CompletePutaway: PutawayTask Assigned→Completed, Stock OnHand ke Available, emit PutawayCompleted.
[Collection(PostgresCollection.Name)]
public sealed class CompletePutawayFlowTests(PostgresFixture postgres) : IAsyncLifetime
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
    public async Task Complete_moves_task_completed_and_stock_available_with_outbox()
    {
        var task = await ReceiveSingleGoodAsync();
        var destination = Guid.NewGuid();
        var operatorId = Guid.NewGuid();

        var result = await PipelineRunner.SendAsync(
            _provider,
            new CompletePutawayCommand(task.Id.Value, destination, operatorId));

        result.IsSuccess.Should().BeTrue(result.IsFailure ? result.Error.Message : string.Empty);

        var completed = (await PipelineRunner.TasksAsync(_provider)).Single();
        completed.Status.Should().Be(PutawayStatus.Completed);
        completed.ActualDestinationId!.Value.Should().Be(destination);

        var stock = (await PipelineRunner.StocksAsync(_provider)).Single();
        stock.Status.Should().Be(StockStatus.Available);
        stock.LocationId.Value.Should().Be(destination, "balance pindah ke rak tujuan");

        var outbox = await PipelineRunner.OutboxRowsAsync(_provider, PutawayCompleted.LogicalName);
        outbox.Should().ContainSingle();
        outbox[0].DeliveryClass.Should().Be(DeliveryClass.CoreFlow);

        var payload = JsonSerializer.Deserialize<PutawayCompleted>(
            outbox[0].Payload,
            MessageEnvelope.PayloadSerializerOptions)!;
        payload.PutawayTaskId.Should().Be(task.Id.Value);
        payload.StockId.Should().Be(stock.Id.Value);
        payload.Sku.Should().Be("SKU-MILK");
        payload.WarehouseId.Should().Be(GrConfirmedFactory.WarehouseId);
        payload.OperatorId.Should().Be(operatorId);
    }

    [Fact]
    public async Task Complete_emits_putaway_completed_telemetry_with_operator_and_warehouse()
    {
        var task = await ReceiveSingleGoodAsync();
        var operatorId = Guid.NewGuid();

        await PipelineRunner.SendAsync(
            _provider,
            new CompletePutawayCommand(task.Id.Value, Guid.NewGuid(), operatorId));

        var stream = (CapturingEventStreamPublisher)_provider.GetRequiredService<IEventStreamPublisher>();
        var records = stream.On<OperationalTelemetryRecord>(OperationalTelemetryStream.Name);
        records.Should().ContainSingle();
        records[0].EventType.Should().Be(OperationalTelemetryEventType.PutawayCompleted);
        records[0].WarehouseId.Should().Be(GrConfirmedFactory.WarehouseId);
        records[0].OperatorId.Should().Be(operatorId);
        records[0].EntityId.Should().Be(task.Id.Value);
        records[0].Quantity.Should().Be(100m);
    }

    [Fact]
    public async Task Complete_twice_second_is_conflict_and_emits_no_extra_outbox()
    {
        var task = await ReceiveSingleGoodAsync();

        (await PipelineRunner.SendAsync(_provider, new CompletePutawayCommand(task.Id.Value, Guid.NewGuid(), null)))
            .IsSuccess.Should().BeTrue();

        var second = await PipelineRunner.SendAsync(_provider, new CompletePutawayCommand(task.Id.Value, Guid.NewGuid(), null));

        second.IsFailure.Should().BeTrue();
        second.ErrorType.Should().Be(ResultErrorType.Conflict);
        second.Error.Code.Should().Be("putaway_task.not_assigned");
        (await PipelineRunner.OutboxRowsAsync(_provider, PutawayCompleted.LogicalName)).Should()
            .ContainSingle("transisi ilegal tak emit event kedua");
    }

    [Fact]
    public async Task Complete_unknown_task_is_not_found()
    {
        var result = await PipelineRunner.SendAsync(
            _provider,
            new CompletePutawayCommand(Guid.NewGuid(), Guid.NewGuid(), null));

        result.IsFailure.Should().BeTrue();
        result.ErrorType.Should().Be(ResultErrorType.NotFound);
        result.Error.Code.Should().Be("putaway_task.not_found");
    }

    [Fact]
    public async Task Xmin_optimistic_concurrency_conflicts_on_stale_complete()
    {
        var task = await ReceiveSingleGoodAsync();

        using var staleScope = _provider.CreateScope();
        var staleTask = (await staleScope.ServiceProvider.GetRequiredService<IPutawayTaskRepository>()
            .GetAsync(task.Id))!;

        // Operator lain menuntaskan lebih dulu
        (await PipelineRunner.SendAsync(_provider, new CompletePutawayCommand(task.Id.Value, Guid.NewGuid(), null)))
            .IsSuccess.Should().BeTrue();

        staleTask.Complete(LocationId.Create(Guid.NewGuid()).Value).IsSuccess.Should().BeTrue();
        var conflict = await staleScope.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync();

        conflict.IsFailure.Should().BeTrue();
        conflict.ErrorType.Should().Be(ResultErrorType.Conflict);
        conflict.Error.Code.Should().Be("concurrency.conflict");
    }

    private async Task<PutawayTask> ReceiveSingleGoodAsync()
    {
        await PipelineRunner.ConsumeAsync(
            _provider,
            GrConfirmedFactory.With(Guid.NewGuid(), GrConfirmedFactory.Good(qty: 100m)),
            Guid.NewGuid());
        return (await PipelineRunner.TasksAsync(_provider)).Single();
    }
}
