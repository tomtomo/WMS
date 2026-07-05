using FluentValidation;

namespace Wms.MasterData.Application.Features.Product.DeactivateProduct;

public sealed class DeactivateProductValidator : AbstractValidator<DeactivateProductCommand>
{
    public DeactivateProductValidator()
    {
        RuleFor(command => command.Sku).NotEmpty();
    }
}
