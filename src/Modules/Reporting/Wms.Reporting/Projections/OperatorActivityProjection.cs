using Wms.Inventory.Contracts;
using Wms.Outbound.Contracts;
using Wms.Reporting.Abstractions;
using Wms.Reporting.ReadModels;

namespace Wms.Reporting.Projections;

// Projection produktivitas operator per (operator, period)
public sealed class OperatorActivityProjection(IProjectionStore store)
{
    public Task ApplyPutawayAsync(PutawayCompleted integrationEvent, DateOnly period, CancellationToken cancellationToken = default)
    {
        if (integrationEvent.OperatorId is not { } operatorId)
        {
            return Task.CompletedTask;
        }

        return IncrementAsync(operatorId, period, activity => activity.PutawayCount += 1, cancellationToken);
    }

    public Task ApplyPickAsync(PickingCompleted integrationEvent, DateOnly period, CancellationToken cancellationToken = default)
    {
        if (integrationEvent.OperatorId is not { } operatorId)
        {
            return Task.CompletedTask;
        }

        return IncrementAsync(operatorId, period, activity => activity.PickCount += 1, cancellationToken);
    }

    private Task IncrementAsync(Guid operatorId, DateOnly period, Action<OperatorActivity> increment, CancellationToken cancellationToken) =>
        store.IncrementAsync(
            [operatorId, period],
            () => new OperatorActivity { OperatorId = operatorId, Period = period },
            increment,
            cancellationToken);
}
