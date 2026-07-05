using FluentValidation;

namespace Wms.MasterData.Application.Features.Location.DeactivateLocation;

public sealed class DeactivateLocationValidator : AbstractValidator<DeactivateLocationCommand>
{
    public DeactivateLocationValidator()
    {
        RuleFor(command => command.LocationId).NotEmpty();
    }
}
