using FluentValidation;

namespace Wms.Auth.Application.Features.RefreshAccessToken;

public sealed class RefreshAccessTokenValidator : AbstractValidator<RefreshAccessTokenCommand>
{
    public RefreshAccessTokenValidator()
    {
        RuleFor(command => command.RefreshToken).NotEmpty();
    }
}
