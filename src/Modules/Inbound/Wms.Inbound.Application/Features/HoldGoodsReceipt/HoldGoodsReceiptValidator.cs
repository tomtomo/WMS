using FluentValidation;

namespace Wms.Inbound.Application.Features.HoldGoodsReceipt;

public sealed class HoldGoodsReceiptValidator : AbstractValidator<HoldGoodsReceiptCommand>
{
    public HoldGoodsReceiptValidator()
    {
        RuleFor(command => command.GoodsReceiptId).NotEmpty();
        RuleFor(command => command.Reason).NotEmpty();
    }
}
