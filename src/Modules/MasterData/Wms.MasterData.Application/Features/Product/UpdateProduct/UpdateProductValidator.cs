using FluentValidation;

namespace Wms.MasterData.Application.Features.Product.UpdateProduct;

public sealed class UpdateProductValidator : AbstractValidator<UpdateProductCommand>
{
    public UpdateProductValidator()
    {
        RuleFor(command => command.Sku).NotEmpty();
        RuleFor(command => command.Name).NotEmpty();
        RuleFor(command => command.Uom).NotEmpty();
        RuleFor(command => command.ShelfLifeDays)
            .GreaterThanOrEqualTo(0)
            .When(command => command.ShelfLifeDays.HasValue);
    }
}
