using Wms.BuildingBlocks.Application.Abstractions;
using Wms.BuildingBlocks.Application.Abstractions.Ports;
using Wms.BuildingBlocks.Domain.Results;
using Wms.Inventory.Contracts;

namespace Wms.Outbound.Application.Features.HandleStockAllocationCompleted;

// Consumer StockAllocationCompleted dari Inventory. Idempotent dua lapis: Inbox guard (eventId, handlerType),
// lalu natural key (waveId, reservationId) per PickingTask di handler.
public sealed class StockAllocationCompletedConsumer(
    IInboxGuard inbox,
    HandleStockAllocationCompletedHandler handler,
    IUnitOfWork unitOfWork)
{
    public const string HandlerType = "HandleStockAllocationCompleted";

    public async Task<Result> ConsumeAsync(
        StockAllocationCompleted integrationEvent,
        Guid eventId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        // Lapis 1: redelivery broker at least once (eventId sama) untuk handler ini.
        if (await inbox.HasProcessedAsync(eventId, HandlerType, cancellationToken))
        {
            return Result.Success();
        }

        var handled = await handler.HandleAsync(integrationEvent, cancellationToken);
        if (handled.IsFailure)
        {
            // Tanpa MarkProcessed & commit, event bisa diretry.
            return handled;
        }

        await inbox.MarkProcessedAsync(eventId, HandlerType, cancellationToken);

        return await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
