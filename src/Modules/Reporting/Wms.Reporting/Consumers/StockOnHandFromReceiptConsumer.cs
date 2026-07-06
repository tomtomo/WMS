using Wms.BuildingBlocks.Application.Abstractions;
using Wms.BuildingBlocks.Application.Abstractions.Ports;
using Wms.BuildingBlocks.Domain.Results;
using Wms.Inbound.Contracts;
using Wms.Reporting.Projections;

namespace Wms.Reporting.Consumers;

// Consumer GRConfirmed
public sealed class StockOnHandFromReceiptConsumer(
    IInboxGuard inbox,
    StockOnHandProjection projection,
    IUnitOfWork unitOfWork)
{
    public const string HandlerType = "reporting.stock_on_hand_receipt";

    public async Task<Result> ConsumeAsync(
        GRConfirmed integrationEvent,
        Guid eventId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        if (await inbox.HasProcessedAsync(eventId, HandlerType, cancellationToken))
        {
            return Result.Success();
        }

        await projection.ApplyReceivedAsync(integrationEvent, cancellationToken);
        await inbox.MarkProcessedAsync(eventId, HandlerType, cancellationToken);

        return await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
