using Wms.BuildingBlocks.Domain.Primitives;
using Wms.BuildingBlocks.Domain.Results;

namespace Wms.Notifications.Deliveries;

public sealed record DeliveryId : StronglyTypedId<DeliveryId, Guid>
{
    private DeliveryId(Guid value)
        : base(value)
    {
    }

    public static Result<DeliveryId> Create(Guid value) => Create(value, v => new DeliveryId(v));
}
