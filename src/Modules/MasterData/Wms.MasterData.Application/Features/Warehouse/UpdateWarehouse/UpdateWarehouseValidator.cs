using FluentValidation;

namespace Wms.MasterData.Application.Features.Warehouse.UpdateWarehouse;

public sealed class UpdateWarehouseValidator : AbstractValidator<UpdateWarehouseCommand>
{
    public UpdateWarehouseValidator()
    {
        RuleFor(command => command.WarehouseId).NotEmpty();
        RuleFor(command => command.Name).NotEmpty();
        RuleFor(command => command.Address).NotEmpty();
    }
}
