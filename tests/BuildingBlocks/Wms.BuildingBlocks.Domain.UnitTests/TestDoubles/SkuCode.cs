using Wms.BuildingBlocks.Domain.Primitives;
using Wms.BuildingBlocks.Domain.Results;

namespace Wms.BuildingBlocks.Domain.UnitTests.TestDoubles;

// Test double — strongly typed ID string, untuk blank string.
public sealed record SkuCode : StronglyTypedId<SkuCode, string>
{
    private SkuCode(string value)
        : base(value)
    {
    }

    public static Result<SkuCode> Create(string value) => Create(value, v => new SkuCode(v));
}
