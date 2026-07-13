using Wms.Auth.Application.Abstractions;
using Wms.Auth.Domain;
using Wms.BuildingBlocks.Application.Messaging;
using Wms.BuildingBlocks.Domain.Results;

namespace Wms.Auth.Application.Features.LinkExternalLogin;

internal sealed class LinkExternalLoginHandler(
    IUserRepository userRepository,
    IUserExternalLoginRepository externalLoginRepository)
    : ICommandHandler<LinkExternalLoginCommand, Guid>
{
    public async Task<Result<Guid>> Handle(LinkExternalLoginCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var userId = UserId.Create(command.UserId).Value;
        var user = await userRepository.GetAsync(userId, cancellationToken);
        if (user is null)
        {
            return Result.NotFound<Guid>(new Error("user.not_found", "User tidak ditemukan."));
        }

        if (await externalLoginRepository.ExistsAsync(command.Provider, command.Subject, cancellationToken))
        {
            return Result.Conflict<Guid>(
                new Error("user_external_login.already_linked", "Identitas eksternal sudah ditautkan."));
        }

        var link = UserExternalLogin.Link(
            UserExternalLoginId.Create(Guid.NewGuid()).Value,
            command.Provider,
            command.Subject,
            userId);
        if (link.IsFailure)
        {
            return link.ForwardFailure<Guid>();
        }

        await externalLoginRepository.AddAsync(link.Value, cancellationToken);
        return Result.Success(link.Value.Id.Value);
    }
}
