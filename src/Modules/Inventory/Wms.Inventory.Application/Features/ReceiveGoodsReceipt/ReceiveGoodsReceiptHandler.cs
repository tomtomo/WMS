using Wms.BuildingBlocks.Application.Abstractions.Ports;
using Wms.BuildingBlocks.Domain.Results;
using Wms.Inbound.Contracts;
using Wms.Inbound.Contracts.Enums;
using Wms.Inbound.Contracts.Payloads;
using Wms.Inventory.Application.Abstractions;
using Wms.Inventory.Contracts;
using Wms.Inventory.Domain;
using Wms.Inventory.Domain.ValueObjects;

namespace Wms.Inventory.Application.Features.ReceiveGoodsReceipt;

// Efek receiving tiap receivedLine GRConfirmed. Line Good menjadi Stock OnHand disertai PutawayTask dan
// integration event PutawayTaskAssigned. line QcHold menjadi Stock Quarantine.
public sealed class ReceiveGoodsReceiptHandler(
    IStockRepository stockRepository,
    IPutawayTaskRepository putawayTaskRepository,
    IReceivingPolicy receivingPolicy,
    IIntegrationEventOutbox outbox)
{
    public async Task<Result> HandleAsync(GRConfirmed integrationEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        for (var line = 0; line < integrationEvent.ReceivedLines.Count; line++)
        {
            var result = await ReceiveLineAsync(integrationEvent, line, integrationEvent.ReceivedLines[line], cancellationToken);
            if (result.IsFailure)
            {
                return result;
            }
        }

        return Result.Success();
    }

    // Batch dan Expiry wajib
    private static Result<LineInputs> BuildInputs(ReceivedLine received)
    {
        var sku = Sku.Create(received.Sku);
        if (sku.IsFailure)
        {
            return sku.ForwardFailure<LineInputs>();
        }

        if (received.Batch is null)
        {
            return Result.Invalid<LineInputs>(new Error("receiving.batch_required", "Batch wajib untuk stock traceable."));
        }

        var batch = Batch.Create(received.Batch);
        if (batch.IsFailure)
        {
            return batch.ForwardFailure<LineInputs>();
        }

        if (received.Expiry is null)
        {
            return Result.Invalid<LineInputs>(new Error("receiving.expiry_required", "Expiry wajib untuk stock FEFO."));
        }

        var expiry = Expiry.Create(received.Expiry.Value);
        if (expiry.IsFailure)
        {
            return expiry.ForwardFailure<LineInputs>();
        }

        var qty = Quantity.Create(received.Qty);
        if (qty.IsFailure)
        {
            return qty.ForwardFailure<LineInputs>();
        }

        return Result.Success(new LineInputs(sku.Value, batch.Value, expiry.Value, qty.Value));
    }

    private async Task<Result> ReceiveLineAsync(GRConfirmed evt, int line, ReceivedLine received, CancellationToken cancellationToken)
    {
        if (await stockRepository.ExistsForReceiptLineAsync(evt.GrId, line, cancellationToken))
        {
            return Result.Success();
        }

        var inputs = BuildInputs(received);
        if (inputs.IsFailure)
        {
            return inputs;
        }

        return received.Status switch
        {
            ReceivedLineStatus.Good => await PutOnHandAsync(evt, line, inputs.Value, cancellationToken),
            ReceivedLineStatus.QcHold => await QuarantineAsync(evt, line, inputs.Value, cancellationToken),
            _ => Result.Invalid(new Error("receiving.line_status_unknown", $"ReceivedLineStatus tak dikenal: {received.Status}.")),
        };
    }

    // Line Good: OnHand di receiving area, PutawayTask, lalu integration event PutawayTaskAssigned ke Outbox.
    private async Task<Result> PutOnHandAsync(GRConfirmed evt, int line, LineInputs inputs, CancellationToken cancellationToken)
    {
        var receivingLocation = receivingPolicy.ReceivingLocation(evt.WarehouseId);

        var stock = Stock.CreateOnHand(
            StockId.Create(Guid.NewGuid()).Value,
            inputs.Sku,
            receivingLocation,
            inputs.Batch,
            inputs.Expiry,
            inputs.Qty,
            evt.GrId,
            line,
            evt.WarehouseId);
        if (stock.IsFailure)
        {
            return stock;
        }

        await stockRepository.AddAsync(stock.Value, cancellationToken);

        var suggestion = receivingPolicy.SuggestPutaway(inputs.Sku, evt.WarehouseId);
        var task = PutawayTask.Create(
            PutawayTaskId.Create(Guid.NewGuid()).Value,
            stock.Value.Id,
            receivingLocation,
            suggestion.Destination,
            suggestion.AssignedTo);
        if (task.IsFailure)
        {
            return task;
        }

        await putawayTaskRepository.AddAsync(task.Value, cancellationToken);

        await outbox.AddAsync(
            new PutawayTaskAssigned(task.Value.Id.Value, stock.Value.Id.Value, inputs.Sku.Value, task.Value.AssignedTo, evt.WarehouseId),
            PutawayTaskAssigned.DeliveryClass,
            cancellationToken);

        stock.Value.ClearDomainEvents();
        task.Value.ClearDomainEvents();
        return Result.Success();
    }

    // Line QcHold: Quarantine saja — tidak allocatable, tidak generate PutawayTask, tidak emit event.
    private async Task<Result> QuarantineAsync(GRConfirmed evt, int line, LineInputs inputs, CancellationToken cancellationToken)
    {
        var stock = Stock.CreateQuarantine(
            StockId.Create(Guid.NewGuid()).Value,
            inputs.Sku,
            receivingPolicy.QuarantineLocation(evt.WarehouseId),
            inputs.Batch,
            inputs.Expiry,
            inputs.Qty,
            evt.GrId,
            line,
            evt.WarehouseId);
        if (stock.IsFailure)
        {
            return stock;
        }

        await stockRepository.AddAsync(stock.Value, cancellationToken);
        stock.Value.ClearDomainEvents();
        return Result.Success();
    }

    // VO tervalidasi satu receivedLine.
    private sealed record LineInputs(Sku Sku, Batch Batch, Expiry Expiry, Quantity Qty);
}
