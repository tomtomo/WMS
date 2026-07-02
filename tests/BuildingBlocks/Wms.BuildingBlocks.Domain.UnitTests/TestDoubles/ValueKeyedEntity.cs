using Wms.BuildingBlocks.Domain.Primitives;

namespace Wms.BuildingBlocks.Domain.UnitTests.TestDoubles;

// Test double — entity ber Id value type (int) untuk test transient (Id default).
public sealed class ValueKeyedEntity : Entity<int>
{
    public ValueKeyedEntity(int id)
        : base(id)
    {
    }
}
