using FluentValidation;

namespace Wms.Auth.Application.Features.LinkExternalLogin;

public sealed class LinkExternalLoginValidator : AbstractValidator<LinkExternalLoginCommand>
{
    public LinkExternalLoginValidator()
    {
        RuleFor(command => command.UserId).NotEmpty();
        RuleFor(command => command.Provider).NotEmpty();
        RuleFor(command => command.Subject).NotEmpty();
    }
}
