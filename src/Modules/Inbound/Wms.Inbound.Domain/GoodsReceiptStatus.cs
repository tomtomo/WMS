namespace Wms.Inbound.Domain;

// State machine GoodsReceipt
public enum GoodsReceiptStatus
{
    InProgress,
    Pending,
    Confirmed,
    Hold,
}
