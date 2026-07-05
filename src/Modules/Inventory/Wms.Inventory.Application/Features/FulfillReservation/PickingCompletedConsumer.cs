using Wms.BuildingBlocks.Application.Abstractions;
using Wms.BuildingBlocks.Application.Abstractions.Ports;
using Wms.BuildingBlocks.Domain.Results;
using Wms.Outbound.Contracts;

namespace Wms.Inventory.Application.Features.FulfillReservation;

// Consumer PickingCompleted dari Outbound. Idempotent dua lapis: Inbox guard (eventId, handlerType), lalu status
// reservasi di handler
public sealed class PickingCompletedConsumer(
    IInboxGuard inbox,
    FulfillReservationHandler handler,
    IUnitOfWork unitOfWork)
{
    public const string HandlerType = "FulfillReservation";

    public async Task<Result> ConsumeAsync(
        PickingCompleted integrationEvent,
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
