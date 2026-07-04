using Wms.BuildingBlocks.Application.Messaging;
using Wms.BuildingBlocks.Domain.Results;
using Wms.Inbound.Application.Abstractions;
using Wms.Inbound.Domain;

namespace Wms.Inbound.Application.Features.SoftDeleteGRAttachment;

internal sealed class SoftDeleteGRAttachmentHandler(IGRAttachmentRepository repository)
    : ICommandHandler<SoftDeleteGRAttachmentCommand>
{
    private static readonly Error _notFound = new("gr_attachment.not_found", "Attachment tidak ditemukan.");

    public async Task<Result> Handle(SoftDeleteGRAttachmentCommand command, CancellationToken cancellationToken)
    {
        var attachmentId = GRAttachmentId.Create(command.AttachmentId);
        if (attachmentId.IsFailure)
        {
            return attachmentId;
        }

        var attachment = await repository.GetActiveAsync(attachmentId.Value, cancellationToken);

        // Mismatch GR disamakan dengan tidak ada
        if (attachment is null || attachment.GoodsReceiptId.Value != command.GoodsReceiptId)
        {
            return Result.NotFound(_notFound);
        }

        attachment.SoftDelete();
        return Result.Success();
    }
}
