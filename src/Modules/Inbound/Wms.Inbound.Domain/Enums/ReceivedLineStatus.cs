namespace Wms.Inbound.Domain.Enums;

// Kualitas leaf yang diterima: Good ke Stock OnHand, QcHold ke Quarantine.
public enum ReceivedLineStatus
{
    Good,
    QcHold,
}
