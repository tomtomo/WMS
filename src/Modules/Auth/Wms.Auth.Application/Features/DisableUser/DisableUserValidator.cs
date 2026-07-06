using FluentValidation;

namespace Wms.Auth.Application.Features.DisableUser;

public sealed class DisableUserValidator : AbstractValidator<DisableUserCommand>
{
    public DisableUserValidator()
    {
        RuleFor(command => command.UserId).NotEmpty();
    }
}
