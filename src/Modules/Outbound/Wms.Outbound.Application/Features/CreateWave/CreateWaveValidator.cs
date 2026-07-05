using FluentValidation;

namespace Wms.Outbound.Application.Features.CreateWave;

public sealed class CreateWaveValidator : AbstractValidator<CreateWaveCommand>
{
    public CreateWaveValidator()
    {
        RuleFor(command => command.WarehouseId).NotEmpty();
        RuleFor(command => command.OrderIds).NotEmpty();
        RuleForEach(command => command.OrderIds).NotEmpty();
    }
}
