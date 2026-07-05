using System.Diagnostics;
using Wms.BuildingBlocks.Application.Abstractions.Ports;
using Wms.Outbound.Contracts;
using Wms.Outbound.Contracts.Payloads;
using Wms.Outbound.Domain;
using Wms.Outbound.Domain.Events;

namespace Wms.Outbound.Application.EventTranslation;

// Terjemahkan perubahan state Outbound ke integration event Contracts, ditulis ke Outbox dalam transaksi
// bisnis yang sama. Sebagian event butuh enrichment (warehouseId dari Wave)
// yang tidak ada di domain event, jadi handler memanggil method eksplisit
public sealed class OutboundEventTranslator(IIntegrationEventOutbox outbox)
{
    // WaveReleased ke Inventory: satu WaveLine per demand line.
    public Task ReleaseWaveAsync(Wave wave, IReadOnlyList<WaveLine> lines, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(wave);
        ArgumentNullException.ThrowIfNull(lines);

        return outbox.AddAsync(new WaveReleased(wave.Id.Value, lines), WaveReleased.DeliveryClass, cancellationToken);
    }

    // PickingTaskAssigned ke Notifications. warehouseId dienrich dari Wave.
    // Domain event PickingTaskAssignedRaised diclear, digantikan integration event ini.
    public Task AssignPickingTaskAsync(PickingTask task, Guid warehouseId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(task);
        task.ClearDomainEvents();

        return outbox.AddAsync(
            new PickingTaskAssigned(task.Id.Value, task.WaveId.Value, task.StockId, task.Sku, task.AssignedTo, warehouseId),
            PickingTaskAssigned.DeliveryClass,
            cancellationToken);
    }

    // PickingCompleted ke Inventory/Reporting — membawa operatorId/reservationId/stockId/stagingLocationId.
    public Task CompletePickingAsync(PickingTask task, Guid? operatorId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(task);

        return outbox.AddAsync(
            new PickingCompleted(
                task.WaveId.Value,
                task.Id.Value,
                task.StockId,
                task.ReservationId,
                task.Sku,
                task.Batch,
                task.Qty,
                task.StagingLocationId!.Value,
                operatorId),
            PickingCompleted.DeliveryClass,
            cancellationToken);
    }

    // Terjemahkan domain event Wave ke integration event (warehouseId di-enrich dari Wave), lalu clear.
    // WaveReady dan ShipmentDispatched.
    public async Task TranslateWaveEventsAsync(Wave wave, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(wave);

        foreach (var domainEvent in wave.DomainEvents)
        {
            switch (domainEvent)
            {
                case WaveReadyRaised:
                    await outbox.AddAsync(
                        new WaveReady(wave.Id.Value, wave.WarehouseId), WaveReady.DeliveryClass, cancellationToken);
                    break;

                case WaveDispatchedRaised:
                    await outbox.AddAsync(
                        new ShipmentDispatched(wave.Id.Value), ShipmentDispatched.DeliveryClass, cancellationToken);
                    break;

                case WaveCancelledRaised:
                    // Auto-cancel
                    break;

                default:
                    throw new UnreachableException($"Wave domain event tanpa aturan translate: {domainEvent.GetType().Name}");
            }
        }

        wave.ClearDomainEvents();
    }
}
