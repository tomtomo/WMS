using FluentValidation;

namespace Wms.MasterData.Application.Features.Warehouse.DeactivateWarehouse;

public sealed class DeactivateWarehouseValidator : AbstractValidator<DeactivateWarehouseCommand>
{
    public DeactivateWarehouseValidator()
    {
        RuleFor(command => command.WarehouseId).NotEmpty();
    }
}
