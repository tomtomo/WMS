using Wms.BuildingBlocks.Domain.Primitives;
using Wms.BuildingBlocks.Domain.Results;

namespace Wms.Auth.Domain;

public sealed record PermissionId : StronglyTypedId<PermissionId, Guid>
{
    private PermissionId(Guid value)
        : base(value)
    {
    }

    public static Result<PermissionId> Create(Guid value) => Create(value, v => new PermissionId(v));
}
