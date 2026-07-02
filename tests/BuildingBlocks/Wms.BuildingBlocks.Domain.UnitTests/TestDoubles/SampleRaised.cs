using Wms.BuildingBlocks.Domain.Events;

namespace Wms.BuildingBlocks.Domain.UnitTests.TestDoubles;

// Test double — domain event in-process untuk test marker IDomainEvent.
public sealed record SampleRaised(Guid AggregateId, int Sequence) : IDomainEvent;
