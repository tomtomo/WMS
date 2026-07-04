using Wms.BuildingBlocks.Application.Abstractions.Ports;
using Wms.BuildingBlocks.Application.Messaging;
using Wms.BuildingBlocks.Domain.Results;
using Wms.Inbound.Application.Abstractions;

namespace Wms.Inbound.Application.Features.GetGRAttachmentDownloadUrl;

internal sealed class GetGRAttachmentDownloadUrlHandler(
    IGRAttachmentReader attachmentReader,
    IObjectStore objectStore,
    TimeProvider timeProvider) : IQueryHandler<GetGRAttachmentDownloadUrlQuery, GRAttachmentDownloadUrl>
{
    // URL untuk satu action download, bukan link permanen.
    private static readonly TimeSpan _timeToLive = TimeSpan.FromMinutes(15);

    public async Task<Result<GRAttachmentDownloadUrl>> Handle(
        GetGRAttachmentDownloadUrlQuery query,
        CancellationToken cancellationToken)
    {
        var contentRef = await attachmentReader.GetActiveContentRefAsync(
            query.GoodsReceiptId,
            query.AttachmentId,
            cancellationToken);
        if (contentRef is null)
        {
            return Result.NotFound<GRAttachmentDownloadUrl>(
                new Error("gr_attachment.not_found", "Attachment tidak ditemukan."));
        }

        var url = objectStore.CreateReadUrl(contentRef, _timeToLive);
        return Result.Success(new GRAttachmentDownloadUrl(url, timeProvider.GetUtcNow().Add(_timeToLive)));
    }
}
