using Wms.BuildingBlocks.Domain.Primitives;
using Wms.BuildingBlocks.Domain.Results;

namespace Wms.Auth.Domain;

public sealed record UserExternalLoginId : StronglyTypedId<UserExternalLoginId, Guid>
{
    private UserExternalLoginId(Guid value)
        : base(value)
    {
    }

    public static Result<UserExternalLoginId> Create(Guid value) => Create(value, v => new UserExternalLoginId(v));
}
