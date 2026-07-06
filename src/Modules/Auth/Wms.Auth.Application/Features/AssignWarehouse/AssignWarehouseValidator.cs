using FluentValidation;

namespace Wms.Auth.Application.Features.AssignWarehouse;

public sealed class AssignWarehouseValidator : AbstractValidator<AssignWarehouseCommand>
{
    public AssignWarehouseValidator()
    {
        RuleFor(command => command.UserId).NotEmpty();
        RuleFor(command => command.WarehouseId).NotEmpty();
    }
}
