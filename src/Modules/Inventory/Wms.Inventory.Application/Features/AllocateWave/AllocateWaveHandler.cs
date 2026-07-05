using Wms.BuildingBlocks.Application.Abstractions.Ports;
using Wms.BuildingBlocks.Application.Messaging;
using Wms.BuildingBlocks.Domain.Results;
using Wms.Inventory.Application.Abstractions;
using Wms.Inventory.Contracts;
using Wms.Inventory.Contracts.Payloads;
using Wms.Inventory.Domain;
using Wms.Inventory.Domain.ValueObjects;
using Wms.Outbound.Contracts;
using Wms.Outbound.Contracts.Payloads;

namespace Wms.Inventory.Application.Features.AllocateWave;

// Efek alokasi WaveReleased: per line cari Stock Available FEFO, reservasi sebesar yang tersedia,
// lalu rilis satu StockAllocationCompleted ke Outbox dalam transaksi yang sama.
public sealed class AllocateWaveHandler(
    IStockRepository stockRepository,
    IStockReservationRepository reservationRepository,
    IIntegrationEventOutbox outbox)
{
    public async Task<Result> HandleAsync(WaveReleased integrationEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        var allocations = new List<Allocation>();
        var shortfalls = new List<Shortfall>();

        foreach (var line in integrationEvent.Lines)
        {
            var lineResult = await AllocateLineAsync(integrationEvent.WaveId, line, allocations, shortfalls, cancellationToken);
            if (lineResult.IsFailure)
            {
                return lineResult;
            }
        }

        if (allocations.Count == 0 && shortfalls.Count == 0)
        {
            return Result.Success();
        }

        var status = AllocationOutcome.Resolve(allocations.Count, shortfalls.Count);
        var completed = new StockAllocationCompleted(integrationEvent.WaveId, status, allocations, shortfalls);

        await outbox.AddAsync(completed, DeliveryClass.CoreFlow, cancellationToken);
        if (shortfalls.Count > 0)
        {
            await outbox.AddAsync(completed, DeliveryClass.Notification, cancellationToken);
        }

        return Result.Success();
    }

    private async Task<Result> AllocateLineAsync(
        Guid waveId,
        WaveLine line,
        List<Allocation> allocations,
        List<Shortfall> shortfalls,
        CancellationToken cancellationToken)
    {
        var sku = Sku.Create(line.Sku);
        if (sku.IsFailure)
        {
            return sku;
        }

        var quantity = Quantity.Create(line.Qty);
        if (quantity.IsFailure)
        {
            return quantity;
        }

        // Natural key idempotent (waveId, orderId, sku)
        if (await reservationRepository.ExistsForLineAsync(waveId, line.OrderId, sku.Value, cancellationToken))
        {
            return Result.Success();
        }

        var candidates = await stockRepository.GetAllocatableAsync(sku.Value, cancellationToken);
        var plan = FefoAllocator.Allocate(
            line.Qty, [.. candidates.Select(stock => new AllocationCandidate(stock.Id.Value, stock.AvailableQty))]);

        var candidatesById = candidates.ToDictionary(stock => stock.Id.Value);
        foreach (var claim in plan.Claims)
        {
            var stock = candidatesById[claim.StockId];
            var reservation = CreateReservation(waveId, line, stock, claim.Qty);
            if (reservation.IsFailure)
            {
                return reservation;
            }

            await reservationRepository.AddAsync(reservation.Value, cancellationToken);
            allocations.Add(new Allocation(
                line.OrderId,
                line.Sku,
                stock.LocationId.Value,
                stock.Batch.Value,
                claim.Qty,
                stock.Id.Value,
                reservation.Value.Id.Value));
        }

        if (plan.ShortQty > 0m)
        {
            shortfalls.Add(new Shortfall(line.OrderId, line.Sku, line.Qty, line.Qty - plan.ShortQty, plan.ShortQty));
        }

        return Result.Success();

        // Reservasi root dan klaim pada Stock berbagi reservationId dalam satu transaksi
        static Result<StockReservation> CreateReservation(Guid waveId, WaveLine line, Stock stock, decimal qty)
        {
            var quantity = Quantity.Create(qty);
            if (quantity.IsFailure)
            {
                return quantity.ForwardFailure<StockReservation>();
            }

            var reservationId = StockReservationId.Create(Guid.NewGuid()).Value;
            var reservation = StockReservation.Create(
                reservationId, stock.Id, waveId, line.OrderId, stock.Sku, stock.Batch, quantity.Value);
            if (reservation.IsFailure)
            {
                return reservation;
            }

            var reserved = stock.Reserve(reservationId, waveId, line.OrderId, quantity.Value);
            if (reserved.IsFailure)
            {
                return reserved.ForwardFailure<StockReservation>();
            }

            stock.ClearDomainEvents();
            return reservation;
        }
    }
}
