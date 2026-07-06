using Wms.BuildingBlocks.Application.Abstractions;
using Wms.BuildingBlocks.Application.Abstractions.Ports;
using Wms.BuildingBlocks.Domain.Results;
using Wms.Inventory.Contracts;
using Wms.Reporting.Projections;

namespace Wms.Reporting.Consumers;

// Consumer StockRemoved
public sealed class StockRemovedConsumer(
    IInboxGuard inbox,
    DispatchSummaryProjection dispatchProjection,
    StockOnHandProjection stockOnHandProjection,
    IUnitOfWork unitOfWork)
{
    public const string HandlerType = "reporting.dispatch_summary";

    public async Task<Result> ConsumeAsync(
        StockRemoved integrationEvent,
        Guid eventId,
        DateTimeOffset occurredAt,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        if (await inbox.HasProcessedAsync(eventId, HandlerType, cancellationToken))
        {
            return Result.Success();
        }

        await dispatchProjection.ApplyAsync(integrationEvent, ReportingPeriod.From(occurredAt), cancellationToken);
        await stockOnHandProjection.ApplyRemovedAsync(integrationEvent, cancellationToken);
        await inbox.MarkProcessedAsync(eventId, HandlerType, cancellationToken);

        return await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
