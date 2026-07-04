using Wms.BuildingBlocks.Domain.Primitives;
using Wms.BuildingBlocks.Domain.Results;

namespace Wms.Inventory.Domain;

// ID Stock — typed agar tidak tertukar antar aggregate.
public sealed record StockId : StronglyTypedId<StockId, Guid>
{
    private StockId(Guid value)
        : base(value)
    {
    }

    public static Result<StockId> Create(Guid value) => Create(value, v => new StockId(v));
}
