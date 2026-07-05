using Wms.BuildingBlocks.Domain.Events;

namespace Wms.Outbound.Domain.Events;

// Wave tidak terpenuhi
public sealed record WaveCancelledRaised(WaveId WaveId, string Reason) : IDomainEvent;
