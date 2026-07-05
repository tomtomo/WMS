using Wms.BuildingBlocks.Domain.Primitives;
using Wms.BuildingBlocks.Domain.Results;

namespace Wms.Outbound.Domain;

// ID Wave — typed agar tidak tertukar antar aggregate.
public sealed record WaveId : StronglyTypedId<WaveId, Guid>
{
    private WaveId(Guid value)
        : base(value)
    {
    }

    public static Result<WaveId> Create(Guid value) => Create(value, v => new WaveId(v));
}
