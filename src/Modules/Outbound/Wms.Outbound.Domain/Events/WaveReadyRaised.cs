using Wms.BuildingBlocks.Domain.Events;

namespace Wms.Outbound.Domain.Events;

// Semua PickingTask wave Completed
public sealed record WaveReadyRaised(WaveId WaveId) : IDomainEvent;
