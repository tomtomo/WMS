using FluentValidation;

namespace Wms.Outbound.Application.Features.DispatchWave;

public sealed class DispatchWaveValidator : AbstractValidator<DispatchWaveCommand>
{
    public DispatchWaveValidator() => RuleFor(command => command.WaveId).NotEmpty();
}
