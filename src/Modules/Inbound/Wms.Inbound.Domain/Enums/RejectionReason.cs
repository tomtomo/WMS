namespace Wms.Inbound.Domain.Enums;

// Penyebab leaf keluar dari stok GR: excess di atas PO atau item salah.
public enum RejectionReason
{
    OverDelivery,
    WrongItem,
}
