using Wms.BuildingBlocks.Domain.Primitives;
using Wms.BuildingBlocks.Domain.Results;

namespace Wms.Auth.Domain;

public sealed record UserId : StronglyTypedId<UserId, Guid>
{
    private UserId(Guid value)
        : base(value)
    {
    }

    public static Result<UserId> Create(Guid value) => Create(value, v => new UserId(v));
}
