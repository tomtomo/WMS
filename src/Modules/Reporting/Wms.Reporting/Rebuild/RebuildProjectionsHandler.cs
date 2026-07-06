using Microsoft.EntityFrameworkCore;
using Wms.BuildingBlocks.Domain.Results;
using Wms.Inbound.Contracts;
using Wms.Inventory.Contracts;
using Wms.Outbound.Contracts;
using Wms.Reporting.Abstractions;
using Wms.Reporting.Persistence;
using Wms.Reporting.Projections;

namespace Wms.Reporting.Rebuild;

// Rebuild
public sealed class RebuildProjectionsHandler(
    ReportingDbContext dbContext,
    IProjectionStore store,
    ReceivingSummaryProjection receivingSummary,
    StockOnHandProjection stockOnHand,
    DispatchSummaryProjection dispatchSummary,
    OperatorActivityProjection operatorActivity)
{
    public async Task<Result> HandleAsync(RebuildProjectionsCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Transaksi eksplisit
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        await store.TruncateAllAsync(cancellationToken);

        foreach (var replay in command.Events)
        {
            await ApplyAsync(replay, cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Result.Success();
    }

    private async Task ApplyAsync(ReplayEvent replay, CancellationToken cancellationToken)
    {
        var period = ReportingPeriod.From(replay.OccurredAt);
        switch (replay.Event)
        {
            case GRConfirmed grConfirmed:
                await receivingSummary.ApplyAsync(grConfirmed, period, cancellationToken);
                await stockOnHand.ApplyReceivedAsync(grConfirmed, cancellationToken);
                break;
            case StockRemoved stockRemoved:
                await dispatchSummary.ApplyAsync(stockRemoved, period, cancellationToken);
                await stockOnHand.ApplyRemovedAsync(stockRemoved, cancellationToken);
                break;
            case PutawayCompleted putawayCompleted:
                await operatorActivity.ApplyPutawayAsync(putawayCompleted, period, cancellationToken);
                break;
            case PickingCompleted pickingCompleted:
                await operatorActivity.ApplyPickAsync(pickingCompleted, period, cancellationToken);
                break;
            default:
                break;
        }
    }
}
