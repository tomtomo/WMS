using FluentValidation;

namespace Wms.MasterData.Application.Features.Product.CreateProduct;

public sealed class CreateProductValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductValidator()
    {
        RuleFor(command => command.Sku).NotEmpty();
        RuleFor(command => command.Name).NotEmpty();
        RuleFor(command => command.Uom).NotEmpty();
        RuleFor(command => command.ShelfLifeDays)
            .GreaterThanOrEqualTo(0)
            .When(command => command.ShelfLifeDays.HasValue);
    }
}
