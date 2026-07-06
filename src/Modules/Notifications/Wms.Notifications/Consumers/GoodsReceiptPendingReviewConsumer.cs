using Wms.BuildingBlocks.Application.Abstractions;
using Wms.BuildingBlocks.Application.Abstractions.Ports;
using Wms.BuildingBlocks.Domain.Results;
using Wms.Inbound.Contracts;

namespace Wms.Notifications.Consumers;

// GR menunggu review SPV.
public sealed class GoodsReceiptPendingReviewConsumer(
    IInboxGuard inbox,
    NotificationFanout fanout,
    IUnitOfWork unitOfWork)
{
    public const string HandlerType = "notifications.goods_receipt_pending_review";

    public async Task<Result> ConsumeAsync(
        GoodsReceiptPendingReview integrationEvent,
        Guid eventId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        if (await inbox.HasProcessedAsync(eventId, HandlerType, cancellationToken))
        {
            return Result.Success();
        }

        var eventRef = eventId.ToString("N");

        var review = new NotificationContent(
            "Goods receipt menunggu review",
            $"GR {integrationEvent.GrId:N} menunggu review — {integrationEvent.DiscrepancyCount} discrepancy.",
            nameof(GoodsReceiptPendingReview));
        await fanout.FanOutAsync(
            NotificationTopics.GoodsReceiptPendingReview, review, integrationEvent.WarehouseId, eventRef, cancellationToken);

        if (integrationEvent.HasOverDelivery)
        {
            var overDelivery = new NotificationContent(
                "Over-delivery terdeteksi",
                $"GR {integrationEvent.GrId:N} over-delivery — {integrationEvent.DiscrepancyCount} discrepancy.",
                nameof(GoodsReceiptPendingReview));
            await fanout.FanOutAsync(
                NotificationTopics.GoodsReceiptOverDelivery, overDelivery, integrationEvent.WarehouseId, eventRef, cancellationToken);
        }

        await inbox.MarkProcessedAsync(eventId, HandlerType, cancellationToken);
        return await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
