using Wms.BuildingBlocks.Domain.Events;

namespace Wms.Outbound.Domain.Events;

// Order terpenuhi & wave dispatch — selesai dari sisi WMS.
public sealed record OutboundOrderClosedRaised(OutboundOrderId OrderId) : IDomainEvent;
