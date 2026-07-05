using Wms.BuildingBlocks.Domain.Results;
using Wms.Inventory.Contracts;
using Wms.Outbound.Application.Abstractions;
using Wms.Outbound.Application.EventTranslation;
using Wms.Outbound.Domain;
using Wms.Outbound.Domain.ValueObjects;
using ContractAllocation = Wms.Inventory.Contracts.Payloads.Allocation;
using InventoryAllocationStatus = Wms.Inventory.Contracts.Enums.AllocationStatus;

namespace Wms.Outbound.Application.Features.HandleStockAllocationCompleted;

// reaksi outcome alokasi. Full/Partial, PickingTask per allocation, ApplyAllocation.
public sealed class HandleStockAllocationCompletedHandler(
    IWaveRepository waveRepository,
    IOutboundOrderRepository orderRepository,
    IPickingTaskRepository pickingTaskRepository,
    IPickAssignmentPolicy pickAssignmentPolicy,
    OutboundEventTranslator translator)
{
    public async Task<Result> HandleAsync(
        StockAllocationCompleted integrationEvent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        var waveId = WaveId.Create(integrationEvent.WaveId);
        if (waveId.IsFailure)
        {
            return waveId;
        }

        var wave = await waveRepository.GetAsync(waveId.Value, cancellationToken);
        if (wave is null)
        {
            return Result.NotFound(new Error("wave.not_found", "Wave alokasi tidak ditemukan."));
        }

        return integrationEvent.Status switch
        {
            InventoryAllocationStatus.FullyAllocated or InventoryAllocationStatus.PartiallyAllocated =>
                await FulfillAsync(wave, integrationEvent, cancellationToken),
            InventoryAllocationStatus.Unfulfilled =>
                await AutoCancelAsync(wave, cancellationToken),
            _ => Result.Invalid(new Error("allocation.status_unknown", $"Status alokasi tak dikenal: {integrationEvent.Status}.")),
        };
    }

    // Full/Partial: membuat PickingTask per allocation dan terjemahkan outcome ke line status
    private async Task<Result> FulfillAsync(Wave wave, StockAllocationCompleted evt, CancellationToken cancellationToken)
    {
        foreach (var allocation in evt.Allocations)
        {
            var created = await CreatePickingTaskAsync(wave, allocation, cancellationToken);
            if (created.IsFailure)
            {
                return created;
            }
        }

        return await ApplyAllocationsAsync(wave, evt, cancellationToken);
    }

    // Satu PickingTask per allocation.
    private async Task<Result> CreatePickingTaskAsync(Wave wave, ContractAllocation allocation, CancellationToken cancellationToken)
    {
        if (await pickingTaskRepository.ExistsForReservationAsync(wave.Id, allocation.ReservationId, cancellationToken))
        {
            return Result.Success();
        }

        var task = PickingTask.Create(
            PickingTaskId.Create(Guid.NewGuid()).Value,
            wave.Id,
            allocation.ReservationId,
            allocation.StockId,
            allocation.LocationId,
            allocation.Sku,
            allocation.Batch,
            allocation.Qty,
            pickAssignmentPolicy.AssignPicker(wave.WarehouseId));
        if (task.IsFailure)
        {
            return task;
        }

        await pickingTaskRepository.AddAsync(task.Value, cancellationToken);

        var attached = wave.AttachPickingTask(task.Value.Id);
        if (attached.IsFailure)
        {
            return attached;
        }

        await translator.AssignPickingTaskAsync(task.Value, wave.WarehouseId, cancellationToken);
        return Result.Success();
    }

    // Terjemahkan allocations/shortfalls ke tiap order
    private async Task<Result> ApplyAllocationsAsync(Wave wave, StockAllocationCompleted evt, CancellationToken cancellationToken)
    {
        var orders = await orderRepository.ListByWaveAsync(wave.Id, cancellationToken);
        var ordersById = orders.ToDictionary(order => order.Id.Value);

        var affectedOrderIds = evt.Allocations.Select(allocation => allocation.OrderId)
            .Concat(evt.Shortfalls.Select(shortfall => shortfall.OrderId))
            .Distinct();

        foreach (var orderId in affectedOrderIds)
        {
            if (!ordersById.TryGetValue(orderId, out var order))
            {
                return Result.NotFound(new Error("wave.order_not_found", "Order alokasi tak ada di wave."));
            }

            var allocationLines = evt.Allocations
                .Where(allocation => allocation.OrderId == orderId)
                .Select(allocation => new AllocationLine(allocation.Sku, allocation.ReservationId, allocation.Qty))
                .ToList();
            var shortfalls = evt.Shortfalls
                .Where(shortfall => shortfall.OrderId == orderId)
                .Select(shortfall => new Shortfall(shortfall.Sku, shortfall.RequestedQty, shortfall.AllocatedQty, shortfall.ShortQty))
                .ToList();

            var applied = order.ApplyAllocation(allocationLines, shortfalls);
            if (applied.IsFailure)
            {
                return applied;
            }
        }

        return Result.Success();
    }

    // Wave Cancelled, tiap order balik backlog.
    private async Task<Result> AutoCancelAsync(Wave wave, CancellationToken cancellationToken)
    {
        var reason = CancelReason.Create("Alokasi nol-terpenuhi (Unfulfilled).");
        if (reason.IsFailure)
        {
            return reason;
        }

        var cancelled = wave.AutoCancel(reason.Value);
        if (cancelled.IsFailure)
        {
            return cancelled;
        }

        var orders = await orderRepository.ListByWaveAsync(wave.Id, cancellationToken);
        foreach (var order in orders)
        {
            var returned = order.ReturnToBacklog("Wave auto-cancel: alokasi nol-terpenuhi.");
            if (returned.IsFailure)
            {
                return returned;
            }

            order.ClearDomainEvents();
        }

        // WaveCancelledRaised tidak jadi integration event (diclear translator).
        await translator.TranslateWaveEventsAsync(wave, cancellationToken);
        return Result.Success();
    }
}
