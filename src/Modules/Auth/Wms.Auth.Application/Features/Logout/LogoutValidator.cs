using FluentValidation;

namespace Wms.Auth.Application.Features.Logout;

public sealed class LogoutValidator : AbstractValidator<LogoutCommand>
{
    public LogoutValidator()
    {
        RuleFor(command => command.RefreshToken).NotEmpty();
    }
}
