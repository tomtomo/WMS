using Wms.BuildingBlocks.Domain.Primitives;
using Wms.BuildingBlocks.Domain.Results;

namespace Wms.BuildingBlocks.Domain.UnitTests.TestDoubles;

// Test double — strongly typed ID Guid. Juga dipakai sebagai TId Entity/AggregateRoot.
public sealed record SampleId : StronglyTypedId<SampleId, Guid>
{
    private SampleId(Guid value)
        : base(value)
    {
    }

    public static Result<SampleId> Create(Guid value) => Create(value, v => new SampleId(v));
}
