using Wms.Inbound.Contracts;
using Wms.Inbound.Contracts.Enums;
using Wms.Reporting.Abstractions;
using Wms.Reporting.ReadModels;

namespace Wms.Reporting.Projections;

// Projection supplier performance dari GRConfirmed per (supplier, period).
public sealed class ReceivingSummaryProjection(IProjectionStore store)
{
    public Task ApplyAsync(GRConfirmed integrationEvent, DateOnly period, CancellationToken cancellationToken = default)
    {
        var receivedQty = integrationEvent.ReceivedLines.Sum(line => line.Qty);
        var hasDiscrepancy = integrationEvent.RejectedLines.Count > 0
            || integrationEvent.ReceivedLines.Any(line => line.Status == ReceivedLineStatus.QcHold);

        return store.IncrementAsync<ReceivingSummary>(
            [integrationEvent.SupplierId, period],
            () => new ReceivingSummary { SupplierId = integrationEvent.SupplierId, Period = period },
            summary =>
            {
                summary.ReceivedQty += receivedQty;
                summary.ReceiptCount += 1;
                if (hasDiscrepancy)
                {
                    summary.DiscrepancyCount += 1;
                }
            },
            cancellationToken);
    }
}
