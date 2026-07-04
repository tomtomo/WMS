using Wms.BuildingBlocks.Application.Abstractions;
using Wms.BuildingBlocks.Application.Messaging;

namespace Wms.Inbound.Application.Features.UploadGRAttachment;

// Upload dokumen pendukung GR — command hanya membawa stream.
[RequiresPermission(InboundPermissions.UploadGRAttachment)]
public sealed record UploadGRAttachmentCommand(
    Guid GoodsReceiptId,
    string FileName,
    string ContentType,
    long SizeBytes,
    Stream Content) : ICommand<Guid>;
