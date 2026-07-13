using FluentValidation;

namespace Wms.Auth.Application.Features.EntraLogin;

public sealed class EntraLoginValidator : AbstractValidator<EntraLoginCommand>
{
    public EntraLoginValidator()
    {
        RuleFor(command => command.IdToken).NotEmpty();
    }
}
