using Wms.BuildingBlocks.Application.Abstractions;
using Wms.BuildingBlocks.Application.Abstractions.Ports;
using Wms.BuildingBlocks.Domain.Results;
using Wms.Inbound.Contracts;
using Wms.Reporting.Projections;

namespace Wms.Reporting.Consumers;

// Consumer GRConfirmed
public sealed class ReceivingSummaryConsumer(
    IInboxGuard inbox,
    ReceivingSummaryProjection projection,
    IUnitOfWork unitOfWork)
{
    public const string HandlerType = "reporting.receiving_summary";

    public async Task<Result> ConsumeAsync(
        GRConfirmed integrationEvent,
        Guid eventId,
        DateTimeOffset occurredAt,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        if (await inbox.HasProcessedAsync(eventId, HandlerType, cancellationToken))
        {
            return Result.Success();
        }

        await projection.ApplyAsync(integrationEvent, ReportingPeriod.From(occurredAt), cancellationToken);
        await inbox.MarkProcessedAsync(eventId, HandlerType, cancellationToken);

        return await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
