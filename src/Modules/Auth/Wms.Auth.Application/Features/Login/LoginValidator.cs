using FluentValidation;

namespace Wms.Auth.Application.Features.Login;

public sealed class LoginValidator : AbstractValidator<LoginCommand>
{
    public LoginValidator()
    {
        RuleFor(command => command.Username).NotEmpty();
        RuleFor(command => command.Password).NotEmpty();
    }
}
