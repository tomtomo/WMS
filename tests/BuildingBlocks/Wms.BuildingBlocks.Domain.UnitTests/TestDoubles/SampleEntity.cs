using Wms.BuildingBlocks.Domain.Primitives;

namespace Wms.BuildingBlocks.Domain.UnitTests.TestDoubles;

// Test double — entity beridentity SampleId untuk test Entity.
public sealed class SampleEntity : Entity<SampleId>
{
    public SampleEntity(SampleId id)
        : base(id)
    {
    }
}
