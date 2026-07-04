using FluentValidation;

namespace Wms.Inbound.Application.Features.CreateGoodsReceiptHeader;

// Validasi bentuk request
public sealed class CreateGoodsReceiptHeaderValidator : AbstractValidator<CreateGoodsReceiptHeaderCommand>
{
    public CreateGoodsReceiptHeaderValidator()
    {
        RuleFor(command => command.PoRef).NotEmpty();
        RuleFor(command => command.SupplierId).NotEmpty();
        RuleFor(command => command.WarehouseId).NotEmpty();
        RuleFor(command => command.DockDoor).NotEmpty();
        RuleFor(command => command.ExpectedLines).NotEmpty();
        RuleForEach(command => command.ExpectedLines).ChildRules(line =>
        {
            line.RuleFor(input => input.Sku).NotEmpty();
            line.RuleFor(input => input.ExpectedQty).GreaterThan(0);
            line.RuleFor(input => input.Uom).NotEmpty();
        });
    }
}
