using Wms.BuildingBlocks.Domain.Results;
using Wms.Inventory.Application.Abstractions;
using Wms.Inventory.Domain;
using Wms.Inventory.Domain.Enums;
using Wms.Inventory.Domain.ValueObjects;
using Wms.Outbound.Contracts;

namespace Wms.Inventory.Application.Features.FulfillReservation;

public sealed class FulfillReservationHandler(
    IStockRepository stockRepository,
    IStockReservationRepository reservationRepository)
{
    public async Task<Result> HandleAsync(PickingCompleted integrationEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        var reservationId = StockReservationId.Create(integrationEvent.ReservationId);
        if (reservationId.IsFailure)
        {
            return reservationId;
        }

        var reservation = await reservationRepository.GetAsync(reservationId.Value, cancellationToken);
        if (reservation is null)
        {
            return Result.NotFound(new Error("fulfillment.reservation_not_found", "Reservasi untuk picking tidak ditemukan."));
        }

        // Idempotent: replay (eventId berbeda) atas reservasi yang sudah Fulfilled
        if (reservation.Status == ReservationStatus.Fulfilled)
        {
            return Result.Success();
        }

        var stock = await stockRepository.GetAsync(reservation.StockId, cancellationToken);
        if (stock is null)
        {
            return Result.NotFound(new Error("fulfillment.stock_not_found", "Stock sumber picking tidak ditemukan."));
        }

        var staging = LocationId.Create(integrationEvent.StagingLocationId);
        if (staging.IsFailure)
        {
            return staging;
        }

        var fulfilled = reservation.Fulfill(integrationEvent.PickingTaskId);
        if (fulfilled.IsFailure)
        {
            return fulfilled;
        }

        // Split balance: qty terreservasi pindah ke balance Picked @staging (klaim dilepas dari sumber).
        var picked = stock.Pick(
            reservationId.Value, StockId.Create(Guid.NewGuid()).Value, integrationEvent.PickingTaskId, staging.Value);
        if (picked.IsFailure)
        {
            return picked;
        }

        await stockRepository.AddAsync(picked.Value, cancellationToken);

        stock.ClearDomainEvents();
        picked.Value.ClearDomainEvents();
        return Result.Success();
    }
}
