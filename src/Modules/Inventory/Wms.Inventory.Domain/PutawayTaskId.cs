using Wms.BuildingBlocks.Domain.Primitives;
using Wms.BuildingBlocks.Domain.Results;

namespace Wms.Inventory.Domain;

// ID PutawayTask — typed agar tidak tertukar antar aggregate.
public sealed record PutawayTaskId : StronglyTypedId<PutawayTaskId, Guid>
{
    private PutawayTaskId(Guid value)
        : base(value)
    {
    }

    public static Result<PutawayTaskId> Create(Guid value) => Create(value, v => new PutawayTaskId(v));
}
