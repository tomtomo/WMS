namespace Wms.Inbound.Domain.Enums;

// Keputusan SPV per discrepancy — satu action valid per DiscrepancyType.
public enum ResolutionAction
{
    AcceptPartial,
    RejectExcess,
    ReturnToSupplier,
    SendToQC,
}
