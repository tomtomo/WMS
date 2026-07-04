using Wms.BuildingBlocks.Application.Abstractions;
using Wms.BuildingBlocks.Application.Messaging;

namespace Wms.Inbound.Application.Features.SoftDeleteGRAttachment;

// Soft delete metadata attachment
[RequiresPermission(InboundPermissions.DeleteGRAttachment)]
public sealed record SoftDeleteGRAttachmentCommand(Guid GoodsReceiptId, Guid AttachmentId) : ICommand;
