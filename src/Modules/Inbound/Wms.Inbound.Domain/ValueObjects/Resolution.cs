using Wms.Inbound.Domain.Enums;

namespace Wms.Inbound.Domain.ValueObjects;

// Keputusan SPV yang dipasangkan ke satu discrepancy.
public sealed record Resolution(Guid DiscrepancyId, ResolutionAction Action, string? Note);
