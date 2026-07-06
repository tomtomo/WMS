using Wms.Auth.Application.Abstractions;
using Wms.Auth.Domain;
using Wms.BuildingBlocks.Application.Abstractions.Ports;
using Wms.BuildingBlocks.Application.Messaging;
using Wms.BuildingBlocks.Domain.Results;

namespace Wms.Auth.Application.Features.CreateUser;

internal sealed class CreateUserHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher)
    : ICommandHandler<CreateUserCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateUserCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (await userRepository.UsernameExistsAsync(command.Username, cancellationToken))
        {
            return Result.Conflict<Guid>(new Error("user.username_taken", "Username sudah dipakai."));
        }

        var passwordHash = passwordHasher.Hash(command.Password);
        var user = User.Create(
            UserId.Create(Guid.NewGuid()).Value,
            command.Username,
            command.Email,
            passwordHash,
            command.RoleIds,
            command.AssignedWarehouseIds);
        if (user.IsFailure)
        {
            return user.ForwardFailure<Guid>();
        }

        await userRepository.AddAsync(user.Value, cancellationToken);
        return Result.Success(user.Value.Id.Value);
    }
}
