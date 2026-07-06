using Wms.BuildingBlocks.Application.Abstractions;
using Wms.BuildingBlocks.Application.Abstractions.Ports;
using Wms.BuildingBlocks.Domain.Results;
using Wms.Outbound.Contracts;
using Wms.Reporting.Projections;

namespace Wms.Reporting.Consumers;

// Consumer PickingCompleted
public sealed class PickingCompletedConsumer(
    IInboxGuard inbox,
    OperatorActivityProjection projection,
    IUnitOfWork unitOfWork)
{
    public const string HandlerType = "reporting.operator_activity_pick";

    public async Task<Result> ConsumeAsync(
        PickingCompleted integrationEvent,
        Guid eventId,
        DateTimeOffset occurredAt,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        if (await inbox.HasProcessedAsync(eventId, HandlerType, cancellationToken))
        {
            return Result.Success();
        }

        await projection.ApplyPickAsync(integrationEvent, ReportingPeriod.From(occurredAt), cancellationToken);
        await inbox.MarkProcessedAsync(eventId, HandlerType, cancellationToken);

        return await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
