using System.Diagnostics;
using Wms.BuildingBlocks.Application.Abstractions.Ports;
using Wms.Inbound.Domain;
using Wms.Inbound.Domain.Events;

namespace Wms.Inbound.Application.EventTranslation;

// Domain event diterjemahkan ke integration event
// Contracts lalu ditulis ke Outbox — baris Outbox ikut SaveChanges transaksi bisnis.
public sealed class GoodsReceiptEventTranslator(IIntegrationEventOutbox outbox)
{
    public async Task TranslateAndClearAsync(GoodsReceipt goodsReceipt, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(goodsReceipt);

        foreach (var domainEvent in goodsReceipt.DomainEvents)
        {
            switch (domainEvent)
            {
                case GoodsReceiptConfirmed confirmed:
                    await outbox.AddAsync(ToContract(confirmed), Contracts.GRConfirmed.DeliveryClass, cancellationToken);
                    break;

                case GoodsReceiptPendingReviewRaised pending:
                    await outbox.AddAsync(ToContract(pending), Contracts.GoodsReceiptPendingReview.DeliveryClass, cancellationToken);
                    break;

                case GoodsReceiptHeld:
                    // Hold tak merilis integration event
                    break;

                default:
                    throw new UnreachableException($"Domain event Inbound tanpa aturan translate: {domainEvent.GetType().Name}");
            }
        }

        goodsReceipt.ClearDomainEvents();
    }

    private static Contracts.GRConfirmed ToContract(GoodsReceiptConfirmed confirmed) => new(
        confirmed.GoodsReceiptId.Value,
        confirmed.WarehouseId,
        confirmed.SupplierId,
        [.. confirmed.ReceivedLines.Select(ToContract)],
        [.. confirmed.RejectedLines.Select(ToContract)]);

    private static Contracts.GoodsReceiptPendingReview ToContract(GoodsReceiptPendingReviewRaised pending) => new(
        pending.GoodsReceiptId.Value,
        pending.WarehouseId,
        pending.HasOverDelivery,
        pending.DiscrepancyCount);

    private static Contracts.Payloads.ReceivedLine ToContract(Domain.ValueObjects.ReceivedLine line) => new(
        line.Sku,
        line.Qty,
        line.Batch,
        line.Expiry,
        ToContract(line.Status));

    private static Contracts.Payloads.RejectedLine ToContract(Domain.ValueObjects.RejectedLine line) => new(
        line.Sku,
        line.Qty,
        ToContract(line.Reason));

    // Mapping enum eksplisit
    private static Contracts.Enums.ReceivedLineStatus ToContract(Domain.Enums.ReceivedLineStatus status) => status switch
    {
        Domain.Enums.ReceivedLineStatus.Good => Contracts.Enums.ReceivedLineStatus.Good,
        Domain.Enums.ReceivedLineStatus.QcHold => Contracts.Enums.ReceivedLineStatus.QcHold,
        _ => throw new UnreachableException($"ReceivedLineStatus domain tak dikenal: {status}"),
    };

    private static Contracts.Enums.RejectionReason ToContract(Domain.Enums.RejectionReason reason) => reason switch
    {
        Domain.Enums.RejectionReason.OverDelivery => Contracts.Enums.RejectionReason.OverDelivery,
        Domain.Enums.RejectionReason.WrongItem => Contracts.Enums.RejectionReason.WrongItem,
        _ => throw new UnreachableException($"RejectionReason domain tak dikenal: {reason}"),
    };
}
