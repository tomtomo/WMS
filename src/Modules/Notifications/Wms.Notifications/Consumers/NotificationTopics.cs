namespace Wms.Notifications.Consumers;

// Daftar topik subscription notifikasi.
internal static class NotificationTopics
{
    public const string GoodsReceiptPendingReview = "GoodsReceiptPendingReview";

    public const string GoodsReceiptOverDelivery = "GoodsReceiptOverDelivery";

    public const string WaveReady = "WaveReady";

    public const string StockShortfall = "StockShortfall";

    public const string StockNearExpiry = "StockNearExpiry";
}
