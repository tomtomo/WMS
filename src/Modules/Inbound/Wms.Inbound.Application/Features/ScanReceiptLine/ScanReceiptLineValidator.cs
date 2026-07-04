using FluentValidation;

namespace Wms.Inbound.Application.Features.ScanReceiptLine;

public sealed class ScanReceiptLineValidator : AbstractValidator<ScanReceiptLineCommand>
{
    public ScanReceiptLineValidator()
    {
        RuleFor(command => command.GoodsReceiptId).NotEmpty();
        RuleFor(command => command.Sku).NotEmpty();
        RuleFor(command => command.ActualQty).GreaterThan(0);
        RuleFor(command => command.LineStatus).IsInEnum();
    }
}
