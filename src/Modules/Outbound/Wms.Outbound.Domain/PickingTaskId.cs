using Wms.BuildingBlocks.Domain.Primitives;
using Wms.BuildingBlocks.Domain.Results;

namespace Wms.Outbound.Domain;

// ID PickingTask — typed agar tidak tertukar antar aggregate.
public sealed record PickingTaskId : StronglyTypedId<PickingTaskId, Guid>
{
    private PickingTaskId(Guid value)
        : base(value)
    {
    }

    public static Result<PickingTaskId> Create(Guid value) => Create(value, v => new PickingTaskId(v));
}
