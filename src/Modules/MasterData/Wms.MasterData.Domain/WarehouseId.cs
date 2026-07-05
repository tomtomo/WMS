using Wms.BuildingBlocks.Domain.Primitives;
using Wms.BuildingBlocks.Domain.Results;

namespace Wms.MasterData.Domain;

public sealed record WarehouseId : StronglyTypedId<WarehouseId, Guid>
{
    private WarehouseId(Guid value)
        : base(value)
    {
    }

    public static Result<WarehouseId> Create(Guid value) => Create(value, v => new WarehouseId(v));
}
