using Wms.BuildingBlocks.Domain.Primitives;
using Wms.BuildingBlocks.Domain.Results;

namespace Wms.MasterData.Domain;

public sealed record Sku : StronglyTypedId<Sku, string>
{
    private Sku(string value)
        : base(value)
    {
    }

    public static Result<Sku> Create(string value) => Create(value, v => new Sku(v));
}
