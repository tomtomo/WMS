using Wms.BuildingBlocks.Domain.Primitives;

namespace Wms.BuildingBlocks.Domain.UnitTests.TestDoubles;

// Test double — aggregate root yang raise SampleRaised.
public sealed class SampleAggregate : AggregateRoot<SampleId>
{
    public SampleAggregate(SampleId id)
        : base(id)
    {
    }

    public void DoSomething(int sequence) => Raise(new SampleRaised(Id.Value, sequence));
}
