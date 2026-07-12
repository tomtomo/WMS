using Wms.BuildingBlocks.Application.Abstractions;
using Wms.BuildingBlocks.Application.Abstractions.Ports;
using Wms.BuildingBlocks.Application.Messaging;
using Wms.BuildingBlocks.Domain.Results;
using Wms.Inventory.Application.Abstractions;
using Wms.Inventory.Contracts;
using Wms.Inventory.Domain;
using Wms.Inventory.Domain.ValueObjects;

namespace Wms.Inventory.Application.Features.CompletePutaway;

internal sealed class CompletePutawayHandler(
    IPutawayTaskRepository putawayTaskRepository,
    IStockRepository stockRepository,
    IIntegrationEventOutbox outbox,
    IOperationalTelemetryEmitter telemetry,
    TimeProvider timeProvider) : ICommandHandler<CompletePutawayCommand>
{
    public async Task<Result> Handle(CompletePutawayCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var taskId = PutawayTaskId.Create(command.PutawayTaskId);
        if (taskId.IsFailure)
        {
            return taskId;
        }

        var task = await putawayTaskRepository.GetAsync(taskId.Value, cancellationToken);
        if (task is null)
        {
            return Result.NotFound(new Error("putaway_task.not_found", "PutawayTask tidak ditemukan."));
        }

        var stock = await stockRepository.GetAsync(task.StockId, cancellationToken);
        if (stock is null)
        {
            return Result.NotFound(new Error("stock.not_found", "Stock terkait PutawayTask tidak ditemukan."));
        }

        var destination = LocationId.Create(command.ActualDestinationId);
        if (destination.IsFailure)
        {
            return destination;
        }

        // Task Assigned ke Completed dulu
        var completed = task.Complete(destination.Value);
        if (completed.IsFailure)
        {
            return completed;
        }

        // Stock OnHand ke Available, lokasi pindah ke rak
        var putAway = stock.PutAway(destination.Value);
        if (putAway.IsFailure)
        {
            return putAway;
        }

        await outbox.AddAsync(
            new PutawayCompleted(task.Id.Value, stock.Id.Value, stock.Sku.Value, stock.WarehouseId, command.OperatorId),
            PutawayCompleted.DeliveryClass,
            cancellationToken);

        task.ClearDomainEvents();
        stock.ClearDomainEvents();

        await telemetry.EmitAsync(
            new OperationalTelemetryRecord(
                timeProvider.GetUtcNow(),
                stock.WarehouseId,
                command.OperatorId,
                OperationalTelemetryEventType.PutawayCompleted,
                task.Id.Value,
                stock.Qty),
            cancellationToken);

        return Result.Success();
    }
}
