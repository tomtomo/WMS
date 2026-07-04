namespace Wms.Inventory.Domain.Enums;

// State fisik balance — lokasi/kualitas, bukan status alokasi
public enum StockStatus
{
    Quarantine,
    OnHand,
    Available,
    Picked,
}
