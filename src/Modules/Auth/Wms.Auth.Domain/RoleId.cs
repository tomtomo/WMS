using Wms.BuildingBlocks.Domain.Primitives;
using Wms.BuildingBlocks.Domain.Results;

namespace Wms.Auth.Domain;

public sealed record RoleId : StronglyTypedId<RoleId, Guid>
{
    private RoleId(Guid value)
        : base(value)
    {
    }

    public static Result<RoleId> Create(Guid value) => Create(value, v => new RoleId(v));
}
