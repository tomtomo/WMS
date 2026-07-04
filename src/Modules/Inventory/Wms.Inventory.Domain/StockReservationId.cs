using Wms.BuildingBlocks.Domain.Primitives;
using Wms.BuildingBlocks.Domain.Results;

namespace Wms.Inventory.Domain;

// ID StockReservation — typed agar tidak tertukar antar aggregate.
public sealed record StockReservationId : StronglyTypedId<StockReservationId, Guid>
{
    private StockReservationId(Guid value)
        : base(value)
    {
    }

    public static Result<StockReservationId> Create(Guid value) => Create(value, v => new StockReservationId(v));
}
