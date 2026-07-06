using FluentValidation;

namespace Wms.Auth.Application.Features.SetRolePermissions;

public sealed class SetRolePermissionsValidator : AbstractValidator<SetRolePermissionsCommand>
{
    public SetRolePermissionsValidator()
    {
        RuleFor(command => command.RoleId).NotEmpty();
    }
}
