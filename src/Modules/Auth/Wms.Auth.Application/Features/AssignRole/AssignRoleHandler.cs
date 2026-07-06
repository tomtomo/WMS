using Wms.Auth.Application.Abstractions;
using Wms.Auth.Domain;
using Wms.BuildingBlocks.Application.Messaging;
using Wms.BuildingBlocks.Domain.Results;

namespace Wms.Auth.Application.Features.AssignRole;

internal sealed class AssignRoleHandler(IUserRepository userRepository)
    : ICommandHandler<AssignRoleCommand>
{
    public async Task<Result> Handle(AssignRoleCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var userId = UserId.Create(command.UserId);
        if (userId.IsFailure)
        {
            return userId;
        }

        var user = await userRepository.GetAsync(userId.Value, cancellationToken);
        if (user is null)
        {
            return Result.NotFound(new Error("user.not_found", "User tidak ditemukan."));
        }

        return user.AssignRole(command.RoleId);
    }
}
