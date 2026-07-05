using Wms.BuildingBlocks.Application.Abstractions;
using Wms.BuildingBlocks.Application.Abstractions.Ports;
using Wms.BuildingBlocks.Domain.Results;
using Wms.Outbound.Contracts;

namespace Wms.Inventory.Application.Features.RemovePickedStock;

// Consumer ShipmentDispatched dari Outbound. Idempotent dua lapis: Inbox guard (eventId, handlerType), lalu
// tidak adanya Stock Picked untuk wave.
public sealed class ShipmentDispatchedConsumer(
    IInboxGuard inbox,
    RemovePickedStockHandler handler,
    IUnitOfWork unitOfWork)
{
    public const string HandlerType = "RemovePickedStock";

    public async Task<Result> ConsumeAsync(
        ShipmentDispatched integrationEvent,
        Guid eventId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        if (await inbox.HasProcessedAsync(eventId, HandlerType, cancellationToken))
        {
            return Result.Success();
        }

        var handled = await handler.HandleAsync(integrationEvent, cancellationToken);
        if (handled.IsFailure)
        {
            return handled;
        }

        await inbox.MarkProcessedAsync(eventId, HandlerType, cancellationToken);

        return await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
