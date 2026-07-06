using FluentValidation;

namespace Wms.Auth.Application.Features.UnlockUser;

public sealed class UnlockUserValidator : AbstractValidator<UnlockUserCommand>
{
    public UnlockUserValidator()
    {
        RuleFor(command => command.UserId).NotEmpty();
    }
}
