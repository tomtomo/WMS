using Wms.BuildingBlocks.Domain.Primitives;

namespace Wms.BuildingBlocks.Domain.UnitTests.TestDoubles;

// Test double — entity tipe TId sama.
public sealed class AnotherEntity : Entity<SampleId>
{
    public AnotherEntity(SampleId id)
        : base(id)
    {
    }
}
