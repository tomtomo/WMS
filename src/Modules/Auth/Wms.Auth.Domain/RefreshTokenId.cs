using Wms.BuildingBlocks.Domain.Primitives;
using Wms.BuildingBlocks.Domain.Results;

namespace Wms.Auth.Domain;

public sealed record RefreshTokenId : StronglyTypedId<RefreshTokenId, Guid>
{
    private RefreshTokenId(Guid value)
        : base(value)
    {
    }

    public static Result<RefreshTokenId> Create(Guid value) => Create(value, v => new RefreshTokenId(v));
}
