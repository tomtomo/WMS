using Wms.BuildingBlocks.Application.Abstractions;
using Wms.BuildingBlocks.Application.Messaging;

namespace Wms.Inbound.Application.Features.GetGRAttachmentDownloadUrl;

// Service menerbitkan pre signed URL
[RequiresPermission(InboundPermissions.ReadGR)]
public sealed record GetGRAttachmentDownloadUrlQuery(Guid GoodsReceiptId, Guid AttachmentId)
    : IQuery<GRAttachmentDownloadUrl>;

public sealed record GRAttachmentDownloadUrl(Uri Url, DateTimeOffset ExpiresAt);
