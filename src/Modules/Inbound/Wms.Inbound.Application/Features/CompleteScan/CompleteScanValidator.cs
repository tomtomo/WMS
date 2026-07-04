using FluentValidation;

namespace Wms.Inbound.Application.Features.CompleteScan;

public sealed class CompleteScanValidator : AbstractValidator<CompleteScanCommand>
{
    public CompleteScanValidator() => RuleFor(command => command.GoodsReceiptId).NotEmpty();
}
