using Wms.BuildingBlocks.Domain.Events;

namespace Wms.Outbound.Domain.Events;

// Wave dispatch (truk keluar).
public sealed record WaveDispatchedRaised(WaveId WaveId) : IDomainEvent;
