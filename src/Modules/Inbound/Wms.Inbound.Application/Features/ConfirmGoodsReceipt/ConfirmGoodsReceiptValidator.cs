using FluentValidation;

namespace Wms.Inbound.Application.Features.ConfirmGoodsReceipt;

public sealed class ConfirmGoodsReceiptValidator : AbstractValidator<ConfirmGoodsReceiptCommand>
{
    public ConfirmGoodsReceiptValidator() => RuleFor(command => command.GoodsReceiptId).NotEmpty();
}
