using Wms.BuildingBlocks.Domain.Primitives;
using Wms.BuildingBlocks.Domain.Results;

namespace Wms.BuildingBlocks.Domain.UnitTests.TestDoubles;

// Test double — strongly typed ID Guid.
public sealed record OtherId : StronglyTypedId<OtherId, Guid>
{
    private OtherId(Guid value)
        : base(value)
    {
    }

    public static Result<OtherId> Create(Guid value) => Create(value, v => new OtherId(v));
}
