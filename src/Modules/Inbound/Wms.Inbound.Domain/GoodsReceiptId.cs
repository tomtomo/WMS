using Wms.BuildingBlocks.Domain.Primitives;
using Wms.BuildingBlocks.Domain.Results;

namespace Wms.Inbound.Domain;

// ID GoodsReceipt — typed agar tidak tertukar antar aggregate.
public sealed record GoodsReceiptId : StronglyTypedId<GoodsReceiptId, Guid>
{
    private GoodsReceiptId(Guid value)
        : base(value)
    {
    }

    public static Result<GoodsReceiptId> Create(Guid value) => Create(value, v => new GoodsReceiptId(v));
}
