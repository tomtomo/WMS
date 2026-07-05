using Wms.BuildingBlocks.Domain.Primitives;
using Wms.BuildingBlocks.Domain.Results;

namespace Wms.MasterData.Domain;

public sealed record LocationId : StronglyTypedId<LocationId, Guid>
{
    private LocationId(Guid value)
        : base(value)
    {
    }

    public static Result<LocationId> Create(Guid value) => Create(value, v => new LocationId(v));
}
