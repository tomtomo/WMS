using Wms.BuildingBlocks.Domain.Events;

namespace Wms.Outbound.Domain.Events;

// Order dilepas balik ke backlog
public sealed record OrderReturnedToBacklogRaised(OutboundOrderId OrderId, string Reason) : IDomainEvent;
