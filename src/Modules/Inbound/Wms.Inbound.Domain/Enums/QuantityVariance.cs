namespace Wms.Inbound.Domain.Enums;

// Sumbu kedua discrepancy — dihitung sistem per SKU saat scan selesai.
public enum QuantityVariance
{
    Normal,
    ShortDelivery,
    OverDelivery,
}
