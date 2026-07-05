using FluentValidation;

namespace Wms.MasterData.Application.Features.Warehouse.CreateWarehouse;

public sealed class CreateWarehouseValidator : AbstractValidator<CreateWarehouseCommand>
{
    public CreateWarehouseValidator()
    {
        RuleFor(command => command.Name).NotEmpty();
        RuleFor(command => command.Address).NotEmpty();
    }
}
