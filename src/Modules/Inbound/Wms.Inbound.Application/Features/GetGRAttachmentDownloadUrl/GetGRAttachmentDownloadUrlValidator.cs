using FluentValidation;

namespace Wms.Inbound.Application.Features.GetGRAttachmentDownloadUrl;

public sealed class GetGRAttachmentDownloadUrlValidator : AbstractValidator<GetGRAttachmentDownloadUrlQuery>
{
    public GetGRAttachmentDownloadUrlValidator()
    {
        RuleFor(query => query.GoodsReceiptId).NotEmpty();
        RuleFor(query => query.AttachmentId).NotEmpty();
    }
}
