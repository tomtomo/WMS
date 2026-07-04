namespace Wms.Inbound.Contracts.Enums;

// Sebab leaf masuk rejectedLines (tidak menambah stok): excess di atas PO, atau item salah.
public enum RejectionReason
{
    OverDelivery,
    WrongItem,
}
