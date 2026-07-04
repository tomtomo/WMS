using Wms.BuildingBlocks.Application.Abstractions.Ports;
using Wms.BuildingBlocks.Application.Messaging;
using Wms.BuildingBlocks.Domain.Results;
using Wms.Inbound.Application.Abstractions;
using Wms.Inbound.Domain;
using Wms.Inbound.Domain.ValueObjects;

namespace Wms.Inbound.Application.Features.UploadGRAttachment;

internal sealed class UploadGRAttachmentHandler(
    IGoodsReceiptReader goodsReceiptReader,
    IGRAttachmentRepository repository,
    IObjectStore objectStore,
    TimeProvider timeProvider) : ICommandHandler<UploadGRAttachmentCommand, Guid>
{
    public async Task<Result<Guid>> Handle(UploadGRAttachmentCommand command, CancellationToken cancellationToken)
    {
        if (!await goodsReceiptReader.ExistsAsync(command.GoodsReceiptId, cancellationToken))
        {
            return Result.NotFound<Guid>(new Error("goods_receipt.not_found", "GoodsReceipt tidak ditemukan."));
        }

        var attachmentId = GRAttachmentId.Create(Guid.NewGuid());
        var goodsReceiptId = GoodsReceiptId.Create(command.GoodsReceiptId);
        if (goodsReceiptId.IsFailure)
        {
            return goodsReceiptId.ForwardFailure<Guid>();
        }

        // blobPath {grId}/{attachmentId}/{fileName}
        var blobPath = $"{command.GoodsReceiptId:D}/{attachmentId.Value.Value:D}/{command.FileName.Trim()}";
        var contentRef = ContentRef.Create(blobPath);
        if (contentRef.IsFailure)
        {
            return contentRef.ForwardFailure<Guid>();
        }

        // whitelist/size dicek sebelum ada byte ke object store.
        var attachment = GRAttachment.Create(
            attachmentId.Value,
            goodsReceiptId.Value,
            command.FileName,
            command.ContentType,
            command.SizeBytes,
            contentRef.Value,
            timeProvider.GetUtcNow());
        if (attachment.IsFailure)
        {
            return attachment.ForwardFailure<Guid>();
        }

        // Blob ditulis sebelum commit metadata.
        await objectStore.PutAsync(blobPath, command.Content, attachment.Value.ContentType, cancellationToken);
        await repository.AddAsync(attachment.Value, cancellationToken);

        return Result.Success(attachment.Value.Id.Value);
    }
}
