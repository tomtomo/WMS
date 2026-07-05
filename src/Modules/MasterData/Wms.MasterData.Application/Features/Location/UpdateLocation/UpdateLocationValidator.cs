using FluentValidation;

namespace Wms.MasterData.Application.Features.Location.UpdateLocation;

public sealed class UpdateLocationValidator : AbstractValidator<UpdateLocationCommand>
{
    public UpdateLocationValidator()
    {
        RuleFor(command => command.LocationId).NotEmpty();
        RuleFor(command => command.Type).NotEmpty();
        RuleFor(command => command.Code).NotEmpty();
    }
}
