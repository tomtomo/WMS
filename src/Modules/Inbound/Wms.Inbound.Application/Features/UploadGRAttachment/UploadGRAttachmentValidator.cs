using FluentValidation;
using Wms.Inbound.Domain;

namespace Wms.Inbound.Application.Features.UploadGRAttachment;

public sealed class UploadGRAttachmentValidator : AbstractValidator<UploadGRAttachmentCommand>
{
    public UploadGRAttachmentValidator()
    {
        RuleFor(command => command.GoodsReceiptId).NotEmpty();
        RuleFor(command => command.FileName)
            .NotEmpty()
            .MaximumLength(GRAttachment.MaxFileNameLength)
            .Must(fileName => fileName is null || (!fileName.Contains('/', StringComparison.Ordinal)
                && !fileName.Contains('\\', StringComparison.Ordinal)))
            .WithMessage("FileName tidak boleh mengandung separator path.");
        RuleFor(command => command.ContentType).NotEmpty();
        RuleFor(command => command.SizeBytes).InclusiveBetween(1, GRAttachment.MaxSizeBytes);
        RuleFor(command => command.Content).NotNull();
    }
}
