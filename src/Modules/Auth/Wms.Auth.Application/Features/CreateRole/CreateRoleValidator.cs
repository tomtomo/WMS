using FluentValidation;

namespace Wms.Auth.Application.Features.CreateRole;

public sealed class CreateRoleValidator : AbstractValidator<CreateRoleCommand>
{
    public CreateRoleValidator()
    {
        RuleFor(command => command.Code).NotEmpty().MaximumLength(50);
        RuleFor(command => command.Name).NotEmpty().MaximumLength(100);
    }
}
