using Wms.BuildingBlocks.Domain.Primitives;
using Wms.BuildingBlocks.Domain.Results;

namespace Wms.Notifications.Subscriptions;

public sealed record SubscriptionId : StronglyTypedId<SubscriptionId, Guid>
{
    private SubscriptionId(Guid value)
        : base(value)
    {
    }

    public static Result<SubscriptionId> Create(Guid value) => Create(value, v => new SubscriptionId(v));
}
