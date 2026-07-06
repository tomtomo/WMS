using Wms.Auth.Application.Abstractions;
using Wms.Auth.Domain;
using Wms.BuildingBlocks.Application.Messaging;
using Wms.BuildingBlocks.Domain.Results;

namespace Wms.Auth.Application.Features.CreateRole;

internal sealed class CreateRoleHandler(IRoleRepository roleRepository)
    : ICommandHandler<CreateRoleCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateRoleCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (await roleRepository.CodeExistsAsync(command.Code, cancellationToken))
        {
            return Result.Conflict<Guid>(new Error("role.code_taken", "Code role sudah dipakai."));
        }

        var role = Role.Create(RoleId.Create(Guid.NewGuid()).Value, command.Code, command.Name, command.PermissionIds);
        if (role.IsFailure)
        {
            return role.ForwardFailure<Guid>();
        }

        await roleRepository.AddAsync(role.Value, cancellationToken);
        return Result.Success(role.Value.Id.Value);
    }
}
