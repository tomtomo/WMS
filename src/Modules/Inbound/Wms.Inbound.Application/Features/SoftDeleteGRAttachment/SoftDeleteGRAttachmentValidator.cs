using FluentValidation;

namespace Wms.Inbound.Application.Features.SoftDeleteGRAttachment;

public sealed class SoftDeleteGRAttachmentValidator : AbstractValidator<SoftDeleteGRAttachmentCommand>
{
    public SoftDeleteGRAttachmentValidator()
    {
        RuleFor(command => command.GoodsReceiptId).NotEmpty();
        RuleFor(command => command.AttachmentId).NotEmpty();
    }
}
