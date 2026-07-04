using Wms.BuildingBlocks.Application.Abstractions;
using Wms.BuildingBlocks.Application.Abstractions.Ports;
using Wms.BuildingBlocks.Domain.Results;
using Wms.Inbound.Contracts;

namespace Wms.Inventory.Application.Features.ReceiveGoodsReceipt;

// Consumer integration event GRConfirmed dari Inbound untuk jalur receiving. Idempotent dua lapis: Inbox guard
// per pasangan eventId dan handlerType, lalu natural key per receivedLine di handler.
public sealed class GRConfirmedConsumer(
    IInboxGuard inbox,
    ReceiveGoodsReceiptHandler handler,
    IUnitOfWork unitOfWork)
{
    public const string HandlerType = "ReceiveGoodsReceipt";

    public async Task<Result> ConsumeAsync(
        GRConfirmed integrationEvent,
        Guid eventId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        // Lapis 1: redelivery broker at least once (eventId sama) untuk handler ini, no-op.
        if (await inbox.HasProcessedAsync(eventId, HandlerType, cancellationToken))
        {
            return Result.Success();
        }

        var handled = await handler.HandleAsync(integrationEvent, cancellationToken);
        if (handled.IsFailure)
        {
            // Tidak MarkProcessed & tidak commit maka event bisa di retry
            return handled;
        }

        await inbox.MarkProcessedAsync(eventId, HandlerType, cancellationToken);

        return await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
