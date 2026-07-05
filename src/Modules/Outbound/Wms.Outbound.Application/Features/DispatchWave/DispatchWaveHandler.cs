using Wms.BuildingBlocks.Application.Messaging;
using Wms.BuildingBlocks.Domain.Results;
using Wms.Outbound.Application.Abstractions;
using Wms.Outbound.Application.EventTranslation;
using Wms.Outbound.Domain;
using Wms.Outbound.Domain.Enums;

namespace Wms.Outbound.Application.Features.DispatchWave;

// Wave Ready jadi Dispatched, emit ShipmentDispatched. Penutupan order: terpenuhi semua jadi
// Closed, backorder outstanding balik ke backlog. Commit oleh TransactionBehavior.
internal sealed class DispatchWaveHandler(
    IWaveRepository waveRepository,
    IOutboundOrderRepository orderRepository,
    OutboundEventTranslator translator) : ICommandHandler<DispatchWaveCommand>
{
    public async Task<Result> Handle(DispatchWaveCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var waveId = WaveId.Create(command.WaveId);
        if (waveId.IsFailure)
        {
            return waveId;
        }

        var wave = await waveRepository.GetAsync(waveId.Value, cancellationToken);
        if (wave is null)
        {
            return Result.NotFound(new Error("wave.not_found", "Wave tidak ditemukan."));
        }

        var dispatched = wave.Dispatch();
        if (dispatched.IsFailure)
        {
            return dispatched;
        }

        var orders = await orderRepository.ListByWaveAsync(waveId.Value, cancellationToken);
        foreach (var order in orders)
        {
            var fullyFulfilled = order.OrderLines.All(line => line.AllocationStatus == AllocationStatus.Allocated);
            var closure = fullyFulfilled
                ? order.Close()
                : order.ReturnToBacklog("Backorder outstanding — sisa demand rewaveable.");
            if (closure.IsFailure)
            {
                return closure;
            }

            // Order closed/returned — event internal, tidak jadi integration event.
            order.ClearDomainEvents();
        }

        await translator.TranslateWaveEventsAsync(wave, cancellationToken);
        return Result.Success();
    }
}
