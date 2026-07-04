using Wms.BuildingBlocks.Domain.Primitives;
using Wms.BuildingBlocks.Domain.Results;

namespace Wms.Inbound.Domain;

// ID GRAttachment — typed
public sealed record GRAttachmentId : StronglyTypedId<GRAttachmentId, Guid>
{
    private GRAttachmentId(Guid value)
        : base(value)
    {
    }

    public static Result<GRAttachmentId> Create(Guid value) => Create(value, v => new GRAttachmentId(v));
}
