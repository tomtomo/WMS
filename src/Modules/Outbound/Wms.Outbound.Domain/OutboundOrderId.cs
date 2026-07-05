using Wms.BuildingBlocks.Domain.Primitives;
using Wms.BuildingBlocks.Domain.Results;

namespace Wms.Outbound.Domain;

// ID OutboundOrder — typed agar tidak tertukar antar aggregate.
public sealed record OutboundOrderId : StronglyTypedId<OutboundOrderId, Guid>
{
    private OutboundOrderId(Guid value)
        : base(value)
    {
    }

    public static Result<OutboundOrderId> Create(Guid value) => Create(value, v => new OutboundOrderId(v));
}
