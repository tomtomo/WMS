using FluentValidation;

namespace Wms.MasterData.Application.Features.Location.CreateLocation;

public sealed class CreateLocationValidator : AbstractValidator<CreateLocationCommand>
{
    public CreateLocationValidator()
    {
        RuleFor(command => command.WarehouseId).NotEmpty();
        RuleFor(command => command.Type).NotEmpty();
        RuleFor(command => command.Code).NotEmpty();
    }
}
